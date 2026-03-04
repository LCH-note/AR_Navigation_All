import { BadRequestException, Injectable, NotFoundException } from '@nestjs/common';
import { PrismaService } from '../prisma/prisma.service';
import { CreatePlaceDto } from './dto/create-place.dto';
import { CoordType } from '@prisma/client';

@Injectable()
export class PlacesService {
  constructor(private prisma: PrismaService) {}

  async create(facilityId: number, dto: CreatePlaceDto) {
    const facility = await this.prisma.facility.findUnique({ where: { id: facilityId } });
    if (!facility) throw new NotFoundException(`Facility ${facilityId} not found`);

    const coordType = dto.coordType ?? CoordType.LOCAL_3D;

    if (coordType === CoordType.LOCAL_3D) {
      if (dto.x === undefined || dto.y === undefined || dto.z === undefined) {
        throw new BadRequestException('coordType=LOCAL_3D requires x, y, z');
      }
    } else if (coordType === CoordType.GPS) {
      if (dto.lat === undefined || dto.lng === undefined) {
        throw new BadRequestException('coordType=GPS requires lat, lng');
      }
    }

    if (dto.anchorId !== undefined && dto.anchorId !== null) {
      const anchor = await this.prisma.anchor.findFirst({
        where: { id: dto.anchorId, facilityId },
        select: { id: true },
      });
      if (!anchor) {
        throw new BadRequestException(`anchorId ${dto.anchorId} is not in facility ${facilityId}`);
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
        description: dto.description,
        coordType,

        x: coordType === CoordType.LOCAL_3D ? dto.x : null,
        y: coordType === CoordType.LOCAL_3D ? dto.y : null,
        z: coordType === CoordType.LOCAL_3D ? dto.z : null,
        floor: dto.floor ?? null,
        anchorId: dto.anchorId ?? null,

        lat: coordType === CoordType.GPS ? dto.lat : null,
        lng: coordType === CoordType.GPS ? dto.lng : null,

        nearestNodeId: dto.nearestNodeId ?? null,
        category: dto.category ?? null,
      },
    });
  }

  findByFacility(
    facilityId: number,
    query?: { category?: string; floor?: number; coordType?: CoordType },
  ) {
    return this.prisma.place.findMany({
      where: {
        facilityId,
        category: query?.category,
        floor: query?.floor,
        coordType: query?.coordType,
      },
      orderBy: { id: 'asc' },
      include: {
        anchor: true,
      },
    });
  }
}