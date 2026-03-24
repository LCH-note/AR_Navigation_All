import { Body, Controller, Get, Param, Post } from '@nestjs/common';
import { MapAssetsService } from './map-assets.service';

@Controller('facilities/:facilityId/map-assets')
export class MapAssetsController {
  constructor(private readonly mapAssetsService: MapAssetsService) {}

  @Post()
  create(
    @Param('facilityId') facilityId: string,
    @Body()
    body: {
      fileUrl: string;
      scale?: number;
      originX?: number;
      originY?: number;
      originZ?: number;
      rotYawDeg?: number;
    },
  ) {
    return this.mapAssetsService.create({
      facilityId: Number(facilityId),
      format: 'obj',
      fileUrl: body.fileUrl,
      scale: body.scale ?? 1,
      originX: body.originX ?? 0,
      originY: body.originY ?? 0,
      originZ: body.originZ ?? 0,
      rotYawDeg: body.rotYawDeg ?? 0,
    });
  }

  @Get()
  list(@Param('facilityId') facilityId: string) {
    return this.mapAssetsService.findByFacility(Number(facilityId));
  }
}
