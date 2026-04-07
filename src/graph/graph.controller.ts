import {
  Body,
  Controller,
  Delete,
  Get,
  Param,
  ParseIntPipe,
  Post,
  Query,
} from '@nestjs/common';
import { GraphService } from './graph.service';
import type { GraphEdgeKind, GraphNodeType } from '@prisma/client';
import { ParseFloatPipe } from '@nestjs/common';

@Controller('facilities/:facilityId/graph')
export class GraphController {
  constructor(private readonly graphService: GraphService) {}

  // ---- Nodes ----
  @Post('nodes')
  async createNode(
    @Param('facilityId', ParseIntPipe) facilityId: number,
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
      facilityId,
      ...body,
    });
  }

  @Get('nodes')
  async listNodes(@Param('facilityId', ParseIntPipe) facilityId: number) {
    return this.graphService.listNodes(facilityId);
  }

  @Delete('nodes/:nodeId')
  async deleteNode(
    @Param('facilityId', ParseIntPipe) facilityId: number,
    @Param('nodeId', ParseIntPipe) nodeId: number,
  ) {
    return this.graphService.deleteNode(facilityId, nodeId);
  }

  // ---- Edges ----
  @Post('edges')
  async createEdge(
    @Param('facilityId', ParseIntPipe) facilityId: number,
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
      facilityId,
      fromNodeId: body.fromNodeId,
      toNodeId: body.toNodeId,
      weight: body.weight,
      kind: body.kind,
      bidirectional: body.bidirectional ?? true,
    });
  }

  @Get('edges')
  async listEdges(@Param('facilityId', ParseIntPipe) facilityId: number) {
    return this.graphService.listEdges(facilityId);
  }

  @Delete('edges/:edgeId')
  async deleteEdge(
    @Param('facilityId', ParseIntPipe) facilityId: number,
    @Param('edgeId', ParseIntPipe) edgeId: number,
  ) {
    return this.graphService.deleteEdge(facilityId, edgeId);
  }

  @Get('path')
  async path(
    @Param('facilityId', ParseIntPipe) facilityId: number,
    @Query('fromNodeId', ParseIntPipe) fromNodeId: number,
    @Query('toNodeId', ParseIntPipe) toNodeId: number,
  ) {
    return this.graphService.findPath(facilityId, fromNodeId, toNodeId);
  }

  @Get('nearest')
async nearest(
  @Param('facilityId', ParseIntPipe) facilityId: number,
  @Query('x', ParseFloatPipe) x: number,
  @Query('y', ParseFloatPipe) y: number,
  @Query('z', ParseFloatPipe) z: number,
) {
  return this.graphService.findNearestNode(facilityId, x, y, z);
}
}