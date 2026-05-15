import { Injectable, InternalServerErrorException } from '@nestjs/common';
import { SupabaseService } from '../supabase/supabase.service';
import { CreateMapDto } from './dto/create-map.dto';
import { UpdateMapDto } from './dto/update-map.dto';

@Injectable()
export class MapRepository {
  constructor(private readonly supabase: SupabaseService) {}

  async findAll() {
    const { data, error } = await this.supabase.db
      .from('maps')
      .select('*')
      .order('created_at', { ascending: false });
    if (error) throw error;
    return data;
  }

  async findById(id: string) {
    const { data, error } = await this.supabase.db
      .from('maps')
      .select('*')
      .eq('id', id)
      .single();
    if (error) return null;
    return data;
  }

  async create(dto: CreateMapDto) {
    // undefined 필드를 제외하고 삽입 객체 구성
    // immersal_map_id가 DB에서 NOT NULL인 경우를 대비해 빈 문자열 기본값 설정
    const insertData: Record<string, unknown> = {
      name: dto.name,
      immersal_map_id: dto.immersal_map_id ?? '',
    };
    if (dto.description !== undefined) insertData.description = dto.description;
    if (dto.map_type !== undefined) insertData.map_type = dto.map_type;
    if (dto.floor !== undefined) insertData.floor = dto.floor;
    if (dto.metadata !== undefined) insertData.metadata = dto.metadata;

    const { data, error } = await this.supabase.db
      .from('maps')
      .insert(insertData)
      .select()
      .single();
    if (error) throw new InternalServerErrorException(error.message);
    return data;
  }

  async update(id: string, dto: UpdateMapDto) {
    const { data, error } = await this.supabase.db
      .from('maps')
      .update({ ...dto, updated_at: new Date().toISOString() })
      .eq('id', id)
      .select()
      .single();
    if (error) throw error;
    return data;
  }

  async updateFileUrl(id: string, fileUrl: string) {
    const { data, error } = await this.supabase.db
      .from('maps')
      .update({ file_url: fileUrl, updated_at: new Date().toISOString() })
      .eq('id', id)
      .select()
      .single();
    if (error) throw error;
    return data;
  }

  async delete(id: string) {
    const { error } = await this.supabase.db
      .from('maps')
      .delete()
      .eq('id', id);
    if (error) throw error;
  }
}
