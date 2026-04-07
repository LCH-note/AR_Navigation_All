import { BadRequestException, Injectable, NotFoundException } from '@nestjs/common';
import { PrismaService } from '../prisma/prisma.service';
import type { GraphEdgeKind, GraphNodeType } from '@prisma/client';

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
    const facility = await this.prisma.facility.findUnique({
      where: { id: input.facilityId },
      select: { id: true },
    });

    if (!facility) {
      throw new NotFoundException(`Facility ${input.facilityId} not found`);
    }

    return this.prisma.graphNode.create({
      data: input,
    });
  }

  async listNodes(facilityId: number) {
    return this.prisma.graphNode.findMany({
      where: { facilityId },
      orderBy: { id: 'asc' },
    });
  }

  async deleteNode(facilityId: number, nodeId: number) {
    const node = await this.prisma.graphNode.findFirst({
      where: { id: nodeId, facilityId },
      select: { id: true },
    });

    if (!node) {
      throw new NotFoundException(`Node ${nodeId} not found in facility ${facilityId}`);
    }

    await this.prisma.graphEdge.deleteMany({
      where: {
        facilityId,
        OR: [{ fromNodeId: nodeId }, { toNodeId: nodeId }],
      },
    });

    await this.prisma.graphNode.delete({
      where: { id: nodeId },
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
    const [fromNode, toNode] = await Promise.all([
      this.prisma.graphNode.findFirst({
        where: { id: input.fromNodeId, facilityId: input.facilityId },
        select: { id: true },
      }),
      this.prisma.graphNode.findFirst({
        where: { id: input.toNodeId, facilityId: input.facilityId },
        select: { id: true },
      }),
    ]);

    if (!fromNode) {
      throw new BadRequestException(
        `fromNodeId ${input.fromNodeId} is not in facility ${input.facilityId}`,
      );
    }

    if (!toNode) {
      throw new BadRequestException(
        `toNodeId ${input.toNodeId} is not in facility ${input.facilityId}`,
      );
    }

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
      orderBy: { id: 'asc' },
    });
  }

  async deleteEdge(facilityId: number, edgeId: number) {
    const edge = await this.prisma.graphEdge.findFirst({
      where: { id: edgeId, facilityId },
      select: { id: true },
    });

    if (!edge) {
      throw new NotFoundException(`Edge ${edgeId} not found in facility ${facilityId}`);
    }

    await this.prisma.graphEdge.delete({
      where: { id: edgeId },
    });

    return { ok: true };
  }

  async findPath(facilityId: number, fromNodeId: number, toNodeId: number) {
    const nodes = await this.prisma.graphNode.findMany({
      where: { facilityId },
      orderBy: { id: 'asc' },
    });

    const edges = await this.prisma.graphEdge.findMany({
      where: { facilityId },
      orderBy: { id: 'asc' },
    });

    const nodeIds = new Set(nodes.map((node) => node.id));

    if (!nodeIds.has(fromNodeId)) {
      throw new BadRequestException(
        `fromNodeId ${fromNodeId} is not in facility ${facilityId}`,
      );
    }

    if (!nodeIds.has(toNodeId)) {
      throw new BadRequestException(
        `toNodeId ${toNodeId} is not in facility ${facilityId}`,
      );
    }

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

  async findNearestNode(
  facilityId: number,
  x: number,
  y: number,
  z: number,
) {
  const nodes = await this.prisma.graphNode.findMany({
    where: { facilityId },
  });

  if (nodes.length === 0) {
    return { nearestNodeId: null };
  }

  let nearestNodeId: number | null = null;
  let minDistance = Infinity;

  for (const node of nodes) {
    const dx = node.x - x;
    const dy = node.y - y;
    const dz = node.z - z;

    const distance = Math.sqrt(dx * dx + dy * dy + dz * dz);

    if (distance < minDistance) {
      minDistance = distance;
      nearestNodeId = node.id;
    }
  }

  return {
    nearestNodeId,
    distance: minDistance,
  };
}
}