import { Module } from '@nestjs/common';
import { ConfigModule } from '@nestjs/config';
import { AnalyticsModule } from './analytics/analytics.module';
import { ArtworkModule } from './artwork/artwork.module';
import { AuthModule } from './auth/auth.module';
import { MapModule } from './map/map.module';
import { ReviewModule } from './review/review.module';
import { SupabaseModule } from './supabase/supabase.module';
import { SurveyModule } from './survey/survey.module';

@Module({
  imports: [
    ConfigModule.forRoot({ isGlobal: true }),
    SupabaseModule,
    AuthModule,
    ArtworkModule,
    MapModule,
    ReviewModule,
    SurveyModule,
    AnalyticsModule,
  ],
})
export class AppModule {}
