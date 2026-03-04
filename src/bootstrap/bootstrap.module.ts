import { Module } from '@nestjs/common';
import { BootstrapController } from './bootstrap.controller';
import { BootstrapService } from './bootstrap.service';
import { FacilitiesModule } from '../facilities/facilities.module';
import { MapAssetsModule } from '../map-assets/map-assets.module';
import { AnchorsModule } from '../anchors/anchors.module';
import { PlacesModule } from '../places/places.module';
import { GraphModule } from '../graph/graph.module';


@Module({
  imports: [FacilitiesModule, MapAssetsModule, AnchorsModule, PlacesModule, GraphModule],
  controllers: [BootstrapController],
  providers: [BootstrapService],
})
export class BootstrapModule {}
