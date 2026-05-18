import { Injectable, UnauthorizedException } from '@nestjs/common';
import { JwtService } from '@nestjs/jwt';
import * as bcrypt from 'bcryptjs';
import { AuthRepository } from './auth.repository';
import { LoginDto } from './dto/login.dto';

@Injectable()
export class AuthService {
  constructor(
    private readonly authRepository: AuthRepository,
    private readonly jwtService: JwtService,
  ) {}

  // 로그인 성공 시 JWT 문자열 반환 — 컨트롤러에서 httpOnly 쿠키로 설정
  async login(dto: LoginDto): Promise<string> {
    const admin = await this.authRepository.findByUsername(dto.username);
    if (!admin) throw new UnauthorizedException('Invalid credentials');

    const isValid = await bcrypt.compare(dto.password, admin.password_hash);
    if (!isValid) throw new UnauthorizedException('Invalid credentials');

    const payload = { sub: admin.id, username: admin.username, role: admin.role };
    return this.jwtService.sign(payload);
  }
}
