import { Injectable } from '@nestjs/common';
import { PrismaService } from '../prisma/prisma.service';
import type { FacilityType } from '@prisma/client';

@Injectable()
export class FacilitiesService {
  constructor(private prisma: PrismaService) {}

  create(input: {
    name: string;
    type?: FacilityType;
    centerLat?: number;
    centerLng?: number;
    description?: string;
  }) {
    return this.prisma.facility.create({
      data: {
        name: input.name,
        type: input.type ?? 'indoor',
        centerLat: input.centerLat,
        centerLng: input.centerLng,
        description: input.description,
      },
    });
  }

  findAll() {
    return this.prisma.facility.findMany();
  }

  findOne(id: number) {
    return this.prisma.facility.findUnique({ where: { id } });
  }
}