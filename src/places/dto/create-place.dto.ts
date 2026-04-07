import {
  IsInt,
  IsNumber,
  IsOptional,
  IsString,
  MaxLength,
} from 'class-validator';

export class CreatePlaceDto {
  @IsString()
  @MaxLength(191)
  name!: string;

  @IsOptional()
  @IsString()
  description?: string;

  @IsNumber()
  x!: number;

  @IsNumber()
  y!: number;

  @IsNumber()
  z!: number;

  @IsOptional()
  @IsInt()
  floor?: number;

  @IsOptional()
  @IsInt()
  anchorId?: number;

  @IsOptional()
  @IsInt()
  nearestNodeId?: number;

  @IsOptional()
  @IsString()
  @MaxLength(191)
  category?: string;
}