import { Body, Controller, Get, Post, UseGuards } from '@nestjs/common';
import { SkipThrottle, ThrottlerGuard } from '@nestjs/throttler';
import { JwtAuthGuard } from '../auth/guards/jwt-auth.guard';
import { AdminGuard } from '../auth/guards/admin.guard';
import { CreateVisitorDto } from './dto/create-visitor.dto';
import { VisitorsService } from './visitors.service';

@Controller('visitors')
export class VisitorsController {
  constructor(private readonly visitorsService: VisitorsService) {}

  // Unity 앱에서 인증 없이 방문자 등록 — Rate Limiting 적용 (1분/30회)
  @Post()
  @UseGuards(ThrottlerGuard)
  create(@Body() dto: CreateVisitorDto) {
    return this.visitorsService.create(dto);
  }

  // 관리자 전용 — Throttle 제외
  @Get()
  @SkipThrottle()
  @UseGuards(JwtAuthGuard, AdminGuard)
  findAll() {
    return this.visitorsService.findAll();
  }
}
