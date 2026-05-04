import { Injectable, NotFoundException } from '@nestjs/common';
import { SupabaseService } from '../supabase/supabase.service';
import { CreateMapDto } from './dto/create-map.dto';
import { UpdateMapDto } from './dto/update-map.dto';
import { MapRepository } from './map.repository';

@Injectable()
export class MapService {
  constructor(
    private readonly mapRepository: MapRepository,
    private readonly supabase: SupabaseService,
  ) {}

  findAll() {
    return this.mapRepository.findAll();
  }

  async findOne(id: string) {
    const map = await this.mapRepository.findById(id);
    if (!map) throw new NotFoundException(`Map ${id} not found`);
    return map;
  }

  create(dto: CreateMapDto) {
    return this.mapRepository.create(dto);
  }

  async update(id: string, dto: UpdateMapDto) {
    await this.findOne(id);
    return this.mapRepository.update(id, dto);
  }

  async remove(id: string) {
    await this.findOne(id);
    await this.mapRepository.delete(id);
    return { message: 'Map deleted successfully' };
  }

  async uploadFile(id: string, file: Express.Multer.File) {
    await this.findOne(id);

    const fileName = `maps/${id}/${Date.now()}_${file.originalname}`;
    const { error } = await this.supabase.db.storage
      .from('map-files')
      .upload(fileName, file.buffer, { contentType: file.mimetype, upsert: true });

    if (error) throw error;

    const { data: { publicUrl } } = this.supabase.db.storage
      .from('map-files')
      .getPublicUrl(fileName);

    return this.mapRepository.updateFileUrl(id, publicUrl);
  }
}
