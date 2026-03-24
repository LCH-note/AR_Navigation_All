import { Body, Controller, Delete, Get, Param, Post, Query } from '@nestjs/common';
import { GraphService } from './graph.service';
import type { GraphNodeType, GraphEdgeKind } from '@prisma/client';

@Controller('facilities/:facilityId/graph')
export class GraphController {
  constructor(private readonly graphService: GraphService) {}

  // ---- Nodes ----
  @Post('nodes')
  async createNode(
    @Param('facilityId') facilityId: string,
    @Body()
    body: {
      x: number;
      y: number;
      z: number;
      floor?: number;
      type: GraphNodeType;
      label?: string;
    },
  ) {
    return this.graphService.createNode({
      facilityId: Number(facilityId),
      ...body,
    });
  }

  @Get('nodes')
  async listNodes(@Param('facilityId') facilityId: string) {
    return this.graphService.listNodes(Number(facilityId));
  }

  @Delete('nodes/:nodeId')
  async deleteNode(@Param('facilityId') facilityId: string, @Param('nodeId') nodeId: string) {
    return this.graphService.deleteNode(Number(facilityId), Number(nodeId));
  }

  // ---- Edges ----
  @Post('edges')
  async createEdge(
    @Param('facilityId') facilityId: string,
    @Body()
    body: {
      fromNodeId: number;
      toNodeId: number;
      weight?: number;
      kind?: GraphEdgeKind;
      bidirectional?: boolean;
    },
  ) {
    return this.graphService.createEdge({
      facilityId: Number(facilityId),
      fromNodeId: body.fromNodeId,
      toNodeId: body.toNodeId,
      weight: body.weight,
      kind: body.kind,
      bidirectional: body.bidirectional ?? true,
    });
  }

  @Get('edges')
  async listEdges(@Param('facilityId') facilityId: string) {
    return this.graphService.listEdges(Number(facilityId));
  }

  @Delete('edges/:edgeId')
  async deleteEdge(@Param('facilityId') facilityId: string, @Param('edgeId') edgeId: string) {
    return this.graphService.deleteEdge(Number(facilityId), Number(edgeId));
  }

  @Get('path')
async path(
  @Param('facilityId') facilityId: string,
  @Query('fromNodeId') fromNodeId: string,
  @Query('toNodeId') toNodeId: string,
) {
  return this.graphService.findPath(
    Number(facilityId),
    Number(fromNodeId),
    Number(toNodeId),
  );
}

  
}