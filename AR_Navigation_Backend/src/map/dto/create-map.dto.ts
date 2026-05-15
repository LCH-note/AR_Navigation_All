import { IsNotEmpty, IsObject, IsOptional, IsString } from 'class-validator';

export class CreateMapDto {
  @IsString()
  @IsNotEmpty()
  name: string;

  @IsString()
  @IsOptional()
  description?: string;

  // 웹 대시보드에서 floor_plan 등록 시 불필요하므로 선택값으로 변경
  @IsString()
  @IsOptional()
  immersal_map_id?: string;

  // 웹 대시보드에서 전송하는 맵 타입 (floor_plan | immersal_map | immersal_map_b)
  @IsString()
  @IsOptional()
  map_type?: string;

  // 평면도 맵이 속하는 플로어 (floor_plan 타입일 때 사용: 'B1' | '1F' | '2F' | '3F')
  @IsString()
  @IsOptional()
  floor?: string;

  @IsObject()
  @IsOptional()
  metadata?: Record<string, unknown>;
}
