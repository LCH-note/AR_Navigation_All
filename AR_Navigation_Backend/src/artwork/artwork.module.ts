import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module';
import { FileStorageService } from '../common/services/file-storage.service';
import { ArtworkController } from './artwork.controller';
import { ArtworkRepository } from './artwork.repository';
import { ArtworkService } from './artwork.service';

@Module({
  imports: [AuthModule],
  controllers: [ArtworkController],
  providers: [ArtworkService, ArtworkRepository, FileStorageService],
  exports: [ArtworkRepository],
})
export class ArtworkModule {}
