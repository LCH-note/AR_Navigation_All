import { Injectable } from '@nestjs/common';
import { PrismaService } from '../prisma/prisma.service';
import type { GraphNodeType, GraphEdgeKind } from '@prisma/client';

@Injectable()
export class GraphService {
  constructor(private prisma: PrismaService) {}

  // ---------- Nodes ----------
  async createNode(input: {
    facilityId: number;
    x: number;
    y: number;
    z: number;
    floor?: number;
    type: GraphNodeType;
    label?: string;
  }) {
    return this.prisma.graphNode.create({
      data: input,
    });
  }

  async listNodes(facilityId: number) {
    return this.prisma.graphNode.findMany({
      where: { facilityId },
    });
  }

  async deleteNode(facilityId: number, nodeId: number) {
    await this.prisma.graphNode.deleteMany({
      where: { id: nodeId, facilityId },
    });

    
    await this.prisma.graphEdge.deleteMany({
      where: {
        facilityId,
        OR: [
          { fromNodeId: nodeId },
          { toNodeId: nodeId },
        ],
      },
    });

    return { ok: true };
  }

  // ---------- Edges ----------
  async createEdge(input: {
    facilityId: number;
    fromNodeId: number;
    toNodeId: number;
    weight?: number;
    kind?: GraphEdgeKind;
    bidirectional?: boolean;
  }) {
    return this.prisma.graphEdge.create({
      data: {
        ...input,
        bidirectional: input.bidirectional ?? true,
      },
    });
  }

  async listEdges(facilityId: number) {
    return this.prisma.graphEdge.findMany({
      where: { facilityId },
    });
  }

  async deleteEdge(facilityId: number, edgeId: number) {
    await this.prisma.graphEdge.deleteMany({
      where: { id: edgeId, facilityId },
    });
    return { ok: true };
  }
  

  async findPath(
  facilityId: number,
  fromNodeId: number,
  toNodeId: number,
) {
  const nodes = await this.prisma.graphNode.findMany({
    where: { facilityId },
  });

  const edges = await this.prisma.graphEdge.findMany({
    where: { facilityId },
  });

  const adjacency = new Map<number, { node: number; weight: number }[]>();

  for (const edge of edges) {
    const weight = edge.weight ?? 1;

    if (!adjacency.has(edge.fromNodeId)) adjacency.set(edge.fromNodeId, []);
    adjacency.get(edge.fromNodeId)!.push({
      node: edge.toNodeId,
      weight,
    });

    if (edge.bidirectional) {
      if (!adjacency.has(edge.toNodeId)) adjacency.set(edge.toNodeId, []);
      adjacency.get(edge.toNodeId)!.push({
        node: edge.fromNodeId,
        weight,
      });
    }
  }

  const distances = new Map<number, number>();
  const previous = new Map<number, number | null>();
  const queue = new Set<number>();

  for (const node of nodes) {
    distances.set(node.id, Infinity);
    previous.set(node.id, null);
    queue.add(node.id);
  }

  distances.set(fromNodeId, 0);

  while (queue.size) {
    let current: number | null = null;
    let minDist = Infinity;

    for (const nodeId of queue) {
      const dist = distances.get(nodeId)!;
      if (dist < minDist) {
        minDist = dist;
        current = nodeId;
      }
    }

    if (current === null) break;
    queue.delete(current);

    if (current === toNodeId) break;

    const neighbors = adjacency.get(current) || [];

    for (const neighbor of neighbors) {
      const alt = distances.get(current)! + neighbor.weight;

      if (alt < distances.get(neighbor.node)!) {
        distances.set(neighbor.node, alt);
        previous.set(neighbor.node, current);
      }
    }
  }

  if (!distances.has(fromNodeId) || !distances.has(toNodeId)) {
  return {
    path: [],
    distance: null,
    message: 'invalid node id',
  };
}

if (distances.get(toNodeId) === Infinity) {
  return {
    path: [],
    distance: null,
    message: 'path not found',
  };
}

const path: number[] = [];
let current: number | null = toNodeId;

while (current != null) {
  path.unshift(current);
  current = previous.get(current) ?? null;
}

return {
  path,
  distance: distances.get(toNodeId) ?? null,
};
}
}