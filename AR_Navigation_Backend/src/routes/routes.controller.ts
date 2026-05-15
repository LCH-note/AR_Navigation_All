import { Body, Controller, Delete, Get, Param, Post } from '@nestjs/common';
import { RawResponse } from '../common/decorators/raw-response.decorator';
import { CreateRouteDto } from './dto/create-route.dto';
import { RoutesService } from './routes.service';

@Controller('routes')
export class RoutesController {
  constructor(private readonly routesService: RoutesService) {}

  // Unity 앱이 직접 호출 — { success, data } 래핑 없이 순수 배열 반환
  @Get()
  @RawResponse()
  findAll() {
    return this.routesService.findAll();
  }

  @Post()
  create(@Body() dto: CreateRouteDto) {
    return this.routesService.create(dto);
  }

  // 가드 제거: 웹 대시보드에서 인증 없이 호출 (추후 JWT 구현 시 복원)
  @Delete(':id')
  remove(@Param('id') id: string) {
    return this.routesService.remove(id);
  }
}
