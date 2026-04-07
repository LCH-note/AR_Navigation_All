import {
  Body,
  Controller,
  Get,
  Param,
  ParseIntPipe,
  Post,
  Query,
} from '@nestjs/common';
import { PlacesService } from './places.service';
import { CreatePlaceDto } from './dto/create-place.dto';

@Controller('facilities/:facilityId/places')
export class PlacesController {
  constructor(private readonly placesService: PlacesService) {}

  @Post()
  create(
    @Param('facilityId', ParseIntPipe) facilityId: number,
    @Body() dto: CreatePlaceDto,
  ) {
    return this.placesService.create(facilityId, dto);
  }

  @Get()
  findAll(
    @Param('facilityId', ParseIntPipe) facilityId: number,
    @Query('category') category?: string,
    @Query('floor') floorStr?: string,
  ) {
    const floor = floorStr !== undefined ? Number(floorStr) : undefined;

    return this.placesService.findByFacility(facilityId, {
      category,
      floor,
    });
  }
}