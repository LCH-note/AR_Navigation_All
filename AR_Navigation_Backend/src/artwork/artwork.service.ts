import { Injectable, NotFoundException } from '@nestjs/common';
import { RoutesService } from '../routes/routes.service';
import { ArtworkRepository } from './artwork.repository';
import { CreateArtworkDto } from './dto/create-artwork.dto';
import { UpdateArtworkDto } from './dto/update-artwork.dto';

@Injectable()
export class ArtworkService {
  constructor(
    private readonly artworkRepository: ArtworkRepository,
    private readonly routesService: RoutesService,
  ) {}

  findAll() {
    return this.artworkRepository.findAll();
  }

  async findOne(id: string) {
    const artwork = await this.artworkRepository.findById(id);
    if (!artwork) throw new NotFoundException(`Artwork ${id} not found`);
    return artwork;
  }

  async create(dto: CreateArtworkDto, file?: Express.Multer.File) {
    const artwork = await this.artworkRepository.create(dto);
    if (file) {
      const imageUrl = await this.artworkRepository.uploadImage(
        artwork.id,
        file,
      );
      return this.artworkRepository.update(artwork.id, { image_url: imageUrl });
    }
    return artwork;
  }

  async update(id: string, dto: UpdateArtworkDto, file?: Express.Multer.File) {
    await this.findOne(id);
    let updated: any;
    // 새 이미지가 업로드된 경우 Storage에 저장 후 URL 갱신
    if (file) {
      const imageUrl = await this.artworkRepository.uploadImage(id, file);
      updated = await this.artworkRepository.update(id, { ...dto, image_url: imageUrl });
    } else {
      updated = await this.artworkRepository.update(id, dto);
    }
    // 전시품 위치/맵 정보 변경 시 routes 웨이포인트 자동 동기화
    await this.routesService.syncWaypointsFromArtwork(updated);
    return updated;
  }

  async remove(id: string) {
    await this.findOne(id);
    await this.artworkRepository.delete(id);
    return { message: 'Artwork deleted successfully' };
  }
}
