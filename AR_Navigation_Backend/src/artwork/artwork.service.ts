import { Injectable, NotFoundException } from '@nestjs/common';
import { ArtworkRepository } from './artwork.repository';
import { CreateArtworkDto } from './dto/create-artwork.dto';
import { UpdateArtworkDto } from './dto/update-artwork.dto';

@Injectable()
export class ArtworkService {
  constructor(private readonly artworkRepository: ArtworkRepository) {}

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
    // 새 이미지가 업로드된 경우 Storage에 저장 후 URL 갱신
    if (file) {
      const imageUrl = await this.artworkRepository.uploadImage(id, file);
      return this.artworkRepository.update(id, { ...dto, image_url: imageUrl });
    }
    return this.artworkRepository.update(id, dto);
  }

  async remove(id: string) {
    await this.findOne(id);
    await this.artworkRepository.delete(id);
    return { message: 'Artwork deleted successfully' };
  }
}
