import { IsEnum, IsOptional, IsUUID } from 'class-validator';

export enum AgeGroup {
  TEENS = 'teens',
  TWENTIES = 'twenties',
  THIRTIES = 'thirties',
  FORTIES = 'forties',
  FIFTIES_PLUS = 'fifties_plus',
}

export class CreateSurveyDto {
  @IsEnum(AgeGroup)
  age_group: AgeGroup;

  @IsUUID()
  @IsOptional()
  artwork_id?: string;
}
