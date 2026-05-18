import { Injectable } from '@nestjs/common';
import * as path from 'path';
import { SupabaseService } from '../../supabase/supabase.service';

@Injectable()
export class FileStorageService {
  constructor(private readonly supabase: SupabaseService) {}

  async upload(
    bucket: string,
    filePath: string,
    file: Express.Multer.File,
    upsert = false,
  ): Promise<string> {
    const { error } = await this.supabase.db.storage
      .from(bucket)
      .upload(filePath, file.buffer, { contentType: file.mimetype, upsert });
    if (error) throw error;

    const { data } = this.supabase.db.storage.from(bucket).getPublicUrl(filePath);
    return data.publicUrl;
  }

  buildPath(folder: string, id: string, originalname: string): string {
    const ext = path.extname(originalname);
    return `${folder}/${id}/${Date.now()}${ext}`;
  }
}
