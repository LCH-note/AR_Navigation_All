import {
  Body,
  Controller,
  Delete,
  Get,
  Param,
  Patch,
  Post,
  UploadedFile,
  UseGuards,
  UseInterceptors,
} from '@nestjs/common';
import { FileInterceptor } from '@nestjs/platform-express';
import { AdminGuard } from '../auth/guards/admin.guard';
import { JwtAuthGuard } from '../auth/guards/jwt-auth.guard';
import { multerImageOptions } from '../common/utils/upload.utils';
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

  @Post()
  @UseGuards(JwtAuthGuard, AdminGuard)
  // MIME Type(jpeg/png/svg) 및 최대 5MB 검증
  @UseInterceptors(FileInterceptor('image', multerImageOptions))
  create(
    @Body() dto: CreateArtworkDto,
    @UploadedFile() file?: Express.Multer.File,
  ) {
    return this.artworkService.create(dto, file);
  }

  @Patch(':id')
  @UseGuards(JwtAuthGuard, AdminGuard)
  @UseInterceptors(FileInterceptor('image', multerImageOptions))
  update(
    @Param('id') id: string,
    @Body() dto: UpdateArtworkDto,
    @UploadedFile() file?: Express.Multer.File,
  ) {
    return this.artworkService.update(id, dto, file);
  }

  @Delete(':id')
  @UseGuards(JwtAuthGuard, AdminGuard)
  remove(@Param('id') id: string) {
    return this.artworkService.remove(id);
  }
}
