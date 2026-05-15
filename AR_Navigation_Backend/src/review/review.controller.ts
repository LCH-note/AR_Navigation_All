import {
  Body,
  Controller,
  Delete,
  Get,
  Param,
  Post,
  Query,
} from '@nestjs/common';
import { CreateReviewDto } from './dto/create-review.dto';
import { ReviewService } from './review.service';

@Controller('reviews')
export class ReviewController {
  constructor(private readonly reviewService: ReviewService) {}

  @Post()
  create(@Body() dto: CreateReviewDto) {
    return this.reviewService.create(dto);
  }

  // 웹 대시보드에서 인증 없이 리뷰 목록 조회 가능
  @Get()
  findAll(@Query('artwork_id') artworkId?: string) {
    if (artworkId) return this.reviewService.findByArtwork(artworkId);
    return this.reviewService.findAll();
  }

  @Delete(':id')
  remove(@Param('id') id: string) {
    return this.reviewService.remove(id);
  }
}
