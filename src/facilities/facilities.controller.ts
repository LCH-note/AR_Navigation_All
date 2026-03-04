import { Body, Controller, Get, Param, Post } from '@nestjs/common';
import { FacilitiesService } from './facilities.service';
import type { FacilityType } from '@prisma/client';

@Controller('facilities')
export class FacilitiesController {
  constructor(private readonly facilitiesService: FacilitiesService) {}

  @Post()
  createFacility(
    @Body()
    body: {
      name: string;
      type: FacilityType;
      centerLat?: number;
      centerLng?: number;
    },
  ) {
    return this.facilitiesService.create(body);
  }

  @Get()
  getFacilities() {
    return this.facilitiesService.findAll();
  }

  @Get(':id')
  getFacility(@Param('id') id: string) {
    return this.facilitiesService.findOne(Number(id));
  }
}
