import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module';
import { MapController } from './map.controller';
import { MapRepository } from './map.repository';
import { MapService } from './map.service';

@Module({
  imports: [AuthModule],
  controllers: [MapController],
  providers: [MapService, MapRepository],
})
export class MapModule {}
