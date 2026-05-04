import { IsNotEmpty, IsObject, IsOptional, IsString } from 'class-validator';

export class CreateMapDto {
  @IsString()
  @IsNotEmpty()
  name: string;

  @IsString()
  @IsOptional()
  description?: string;

  @IsString()
  @IsNotEmpty()
  immersal_map_id: string;

  @IsObject()
  @IsOptional()
  metadata?: Record<string, unknown>;
}
