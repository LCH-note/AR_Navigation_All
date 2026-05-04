import { Injectable } from '@nestjs/common';
import { ReviewRepository } from './review.repository';
import { CreateReviewDto } from './dto/create-review.dto';

@Injectable()
export class ReviewService {
  constructor(private readonly reviewRepository: ReviewRepository) {}

  findAll() {
    return this.reviewRepository.findAll();
  }

  findByArtwork(artworkId: string) {
    return this.reviewRepository.findByArtworkId(artworkId);
  }

  create(dto: CreateReviewDto) {
    return this.reviewRepository.create(dto);
  }

  async remove(id: string) {
    await this.reviewRepository.delete(id);
    return { message: 'Review deleted successfully' };
  }
}
