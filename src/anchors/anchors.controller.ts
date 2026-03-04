import { Body, Controller, Get, Param, ParseIntPipe, Post } from '@nestjs/common';
import { AnchorsService } from './anchors.service';
import { CreateAnchorDto } from './dto/create-anchor.dto';

@Controller('facilities/:facilityId/anchors')
export class AnchorsController {
  constructor(private readonly anchorsService: AnchorsService) {}

  @Post()
  create(
    @Param('facilityId', ParseIntPipe) facilityId: number,
    @Body() dto: CreateAnchorDto,
  ) {
    return this.anchorsService.create(facilityId, dto);
  }

  @Get()
  findAll(@Param('facilityId', ParseIntPipe) facilityId: number) {
    return this.anchorsService.findByFacility(facilityId);
  }
}