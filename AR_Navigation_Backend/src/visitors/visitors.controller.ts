import { Body, Controller, Get, Post } from '@nestjs/common';
import { CreateVisitorDto } from './dto/create-visitor.dto';
import { VisitorsService } from './visitors.service';

@Controller('visitors')
export class VisitorsController {
  constructor(private readonly visitorsService: VisitorsService) {}

  // Unity 앱에서 인증 없이 방문자 등록
  @Post()
  create(@Body() dto: CreateVisitorDto) {
    return this.visitorsService.create(dto);
  }

  @Get()
  findAll() {
    return this.visitorsService.findAll();
  }
}
