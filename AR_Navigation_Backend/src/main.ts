import { NestFactory } from '@nestjs/core';
import { AppModule } from './app.module';
import { ValidationPipe } from '@nestjs/common';

async function bootstrap() {
  const app = await NestFactory.create(AppModule);

  app.useGlobalPipes(
    new ValidationPipe({
      transform: true,          // 문자열 → 숫자 자동 변환
      whitelist: true,          // DTO에 없는 값 제거
      forbidNonWhitelisted: true, // 이상한 값 있으면 400
    }),
  );

  const port = Number(process.env.PORT) || 3000;
  await app.listen(port, '0.0.0.0');
  console.log(`✅ Server is running on http://localhost:${port}`);
}
bootstrap();
