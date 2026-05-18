import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module';
import { FileStorageService } from '../common/services/file-storage.service';
import { MapController } from './map.controller';
import { MapRepository } from './map.repository';
import { MapService } from './map.service';

@Module({
  imports: [AuthModule],
  controllers: [MapController],
  providers: [MapService, MapRepository, FileStorageService],
})
export class MapModule {}
