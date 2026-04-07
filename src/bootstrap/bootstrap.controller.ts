import { Controller, Get, Param, ParseIntPipe } from '@nestjs/common';
import { BootstrapService } from './bootstrap.service';

@Controller('facilities/:facilityId/bootstrap')
export class BootstrapController {
  constructor(private readonly bootstrapService: BootstrapService) {}

  @Get()
  async getBootstrap(
    @Param('facilityId', ParseIntPipe) facilityId: number,
  ) {
    return this.bootstrapService.getFacilityBootstrap(facilityId);
  }
}