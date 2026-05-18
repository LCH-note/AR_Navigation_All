import { Transform } from 'class-transformer';
import {
  IsNotEmpty,
  IsNumber,
  IsObject,
  IsOptional,
  IsString,
  Max,
  Min,
} from 'class-validator';

export class CreateArtworkDto {
  @IsString()
  @IsNotEmpty()
  title: string;

  @IsString()
  @IsOptional()
  description?: string;

  // AR 앱에서만 사용 (웹 대시보드에서는 선택)
  @IsString()
  @IsOptional()
  artist?: string;

  // 빈 문자열을 undefined로 변환하여 선택값 처리
  @Transform(({ value }) =>
    value === '' || value == null ? undefined : Number(value),
  )
  @IsNumber()
  @Min(-90)
  @Max(90)
  @IsOptional()
  latitude?: number;

  @Transform(({ value }) =>
    value === '' || value == null ? undefined : Number(value),
  )
  @IsNumber()
  @Min(-180)
  @Max(180)
  @IsOptional()
  longitude?: number;

  @IsObject()
  @IsOptional()
  immersal_anchors?: Record<string, unknown>;

  // 이미지 업로드 후 서버에서 자동 설정
  @IsString()
  @IsOptional()
  image_url?: string;

  // 웹 대시보드 전용 필드
  @IsString()
  @IsOptional()
  feature?: string;

  @IsString()
  @IsOptional()
  contents?: string;

  @IsString()
  @IsOptional()
  ar_marker_id?: string;

  @IsString()
  @IsOptional()
  pos_x?: string;

  @IsString()
  @IsOptional()
  pos_z?: string;

  // 멀티파트 폼에서 문자열로 전달되므로 정수로 변환 (기본값 0 = 맵 A)
  @Transform(({ value }) =>
    value === '' || value == null ? 0 : parseInt(value, 10),
  )
  @IsNumber()
  @Min(0)
  @IsOptional()
  map_index?: number;

  @IsString()
  @IsOptional()
  floor_info?: string;
}
