import { Injectable } from '@nestjs/common';
import { SupabaseService } from '../supabase/supabase.service';

export interface Admin {
  id: string;
  username: string;
  password_hash: string;
  role: string;
}

@Injectable()
export class AuthRepository {
  constructor(private readonly supabase: SupabaseService) {}

  async findByUsername(username: string): Promise<Admin | null> {
    const { data, error } = await this.supabase.db
      .from('admins')
      .select('*')
      .eq('username', username)
      .single();

    if (error || !data) return null;
    return data as Admin;
  }
}
