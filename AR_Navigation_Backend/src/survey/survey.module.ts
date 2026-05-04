import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module';
import { SurveyController } from './survey.controller';
import { SurveyRepository } from './survey.repository';
import { SurveyService } from './survey.service';

@Module({
  imports: [AuthModule],
  controllers: [SurveyController],
  providers: [SurveyService, SurveyRepository],
})
export class SurveyModule {}
