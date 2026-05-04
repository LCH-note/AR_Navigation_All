import { Injectable } from '@nestjs/common';
import { SupabaseService } from '../supabase/supabase.service';

@Injectable()
export class AnalyticsRepository {
  constructor(private readonly supabase: SupabaseService) {}

  async getAgeGroupCounts(): Promise<Record<string, number>> {
    const { data, error } = await this.supabase.db
      .from('surveys')
      .select('age_group');
    if (error) throw error;

    return (data ?? []).reduce<Record<string, number>>((acc, row) => {
      acc[row.age_group] = (acc[row.age_group] ?? 0) + 1;
      return acc;
    }, {});
  }

  async getTotalUserCount(): Promise<number> {
    const { count, error } = await this.supabase.db
      .from('surveys')
      .select('*', { count: 'exact', head: true });
    if (error) throw error;
    return count ?? 0;
  }

  async getArtworkReviewStats() {
    const { data, error } = await this.supabase.db
      .from('reviews')
      .select('artwork_id, rating, artworks(title, artist)');
    if (error) throw error;

    const stats = (data ?? []).reduce<
      Record<string, { count: number; totalRating: number; title: string; artist: string }>
    >((acc, row) => {
      if (!acc[row.artwork_id]) {
        const artwork = row.artworks as unknown as { title: string; artist: string } | null;
        acc[row.artwork_id] = {
          count: 0,
          totalRating: 0,
          title: artwork?.title ?? '',
          artist: artwork?.artist ?? '',
        };
      }
      acc[row.artwork_id].count += 1;
      acc[row.artwork_id].totalRating += row.rating;
      return acc;
    }, {});

    return Object.entries(stats).map(([artworkId, s]) => ({
      artwork_id: artworkId,
      title: s.title,
      artist: s.artist,
      review_count: s.count,
      average_rating: Math.round((s.totalRating / s.count) * 10) / 10,
    }));
  }
}
