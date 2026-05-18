import { IsInt, IsOptional, IsString, IsUUID, Max, MaxLength, Min } from 'class-validator';

export class CreateReviewDto {
  // 웹 대시보드 리뷰는 특정 전시품 없이 등록 가능
  @IsUUID()
  @IsOptional()
  artwork_id?: string;

  // 별점 1~5 정수
  @IsInt()
  @Min(1)
  @Max(5)
  rating: number;

  // 리뷰 본문 — 최대 500자
  @IsString()
  @MaxLength(500)
  @IsOptional()
  content?: string;

  // 작성자 닉네임 — 최대 50자
  @IsString()
  @MaxLength(50)
  @IsOptional()
  nickname?: string;
}
