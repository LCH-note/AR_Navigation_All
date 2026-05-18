import { IsDateString, IsNotEmpty, IsOptional, IsString, MaxLength } from 'class-validator';

export class CreateVisitorDto {
  // Unity SystemInfo.deviceUniqueIdentifier — 필수
  @IsNotEmpty()
  @IsString()
  @MaxLength(128)
  deviceId: string;

  // ISO 8601 형식 날짜 — 없으면 서버 현재 시각 사용
  @IsOptional()
  @IsDateString()
  visitedAt?: string;

  // 한국어 연령대 문자열 (예: "20대") — visitors 테이블 TEXT 컬럼
  @IsOptional()
  @IsString()
  @MaxLength(20)
  ageGroup?: string;
}
