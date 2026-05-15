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
import { ArtworkService } from './artwork.service';
import { CreateArtworkDto } from './dto/create-artwork.dto';
import { UpdateArtworkDto } from './dto/update-artwork.dto';

@Controller('artworks')
export class ArtworkController {
  constructor(private readonly artworkService: ArtworkService) {}

  @Get()
  findAll() {
    return this.artworkService.findAll();
  }

  @Get(':id')
  findOne(@Param('id') id: string) {
    return this.artworkService.findOne(id);
  }

  // 웹 대시보드에서 인증 없이 전시품 등록 가능 (multipart/form-data 지원)
  @Post()
  @UseInterceptors(FileInterceptor('image'))
  create(
    @Body() dto: CreateArtworkDto,
    @UploadedFile() file?: Express.Multer.File,
  ) {
    return this.artworkService.create(dto, file);
  }

  // 웹 대시보드에서 인증 없이 전시품 수정 가능 (JWT 구현 후 가드 복원)
  @Patch(':id')
  @UseInterceptors(FileInterceptor('image'))
  update(
    @Param('id') id: string,
    @Body() dto: UpdateArtworkDto,
    @UploadedFile() file?: Express.Multer.File,
  ) {
    return this.artworkService.update(id, dto, file);
  }

  // 웹 대시보드에서 인증 없이 삭제 가능
  @Delete(':id')
  remove(@Param('id') id: string) {
    return this.artworkService.remove(id);
  }
}
