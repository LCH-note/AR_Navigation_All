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

  create(dto: CreateArtworkDto) {
    return this.artworkRepository.create(dto);
  }

  async update(id: string, dto: UpdateArtworkDto) {
    await this.findOne(id);
    return this.artworkRepository.update(id, dto);
  }

  async remove(id: string) {
    await this.findOne(id);
    await this.artworkRepository.delete(id);
    return { message: 'Artwork deleted successfully' };
  }
}
