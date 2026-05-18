import { Controller, Get } from '@nestjs/common';
import { AnalyticsService } from './analytics.service';

// 웹 대시보드에서 인증 없이 조회 — 추후 JWT 로그인 구현 시 가드 복원
@Controller('analytics')
export class AnalyticsController {
  constructor(private readonly analyticsService: AnalyticsService) {}

  @Get('age-groups')
  getAgeGroups() {
    return this.analyticsService.getAgeGroupCounts();
  }

  @Get('user-count')
  getUserCount() {
    return this.analyticsService.getTotalUserCount();
  }

  @Get('artwork-stats')
  getArtworkStats() {
    return this.analyticsService.getArtworkReviewStats();
  }
}
