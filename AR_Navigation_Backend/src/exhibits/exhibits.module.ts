import { Module } from '@nestjs/common';
import { ArtworkModule } from '../artwork/artwork.module';
import { ExhibitsController } from './exhibits.controller';
import { ExhibitsService } from './exhibits.service';

@Module({
  imports: [ArtworkModule],
  controllers: [ExhibitsController],
  providers: [ExhibitsService],
})
export class ExhibitsModule {}
