import { Injectable } from '@nestjs/common';
import { ArtworkRepository } from '../artwork/artwork.repository';

@Injectable()
export class ExhibitsService {
  constructor(private readonly artworkRepository: ArtworkRepository) {}

  async findAll() {
    const artworks = await this.artworkRepository.findAll();
    return artworks.map((a) => ({
      exhibitId:  a.ar_marker_id ?? a.id,
      name:       a.title,
      artist:     a.artist ?? '',
      hall:       a.floor_info ?? '',
      docentText: a.contents ?? a.description ?? '',
      imageUrl:   a.image_url ?? '',
      feature:    a.feature  ?? '',
      x:          parseFloat(a.pos_x) || 0,
      y:          0,
      z:          parseFloat(a.pos_z) || 0,
      mapIndex:   a.map_index ?? 0,   // 0 = 맵 A (145962), 1 = 맵 B (145963)
    }));
  }
}
