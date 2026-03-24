import { IsInt, IsNumber, IsOptional, IsString, Min } from 'class-validator';

export class CreateAnchorDto {
  @IsString()
  cloudAnchorId: string;

  @IsNumber()
  localX: number;

  @IsNumber()
  localY: number;

  @IsNumber()
  localZ: number;

  @IsOptional()
  @IsNumber()
  localYawDeg?: number;

  @IsOptional()
  @IsInt()
  floor?: number;

  @IsOptional()
  @IsString()
  label?: string;

  @IsOptional()
  @IsString()
  note?: string;
}