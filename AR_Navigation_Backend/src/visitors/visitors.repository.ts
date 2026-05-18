import { Injectable } from '@nestjs/common';
import { SupabaseService } from '../supabase/supabase.service';
import { CreateVisitorDto } from './dto/create-visitor.dto';

@Injectable()
export class VisitorsRepository {
  constructor(private readonly supabase: SupabaseService) {}

  async create(dto: CreateVisitorDto) {
    const { data, error } = await this.supabase.db
      .from('visitors')
      .insert({
        device_id:  dto.deviceId,
        visited_at: dto.visitedAt ?? new Date().toISOString(),
        age_group:  dto.ageGroup,
      })
      .select()
      .single();
    if (error) throw error;
    return data;
  }

  async findAll() {
    const { data, error } = await this.supabase.db
      .from('visitors')
      .select('*')
      .order('visited_at', { ascending: false });
    if (error) throw error;
    return data;
  }
}
