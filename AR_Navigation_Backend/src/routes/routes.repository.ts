import { Injectable } from '@nestjs/common';
import { SupabaseService } from '../supabase/supabase.service';
import { CreateRouteDto } from './dto/create-route.dto';

@Injectable()
export class RoutesRepository {
  constructor(private readonly supabase: SupabaseService) {}

  async findAll() {
    const { data, error } = await this.supabase.db
      .from('routes')
      .select('*')
      .order('created_at', { ascending: true });
    if (error) throw error;
    return data;
  }

  async create(dto: CreateRouteDto) {
    const { data, error } = await this.supabase.db
      .from('routes')
      .insert(dto)
      .select()
      .single();
    if (error) throw error;
    return data;
  }

  async updateWaypoints(id: string, waypoints: any[]) {
    const { error } = await this.supabase.db
      .from('routes')
      .update({ waypoints, updated_at: new Date().toISOString() })
      .eq('id', id);
    if (error) throw error;
  }

  async delete(id: string) {
    const { error } = await this.supabase.db
      .from('routes')
      .delete()
      .eq('id', id);
    if (error) throw error;
  }
}
