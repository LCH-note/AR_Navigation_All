import { BadRequestException, Injectable, NotFoundException } from '@nestjs/common';
import { PrismaService } from '../prisma/prisma.service';
import { CreatePlaceDto } from './dto/create-place.dto';
import { CreateReviewDto } from './dto/create-review.dto';

@Injectable()
export class PlacesService {
  constructor(private prisma: PrismaService) {}

  async create(facilityId: number, dto: CreatePlaceDto) {
    const facility = await this.prisma.facility.findUnique({
      where: { id: facilityId },
    });
    if (!facility) {
      throw new NotFoundException(`Facility ${facilityId} not found`);
    }

    if (dto.x === undefined || dto.y === undefined || dto.z === undefined) {
      throw new BadRequestException('Place requires x, y, z for indoor LOCAL_3D coordinates');
    }

    if (dto.anchorId !== undefined && dto.anchorId !== null) {
      const anchor = await this.prisma.anchor.findFirst({
        where: { id: dto.anchorId, facilityId },
        select: { id: true },
      });

      if (!anchor) {
        throw new BadRequestException(
          `anchorId ${dto.anchorId} is not in facility ${facilityId}`,
        );
      }
    }

    if (dto.nearestNodeId !== undefined && dto.nearestNodeId !== null) {
      const node = await this.prisma.graphNode.findFirst({
        where: { id: dto.nearestNodeId, facilityId },
        select: { id: true },
      });

      if (!node) {
        throw new BadRequestException(
          `nearestNodeId ${dto.nearestNodeId} is not in facility ${facilityId}`,
        );
      }
    }

    return this.prisma.place.create({
      data: {
        facilityId,
        name: dto.name,
        description: dto.description ?? null,

        feature: dto.feature ?? null,
        imagePath: dto.imagePath ?? null,
        arMarkerId: dto.arMarkerId ?? null,

        coordType: 'LOCAL_3D',

        x: dto.x,
        y: dto.y,
        z: dto.z,
        floor: dto.floor ?? null,
        anchorId: dto.anchorId ?? null,

        lat: null,
        lng: null,

        nearestNodeId: dto.nearestNodeId ?? null,
        category: dto.category ?? null,
      },
    });
  }

  findByFacility(
    facilityId: number,
    query?: { category?: string; floor?: number },
  ) {
    return this.prisma.place.findMany({
      where: {
        facilityId,
        category: query?.category,
        floor: query?.floor,
      },
      orderBy: { id: 'asc' },
      include: {
        anchor: true,
        reviews: true,
      },
    });
  }

  async findOne(id: number) {
    const place = await this.prisma.place.findUnique({
      where: { id },
      include: {
        anchor: true,
        reviews: true,
      },
    });

    if (!place) {
      throw new NotFoundException(`Place ${id} not found`);
    }

    return place;
  }

  async createReview(placeId: number, dto: CreateReviewDto) {
    const place = await this.prisma.place.findUnique({
      where: { id: placeId },
    });

    if (!place) {
      throw new NotFoundException(`Place ${placeId} not found`);
    }

    return this.prisma.review.create({
      data: {
        placeId,
        star: dto.star,
        content: dto.content,
        nickname: dto.nickname ?? '익명',
      },
    });
  }

  async findReviews(placeId: number) {
    return this.prisma.review.findMany({
      where: { placeId },
      orderBy: { createdAt: 'desc' },
    });
  }
}