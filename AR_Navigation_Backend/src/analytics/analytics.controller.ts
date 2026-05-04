import { Controller, Get, UseGuards } from '@nestjs/common';
import { AdminGuard } from '../auth/guards/admin.guard';
import { JwtAuthGuard } from '../auth/guards/jwt-auth.guard';
import { AnalyticsService } from './analytics.service';

@Controller('analytics')
@UseGuards(JwtAuthGuard, AdminGuard)
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
