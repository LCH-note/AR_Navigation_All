import { Controller, Get, Param, ParseIntPipe, Query } from '@nestjs/common';
import { PlacesService } from './places.service';
import { CoordType } from '@prisma/client';

@Controller('facilities/:facilityId/places')
export class PlacesController {
  constructor(private readonly placesService: PlacesService) {}

  @Get()
  findAll(
    @Param('facilityId', ParseIntPipe) facilityId: number,
    @Query('category') category?: string,
    @Query('floor') floorStr?: string,
    @Query('coordType') coordTypeStr?: string,
  ) {
    const floor = floorStr !== undefined ? Number(floorStr) : undefined;
    const coordType =
      coordTypeStr === 'LOCAL_3D' || coordTypeStr === 'GPS'
        ? (coordTypeStr as CoordType)
        : undefined;

    return this.placesService.findByFacility(facilityId, { category, floor, coordType });
  }
}