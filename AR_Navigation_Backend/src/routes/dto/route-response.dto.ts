import { Exclude, Expose } from 'class-transformer';

// routes 응답 직렬화 — DB 내부 메타데이터(created_at, updated_at 등) 자동 제외
@Exclude()
export class RouteResponseDto {
  @Expose()
  id: string;

  @Expose()
  routeId: string;

  @Expose()
  routeName: string;

  @Expose()
  destination: string;

  @Expose()
  description: string;

  @Expose()
  estimatedDistance: string;

  @Expose()
  estimatedTime: string;

  @Expose()
  waypoints: any[];
}
