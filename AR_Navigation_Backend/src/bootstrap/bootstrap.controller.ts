import { Controller, Get, Param } from '@nestjs/common';
import { BootstrapService } from './bootstrap.service';

@Controller('facilities/:facilityId/bootstrap')
export class BootstrapController {
  constructor(private readonly bootstrapService: BootstrapService) {}

  @Get()
  async getBootstrap(@Param('facilityId') facilityId: string) {
    return this.bootstrapService.getFacilityBootstrap(Number(facilityId));
  }
}