import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module';
import { VisitorsController } from './visitors.controller';
import { VisitorsRepository } from './visitors.repository';
import { VisitorsService } from './visitors.service';

@Module({
  imports: [AuthModule],
  controllers: [VisitorsController],
  providers: [VisitorsService, VisitorsRepository],
})
export class VisitorsModule {}
