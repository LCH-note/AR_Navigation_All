import {
  IsEnum,
  IsInt,
  IsNumber,
  IsOptional,
  IsString,
  MaxLength,
} from 'class-validator';
import { CoordType } from '@prisma/client';

export class CreatePlaceDto {
  @IsString()
  @MaxLength(191)
  name: string;

  @IsOptional()
  @IsString()
  description?: string;

  @IsOptional()
  @IsEnum(CoordType)
  coordType?: CoordType;

  // LOCAL_3D
  @IsOptional()
  @IsNumber()
  x?: number;

  @IsOptional()
  @IsNumber()
  y?: number;

  @IsOptional()
  @IsNumber()
  z?: number;

  @IsOptional()
  @IsInt()
  floor?: number;

  @IsOptional()
  @IsInt()
  anchorId?: number;

  // GPS
  @IsOptional()
  @IsNumber()
  lat?: number;

  @IsOptional()
  @IsNumber()
  lng?: number;

  @IsOptional()
  @IsInt()
  nearestNodeId?: number;

  @IsOptional()
  @IsString()
  @MaxLength(191)
  category?: string;
}