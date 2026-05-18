import { Module } from '@nestjs/common';
import { RoutesController } from './routes.controller';
import { RoutesRepository } from './routes.repository';
import { RoutesService } from './routes.service';

@Module({
  controllers: [RoutesController],
  providers: [RoutesService, RoutesRepository],
  exports: [RoutesService],
})
export class RoutesModule {}
