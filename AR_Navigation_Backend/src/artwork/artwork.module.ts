import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module';
import { ArtworkController } from './artwork.controller';
import { ArtworkRepository } from './artwork.repository';
import { ArtworkService } from './artwork.service';

@Module({
  imports: [AuthModule],
  controllers: [ArtworkController],
  providers: [ArtworkService, ArtworkRepository],
})
export class ArtworkModule {}
