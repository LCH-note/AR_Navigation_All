import {
  Body,
  Controller,
  Delete,
  Get,
  Param,
  Post,
  Query,
  UseGuards,
} from '@nestjs/common';
import { SkipThrottle, ThrottlerGuard } from '@nestjs/throttler';
import { AdminGuard } from '../auth/guards/admin.guard';
import { JwtAuthGuard } from '../auth/guards/jwt-auth.guard';
import { CreateReviewDto } from './dto/create-review.dto';
import { ReviewService } from './review.service';

@Controller('reviews')
export class ReviewController {
  constructor(private readonly reviewService: ReviewService) {}

  // Unity 앱에서 인증 없이 리뷰 등록 — Rate Limiting 적용 (1분/30회)
  @Post()
  @UseGuards(ThrottlerGuard)
  create(@Body() dto: CreateReviewDto) {
    return this.reviewService.create(dto);
  }

  // 관리자 전용 — Throttle 제외
  @Get()
  @SkipThrottle()
  @UseGuards(JwtAuthGuard, AdminGuard)
  findAll(@Query('artwork_id') artworkId?: string) {
    if (artworkId) return this.reviewService.findByArtwork(artworkId);
    return this.reviewService.findAll();
  }

  // 관리자 전용 — Throttle 제외
  @Delete(':id')
  @SkipThrottle()
  @UseGuards(JwtAuthGuard, AdminGuard)
  remove(@Param('id') id: string) {
    return this.reviewService.remove(id);
  }
}
