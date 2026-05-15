import { IsInt, IsOptional, IsString, IsUUID, Max, Min } from 'class-validator';

export class CreateReviewDto {
  // 웹 대시보드 리뷰는 특정 전시품 없이 등록 가능
  @IsUUID()
  @IsOptional()
  artwork_id?: string;

  @IsInt()
  @Min(1)
  @Max(5)
  rating: number;

  @IsString()
  @IsOptional()
  content?: string;

  // 웹 대시보드용 작성자 닉네임
  @IsString()
  @IsOptional()
  nickname?: string;
}
