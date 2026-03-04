import { Injectable } from '@nestjs/common';
import { PrismaService } from '../prisma/prisma.service';
import { CreateAnchorDto } from './dto/create-anchor.dto';

@Injectable()
export class AnchorsService {
  constructor(private prisma: PrismaService) {}

  create(facilityId: number, dto: CreateAnchorDto) {
    return this.prisma.anchor.create({
      data: {
        facilityId,
        cloudAnchorId: dto.cloudAnchorId,
        localX: dto.localX,
        localY: dto.localY,
        localZ: dto.localZ,
        localYawDeg: dto.localYawDeg,
        floor: dto.floor,
        label: dto.label,
        note: dto.note,
      },
    });
  }

  findByFacility(facilityId: number) {
    return this.prisma.anchor.findMany({
      where: { facilityId },
      orderBy: { id: 'asc' },
    });
  }
}