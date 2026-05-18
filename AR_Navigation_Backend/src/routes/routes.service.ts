import { Injectable } from '@nestjs/common';
import { plainToInstance } from 'class-transformer';
import { CreateRouteDto } from './dto/create-route.dto';
import { RouteResponseDto } from './dto/route-response.dto';
import { RoutesRepository } from './routes.repository';

@Injectable()
export class RoutesService {
  constructor(private readonly routesRepository: RoutesRepository) {}

  // Unity 앱 형식에 맞게 snake_case → camelCase 변환, RouteResponseDto로 직렬화 (DB 내부 필드 제외)
  async findAll(): Promise<RouteResponseDto[]> {
    const rows = await this.routesRepository.findAll();
    const mapped = rows.map((r) => ({
      id:                r.id,
      routeId:           r.route_id,
      routeName:         r.route_name,
      destination:       r.destination ?? '',
      description:       r.description ?? '',
      estimatedDistance: r.estimated_distance ?? '',
      estimatedTime:     r.estimated_time ?? '',
      waypoints:         r.waypoints ?? [],
    }));
    return plainToInstance(RouteResponseDto, mapped, { excludeExtraneousValues: true });
  }

  create(dto: CreateRouteDto) {
    return this.routesRepository.create(dto);
  }

  // artwork title 기준으로 모든 경로의 웨이포인트 좌표/맵인덱스를 동기화
  async syncWaypointsFromArtwork(artwork: {
    title: string;
    pos_x?: string | null;
    pos_z?: string | null;
    map_index?: number | null;
  }) {
    const routes = await this.routesRepository.findAll();

    for (const route of routes) {
      const waypoints: any[] = route.waypoints ?? [];
      let changed = false;

      const updated = waypoints.map((wp) => {
        if (wp.displayName !== artwork.title) return wp;

        changed = true;
        return {
          ...wp,
          x: parseFloat(artwork.pos_x ?? '0') || 0,
          z: parseFloat(artwork.pos_z ?? '0') || 0,
          mapIndex: artwork.map_index ?? 0,
        };
      });

      if (changed) {
        await this.routesRepository.updateWaypoints(route.id, updated);
      }
    }
  }

  async remove(id: string) {
    await this.routesRepository.delete(id);
    return { message: 'Route deleted successfully' };
  }
}
