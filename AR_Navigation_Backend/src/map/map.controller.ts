import {
  Body,
  Controller,
  Delete,
  Get,
  Param,
  Patch,
  Post,
  UploadedFile,
  UseInterceptors,
} from '@nestjs/common';
import { FileInterceptor } from '@nestjs/platform-express';
import { CreateMapDto } from './dto/create-map.dto';
import { UpdateMapDto } from './dto/update-map.dto';
import { MapService } from './map.service';

@Controller('maps')
export class MapController {
  constructor(private readonly mapService: MapService) {}

  @Get()
  findAll() {
    return this.mapService.findAll();
  }

  @Get(':id')
  findOne(@Param('id') id: string) {
    return this.mapService.findOne(id);
  }

  // 가드 제거: 웹 대시보드에서 인증 없이 호출 (artworks와 동일한 이유)
  // 추후 웹 대시보드에 JWT 로그인 구현 시 가드 복원
  @Post()
  create(@Body() dto: CreateMapDto) {
    return this.mapService.create(dto);
  }

  @Patch(':id')
  update(@Param('id') id: string, @Body() dto: UpdateMapDto) {
    return this.mapService.update(id, dto);
  }

  // 가드 제거: 웹 대시보드에서 인증 없이 호출
  @Delete(':id')
  remove(@Param('id') id: string) {
    return this.mapService.remove(id);
  }

  // 가드 제거: 웹 대시보드에서 인증 없이 호출
  @Post(':id/upload')
  @UseInterceptors(FileInterceptor('file'))
  uploadFile(
    @Param('id') id: string,
    @UploadedFile() file: Express.Multer.File,
  ) {
    return this.mapService.uploadFile(id, file);
  }
}
