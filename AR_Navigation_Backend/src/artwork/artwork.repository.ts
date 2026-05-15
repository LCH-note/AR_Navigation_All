import { Injectable } from '@nestjs/common';
import { FileStorageService } from '../common/services/file-storage.service';
import { SupabaseService } from '../supabase/supabase.service';
import { CreateArtworkDto } from './dto/create-artwork.dto';
import { UpdateArtworkDto } from './dto/update-artwork.dto';

@Injectable()
export class ArtworkRepository {
  constructor(
    private readonly supabase: SupabaseService,
    private readonly fileStorage: FileStorageService,
  ) {}

  async findAll() {
    const { data, error } = await this.supabase.db
      .from('artworks')
      .select('*')
      .order('created_at', { ascending: false });
    if (error) throw error;
    return data;
  }

  async findById(id: string) {
    const { data, error } = await this.supabase.db
      .from('artworks')
      .select('*')
      .eq('id', id)
      .single();
    if (error) return null;
    return data;
  }

  async create(dto: CreateArtworkDto) {
    const { data, error } = await this.supabase.db
      .from('artworks')
      .insert(dto)
      .select()
      .single();
    if (error) throw error;
    return data;
  }

  async update(id: string, dto: UpdateArtworkDto) {
    const { data, error } = await this.supabase.db
      .from('artworks')
      .update({ ...dto, updated_at: new Date().toISOString() })
      .eq('id', id)
      .select()
      .single();
    if (error) throw error;
    return data;
  }

  async delete(id: string) {
    const { error } = await this.supabase.db
      .from('artworks')
      .delete()
      .eq('id', id);
    if (error) throw error;
  }

  async uploadImage(artworkId: string, file: Express.Multer.File): Promise<string> {
    const filePath = this.fileStorage.buildPath('artwork-images', artworkId, file.originalname);
    return this.fileStorage.upload('artwork-images', filePath, file);
  }
}
