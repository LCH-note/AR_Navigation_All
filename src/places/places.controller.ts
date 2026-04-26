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
import { CreateReviewDto } from './dto/create-review.dto';

@Controller()
export class PlacesController {
  constructor(private readonly placesService: PlacesService) {}

  @Post('facilities/:facilityId/places')
  create(
    @Param('facilityId', ParseIntPipe) facilityId: number,
    @Body() dto: CreatePlaceDto,
  ) {
    return this.placesService.create(facilityId, dto);
  }

  @Get('facilities/:facilityId/places')
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

  @Get('places/:id')
  findOne(@Param('id', ParseIntPipe) id: number) {
    return this.placesService.findOne(id);
  }

  @Post('places/:id/reviews')
  createReview(
    @Param('id', ParseIntPipe) id: number,
    @Body() dto: CreateReviewDto,
  ) {
    return this.placesService.createReview(id, dto);
  }

  @Get('places/:id/reviews')
  findReviews(@Param('id', ParseIntPipe) id: number) {
    return this.placesService.findReviews(id);
  }
}