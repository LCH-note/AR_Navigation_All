import { Injectable } from '@nestjs/common';
import { CreateRouteDto } from './dto/create-route.dto';
import { RoutesRepository } from './routes.repository';

@Injectable()
export class RoutesService {
  constructor(private readonly routesRepository: RoutesRepository) {}

  // Unity 앱 형식에 맞게 snake_case → camelCase 변환 후 반환
  async findAll() {
    const rows = await this.routesRepository.findAll();
    return rows.map((r) => ({
      id:                r.id,
      routeId:           r.route_id,
      routeName:         r.route_name,
      destination:       r.destination ?? '',
      description:       r.description ?? '',
      estimatedDistance: r.estimated_distance ?? '',
      estimatedTime:     r.estimated_time ?? '',
      waypoints:         r.waypoints ?? [],
    }));
  }

  create(dto: CreateRouteDto) {
    return this.routesRepository.create(dto);
  }

  async remove(id: string) {
    await this.routesRepository.delete(id);
    return { message: 'Route deleted successfully' };
  }
}
