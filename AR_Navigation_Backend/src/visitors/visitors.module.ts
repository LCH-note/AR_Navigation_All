import { Module } from '@nestjs/common';
import { VisitorsController } from './visitors.controller';
import { VisitorsRepository } from './visitors.repository';
import { VisitorsService } from './visitors.service';

@Module({
  controllers: [VisitorsController],
  providers: [VisitorsService, VisitorsRepository],
})
export class VisitorsModule {}
