import { IsInt, IsOptional, IsString, Max, Min } from 'class-validator';

export class CreateReviewDto {
  @IsInt()
  @Min(1)
  @Max(5)
  star!: number;

  @IsOptional()
  @IsString()
  content?: string;

  @IsOptional()
  @IsString()
  nickname?: string;
}