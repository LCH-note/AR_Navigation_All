import { Injectable } from '@nestjs/common';
import { SupabaseService } from '../supabase/supabase.service';
import { CreateSurveyDto } from './dto/create-survey.dto';

@Injectable()
export class SurveyRepository {
  constructor(private readonly supabase: SupabaseService) {}

  async findAll() {
    const { data, error } = await this.supabase.db
      .from('surveys')
      .select('*')
      .order('created_at', { ascending: false });
    if (error) throw error;
    return data;
  }

  async create(dto: CreateSurveyDto) {
    const { data, error } = await this.supabase.db
      .from('surveys')
      .insert(dto)
      .select()
      .single();
    if (error) throw error;
    return data;
  }
}
