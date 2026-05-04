import { Injectable } from '@nestjs/common';
import { SupabaseService } from '../supabase/supabase.service';
import { CreateReviewDto } from './dto/create-review.dto';

@Injectable()
export class ReviewRepository {
  constructor(private readonly supabase: SupabaseService) {}

  async findAll() {
    const { data, error } = await this.supabase.db
      .from('reviews')
      .select('*, artworks(title, artist)')
      .order('created_at', { ascending: false });
    if (error) throw error;
    return data;
  }

  async findByArtworkId(artworkId: string) {
    const { data, error } = await this.supabase.db
      .from('reviews')
      .select('*')
      .eq('artwork_id', artworkId)
      .order('created_at', { ascending: false });
    if (error) throw error;
    return data;
  }

  async create(dto: CreateReviewDto) {
    const { data, error } = await this.supabase.db
      .from('reviews')
      .insert(dto)
      .select()
      .single();
    if (error) throw error;
    return data;
  }

  async delete(id: string) {
    const { error } = await this.supabase.db
      .from('reviews')
      .delete()
      .eq('id', id);
    if (error) throw error;
  }
}
