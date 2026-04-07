import { Injectable } from '@nestjs/common';
import { PrismaService } from '../prisma/prisma.service';

@Injectable()
export class FacilitiesService {
  constructor(private prisma: PrismaService) {}

  create(input: {
    name: string;
    description?: string;
  }) {
    return this.prisma.facility.create({
      data: {
        name: input.name,
        type: 'indoor',
        centerLat: null,
        centerLng: null,
        description: input.description ?? null,
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