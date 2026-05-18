import { Injectable, UnauthorizedException } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { PassportStrategy } from '@nestjs/passport';
import { Request } from 'express';
import { ExtractJwt, Strategy } from 'passport-jwt';

export interface JwtPayload {
  sub: string;
  username: string;
  role: string;
}

@Injectable()
export class JwtStrategy extends PassportStrategy(Strategy) {
  constructor(configService: ConfigService) {
    super({
      jwtFromRequest: ExtractJwt.fromExtractors([
        // 1순위: httpOnly 쿠키에서 추출 (웹 대시보드)
        (req: Request) => req?.cookies?.['access_token'] ?? null,
        // 2순위: Authorization 헤더 폴백 (Unity 앱 등 쿠키 불가 클라이언트용)
        ExtractJwt.fromAuthHeaderAsBearerToken(),
      ]),
      ignoreExpiration: false,
      secretOrKey: configService.getOrThrow<string>('JWT_SECRET'),
      passReqToCallback: false,
    });
  }

  validate(payload: JwtPayload): JwtPayload {
    if (!payload.sub) throw new UnauthorizedException();
    return payload;
  }
}
