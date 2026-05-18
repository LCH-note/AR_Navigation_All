import cookieParser = require('cookie-parser');
import { ClassSerializerInterceptor, ValidationPipe } from '@nestjs/common';
import { NestFactory, Reflector } from '@nestjs/core';
import { AppModule } from './app.module';
import { AllExceptionsFilter } from './common/filters/http-exception.filter';
import { ResponseInterceptor } from './common/interceptors/response.interceptor';

async function bootstrap() {
  const app = await NestFactory.create(AppModule);

  app.setGlobalPrefix('api');

  // 허용 Origin: 환경변수로 관리, production에선 목록만, development에선 localhost 계열 추가 허용
  const allowedOrigins = (process.env.ALLOWED_ORIGINS ?? '')
    .split(',')
    .map((o) => o.trim())
    .filter(Boolean);

  const devOrigins = ['http://localhost:3000', 'http://localhost:3001'];

  const origins =
    process.env.NODE_ENV === 'production'
      ? allowedOrigins
      : [...allowedOrigins, ...devOrigins];

  app.enableCors({
    origin: origins.length > 0 ? origins : false,
    credentials: true, // 쿠키 기반 인증 연동
    methods: ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'OPTIONS'],
    allowedHeaders: ['Content-Type', 'Authorization'],
  });

  // httpOnly 쿠키 파싱 미들웨어
  app.use(cookieParser());

  app.useGlobalPipes(
    new ValidationPipe({ whitelist: true, forbidNonWhitelisted: true, transform: true }),
  );
  app.useGlobalFilters(new AllExceptionsFilter());
  app.useGlobalInterceptors(
    new ResponseInterceptor(app.get(Reflector)),
    // @Exclude() 데코레이터가 적용된 DTO의 민감 필드 자동 제외
    new ClassSerializerInterceptor(app.get(Reflector)),
  );

  const port = process.env.PORT ?? 3000;
  await app.listen(port);
  console.log(`Server running on http://localhost:${port}/api`);
}

bootstrap();
