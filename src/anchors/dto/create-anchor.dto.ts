import { IsInt, IsNumber, IsOptional, IsString, MaxLength } from 'class-validator';

export class CreateAnchorDto {
  @IsString()
  @MaxLength(191)
  cloudAnchorId!: string;

  @IsNumber()
  localX!: number;

  @IsNumber()
  localY!: number;

  @IsNumber()
  localZ!: number;

  @IsOptional()
  @IsNumber()
  localYawDeg?: number;

  @IsOptional()
  @IsInt()
  floor?: number;

  @IsOptional()
  @IsString()
  @MaxLength(191)
  label?: string;

  @IsOptional()
  @IsString()
  note?: string;
}