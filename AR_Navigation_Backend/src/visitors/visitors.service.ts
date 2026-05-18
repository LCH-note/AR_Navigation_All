import { Injectable } from '@nestjs/common';
import { CreateVisitorDto } from './dto/create-visitor.dto';
import { VisitorsRepository } from './visitors.repository';

@Injectable()
export class VisitorsService {
  constructor(private readonly visitorsRepository: VisitorsRepository) {}

  create(dto: CreateVisitorDto) {
    return this.visitorsRepository.create(dto);
  }

  findAll() {
    return this.visitorsRepository.findAll();
  }
}
