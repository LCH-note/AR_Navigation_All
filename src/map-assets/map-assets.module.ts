import { Module } from '@nestjs/common';
import { MapAssetsController } from './map-assets.controller';
import { MapAssetsService } from './map-assets.service';

@Module({
  controllers: [MapAssetsController],
  providers: [MapAssetsService],
  exports: [MapAssetsService],
})
export class MapAssetsModule {}
