import { Injectable } from '@nestjs/common';
import { CreateSurveyDto } from './dto/create-survey.dto';
import { SurveyRepository } from './survey.repository';

@Injectable()
export class SurveyService {
  constructor(private readonly surveyRepository: SurveyRepository) {}

  findAll() {
    return this.surveyRepository.findAll();
  }

  create(dto: CreateSurveyDto) {
    return this.surveyRepository.create(dto);
  }
}
