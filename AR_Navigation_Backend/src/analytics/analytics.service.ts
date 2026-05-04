import { Injectable } from '@nestjs/common';
import { AnalyticsRepository } from './analytics.repository';

@Injectable()
export class AnalyticsService {
  constructor(private readonly analyticsRepository: AnalyticsRepository) {}

  getAgeGroupCounts() {
    return this.analyticsRepository.getAgeGroupCounts();
  }

  getTotalUserCount() {
    return this.analyticsRepository.getTotalUserCount();
  }

  getArtworkReviewStats() {
    return this.analyticsRepository.getArtworkReviewStats();
  }
}
