import { Module } from '@nestjs/common';
import { ConfigModule } from '@nestjs/config';

import { AppController } from './app.controller';
import { AppService } from './app.service';

import { PrismaModule } from './prisma/prisma.module';
import { PlacesModule } from './places/places.module';
import { FacilitiesModule } from './facilities/facilities.module';
import { MapAssetsModule } from './map-assets/map-assets.module';
import { AnchorsModule } from './anchors/anchors.module';
import { BootstrapModule } from './bootstrap/bootstrap.module';
import { GraphModule } from './graph/graph.module';

@Module({
  imports: [
    ConfigModule.forRoot({ isGlobal: true }),
    PrismaModule,
    PlacesModule,
    FacilitiesModule,
    MapAssetsModule,
    AnchorsModule,
    BootstrapModule,
    GraphModule,
  ],
  controllers: [AppController],
  providers: [AppService],
})
export class AppModule {}