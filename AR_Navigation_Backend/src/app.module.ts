import { Module } from '@nestjs/common';
import { ConfigModule } from '@nestjs/config';
import { ThrottlerModule } from '@nestjs/throttler';
import { ServeStaticModule } from '@nestjs/serve-static';
import * as fs from 'fs';
import * as path from 'path';
import { AnalyticsModule } from './analytics/analytics.module';
import { ArtworkModule } from './artwork/artwork.module';
import { AuthModule } from './auth/auth.module';
import { AssetsModule } from './assets/assets.module';
import { ExhibitsModule } from './exhibits/exhibits.module';
import { MapModule } from './map/map.module';
import { ReviewModule } from './review/review.module';
import { RoutesModule } from './routes/routes.module';
import { SupabaseModule } from './supabase/supabase.module';
import { SurveyModule } from './survey/survey.module';
import { VisitorsModule } from './visitors/visitors.module';

const buildPath = path.join(process.cwd(), '..', 'AR_Navigation_All-Web', 'build');
const publicPath = path.join(process.cwd(), '..', 'AR_Navigation_All-Web', 'public');

// build 폴더 있으면 프로덕션 서빙, 없으면 개발용 public 폴더 서빙 (이미지 등 정적 파일)
let staticModules: any[] = [];
if (fs.existsSync(buildPath)) {
  // path-to-regexp v8 호환 문법: (.*)  →  {*path}
  staticModules = [ServeStaticModule.forRoot({ rootPath: buildPath, exclude: ['/api/{*path}'] })];
} else if (fs.existsSync(publicPath)) {
  staticModules = [
    ServeStaticModule.forRoot({
      rootPath: publicPath,
      exclude: ['/api/{*path}'],
      serveStaticOptions: { index: false }, // index.html 자동 서빙 비활성화
    }),
  ];
}

@Module({
  imports: [
    ConfigModule.forRoot({ isGlobal: true }),
    // Rate Limiting: 1분(60_000ms)당 IP당 최대 30회
    ThrottlerModule.forRoot([{ ttl: 60_000, limit: 30 }]),
    ...staticModules,
    SupabaseModule,
    AuthModule,
    ArtworkModule,
    MapModule,
    ReviewModule,
    SurveyModule,
    AnalyticsModule,
    RoutesModule,
    ExhibitsModule,
    AssetsModule,
    VisitorsModule,
  ],
})
export class AppModule {}
