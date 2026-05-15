import { Injectable, NotFoundException } from '@nestjs/common';
import { FileStorageService } from '../common/services/file-storage.service';
import { CreateMapDto } from './dto/create-map.dto';
import { UpdateMapDto } from './dto/update-map.dto';
import { MapRepository } from './map.repository';

@Injectable()
export class MapService {
  constructor(
    private readonly mapRepository: MapRepository,
    private readonly fileStorage: FileStorageService,
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
    const filePath = this.fileStorage.buildPath('maps', id, file.originalname);
    const publicUrl = await this.fileStorage.upload('map-files', filePath, file, true);
    return this.mapRepository.updateFileUrl(id, publicUrl);
  }
}
