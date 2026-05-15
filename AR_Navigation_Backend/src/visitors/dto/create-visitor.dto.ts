import { IsOptional, IsString } from 'class-validator';

export class CreateVisitorDto {
  @IsString()
  deviceId: string;

  @IsOptional()
  @IsString()
  visitedAt?: string;

  @IsOptional()
  @IsString()
  ageGroup?: string;
}
