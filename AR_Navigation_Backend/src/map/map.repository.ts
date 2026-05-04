import { Injectable } from '@nestjs/common';
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
    const { data, error } = await this.supabase.db
      .from('maps')
      .insert(dto)
      .select()
      .single();
    if (error) throw error;
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
