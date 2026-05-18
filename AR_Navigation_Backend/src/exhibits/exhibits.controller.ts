import { Controller, Get } from '@nestjs/common';
import { RawResponse } from '../common/decorators/raw-response.decorator';
import { ExhibitsService } from './exhibits.service';

@Controller('exhibits')
export class ExhibitsController {
  constructor(private readonly exhibitsService: ExhibitsService) {}

  @Get()
  @RawResponse()
  findAll() {
    return this.exhibitsService.findAll();
  }
}
