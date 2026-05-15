# NestJS 백엔드 작업 규칙

> `AR_Navigation_Backend/` 폴더 작업 시에만 이 파일을 참고한다.

---

## 파일 구조

```
AR_Navigation_Backend/src/
├── main.ts                 포트 3000, 글로벌 프리픽스 /api, CORS, ValidationPipe
├── app.module.ts           모든 모듈 임포트 + ServeStaticModule (빌드 폴더 존재 시만 로드)
├── auth/                   인증 (JWT + Passport)
│   ├── dto/login.dto.ts
│   ├── guards/jwt-auth.guard.ts
│   ├── guards/admin.guard.ts
│   └── strategies/jwt.strategy.ts
├── artwork/                전시물 CRUD + 이미지 업로드
│   └── dto/create-artwork.dto.ts
├── map/                    맵/평면도 에셋 관리
│   └── dto/create-map.dto.ts
├── routes/                 경로 목록 (Unity 앱용)
│   └── dto/create-route.dto.ts
├── exhibits/               전시물 목록 — artworks 테이블을 ExhibitDto로 변환 (Unity 앱용)
├── assets/                 맵/평면도 에셋 URL (Unity 앱용)
├── visitors/               방문자 등록
│   └── dto/create-visitor.dto.ts
├── review/                 리뷰 수집
│   └── dto/create-review.dto.ts
├── survey/                 방문자 연령대 조사
│   └── dto/create-survey.dto.ts
├── analytics/              통계 (전체 관리자 전용)
├── supabase/               Supabase 클라이언트 (@Global)
└── common/
    ├── decorators/raw-response.decorator.ts   @RawResponse() — Unity용 래핑 스킵
    ├── services/file-storage.service.ts       Supabase Storage 공통 업로드 서비스
    ├── dto/pagination.dto.ts
    ├── filters/http-exception.filter.ts
    └── interceptors/response.interceptor.ts
```

---

## 기술 스택

- NestJS 11.0.1 (TypeScript)
- Supabase (PostgreSQL + Storage)
- Passport JWT + bcryptjs
- `@nestjs/serve-static` — React 빌드 정적 서빙 (프로덕션, 빌드 폴더 존재 시만 활성화)

---

## 전역 응답 형식

**웹 대시보드용 엔드포인트** (ResponseInterceptor 자동 래핑):
```json
{ "success": true, "data": { ... } }
```

**Unity 앱용 엔드포인트** (`@RawResponse()` 데코레이터 적용 — 래핑 생략):
```json
[...] 또는 { "fileUrl": "...", "version": "..." }
```

**에러 응답** (HttpExceptionFilter):
```json
{
  "success": false,
  "error": {
    "statusCode": 403,
    "message": "...",
    "path": "/api/...",
    "timestamp": "..."
  }
}
```

> **React 클라이언트**: 응답 배열/객체를 꺼낼 때 반드시 `.data` 필드 참조.  
> 예: `const result = await res.json(); setItems(result.data);`
>
> **Unity 앱**: `@RawResponse()` 적용 엔드포인트는 래핑 없이 순수 JSON 반환.  
> `GetArrayAsync`가 응답을 `{"items": <raw>}` 형태로 감싸 파싱함.

---

## @RawResponse() 데코레이터

Unity 앱과 호환이 필요한 엔드포인트에 적용. `{ success, data }` 래핑을 건너뜀.

```typescript
import { RawResponse } from '../common/decorators/raw-response.decorator';

@Get()
@RawResponse()
findAll() { ... }   // 순수 배열 또는 객체 그대로 반환
```

적용 대상: `GET /routes`, `GET /exhibits`, `GET /assets/map`, `GET /assets/floor-plan`

---

## API 엔드포인트 전체 목록

> 모든 경로에 글로벌 프리픽스 `/api` 적용됨.

### 인증 (auth)

| Method | Path | Guard | 설명 |
|--------|------|-------|------|
| `POST` | `/api/auth/login` | 없음 | 관리자 로그인 → JWT 발급 |

**LoginDto**: `username (string)`, `password (string)`  
**응답**: `{ access_token: string }` (JWT payload: `{sub, username, role}`)

---

### 전시물 (artwork)

| Method | Path | Guard | 설명 |
|--------|------|-------|------|
| `GET` | `/api/artworks` | 없음 | 전체 목록 (created_at 내림차순) |
| `GET` | `/api/artworks/:id` | 없음 | 개별 조회 |
| `POST` | `/api/artworks` | **없음** | 전시물 생성 (multipart/form-data, 이미지 업로드 포함) |
| `PATCH` | `/api/artworks/:id` | JWT + Admin | 전시물 수정 |
| `DELETE` | `/api/artworks/:id` | **없음** | 전시물 삭제 |

> `POST`, `DELETE`의 가드가 제거된 이유: 웹 대시보드(React)에서 인증 없이 호출하기 때문.  
> 추후 웹 대시보드에 JWT 로그인이 구현되면 가드를 복원한다.

**CreateArtworkDto**:
```typescript
title: string         // 필수
description?: string
artist?: string       // 선택 (AR 앱 전용, 웹에서 미사용)
latitude?: number     // -90 ~ 90, 선택
longitude?: number    // -180 ~ 180, 선택
immersal_anchors?: object
image_url?: string    // 업로드 후 서버에서 자동 설정

// 웹 대시보드 전용
feature?: string
contents?: string
ar_marker_id?: string
pos_x?: string
pos_z?: string
floor_info?: string   // 기본값 'Museum 1F'
```

> `latitude`, `longitude`는 multipart 폼에서 빈 문자열로 올 수 있으므로  
> `@Transform`으로 빈 문자열 → `undefined` 변환 처리됨.

**이미지 업로드 흐름**:
1. `POST /api/artworks` 요청 시 `FileInterceptor('image')`로 파일 인터셉트
2. artwork 레코드 먼저 생성 (image_url 없이)
3. `FileStorageService.upload()`로 `artwork-images` 버킷에 업로드: `artwork-images/{id}/{timestamp}{ext}`
4. 공개 URL을 `image_url` 컬럼에 업데이트

**Supabase 테이블**: `artworks`  
**Supabase Storage 버킷**: `artwork-images` (Public)  
**업데이트 시**: `updated_at`을 현재 시간으로 자동 갱신.

---

### 경로 (routes) — Unity 앱 전용

| Method | Path | Guard | 설명 |
|--------|------|-------|------|
| `GET` | `/api/routes` | 없음 | 경로 목록 — **@RawResponse, 순수 배열** |
| `POST` | `/api/routes` | JWT + Admin | 경로 생성 |

**응답 형식** (GET): 순수 배열 `[RouteDto, ...]`
```json
[
  {
    "routeId": "route_all_stops",
    "routeName": "전체 순회 (A → B → C)",
    "destination": "전시품 C",
    "estimatedDistance": "약 5m",
    "estimatedTime": "약 3분",
    "waypoints": [
      { "x": 1.173, "y": 0, "z": -1.596, "displayName": "전시품 A", "instruction": "..." }
    ]
  }
]
```

**Supabase 테이블**: `routes`
```sql
route_id TEXT, route_name TEXT, destination TEXT, description TEXT,
estimated_distance TEXT, estimated_time TEXT, waypoints JSONB DEFAULT '[]'
```

> DB는 snake_case 저장, 서비스 레이어에서 camelCase로 변환 후 반환.

---

### 전시물 Unity용 (exhibits) — Unity 앱 전용

| Method | Path | Guard | 설명 |
|--------|------|-------|------|
| `GET` | `/api/exhibits` | 없음 | 전시물 목록 — **@RawResponse, 순수 배열** |

**응답 형식** (GET): 순수 배열 `[ExhibitDto, ...]`
```json
[
  {
    "exhibitId": "ar_marker_id 또는 UUID",
    "name": "전시품 제목",
    "artist": "작가",
    "hall": "floor_info",
    "docentText": "contents 또는 description",
    "x": 1.173, "y": 0, "z": -1.596
  }
]
```

> `artworks` 테이블을 ExhibitDto 형식으로 변환해 반환. 별도 테이블 없음.  
> `ExhibitsService`에서 매핑 처리, `ArtworkRepository` 주입.

---

### 에셋 (assets) — Unity 앱 전용

| Method | Path | Guard | 설명 |
|--------|------|-------|------|
| `GET` | `/api/assets/map` | 없음 | Immersal 맵 파일 URL+버전 — **@RawResponse** |
| `GET` | `/api/assets/floor-plan` | 없음 | 평면도 이미지 URL+버전 — **@RawResponse** |

**응답 형식**: `{ "fileUrl": "https://...", "version": "ISO타임스탬프" }`

> `maps` 테이블의 `map_type` 컬럼으로 구분:  
> `'immersal_map'` → `/api/assets/map`, `'floor_plan'` → `/api/assets/floor-plan`  
> **파일 미등록 시**: 404 예외 없이 `{ "fileUrl": "", "version": "" }` 반환.  
> Unity 앱은 `fileUrl`이 빈 문자열이면 다운로드 스킵 후 로컬 캐시 사용.

---

### 방문자 (visitors)

| Method | Path | Guard | 설명 |
|--------|------|-------|------|
| `POST` | `/api/visitors` | 없음 | 방문자 등록 (Unity 앱에서 호출) |
| `GET` | `/api/visitors` | JWT + Admin | 목록 조회 |

**CreateVisitorDto**:
```typescript
deviceId: string    // 필수 (SystemInfo.deviceUniqueIdentifier)
visitedAt?: string  // ISO 8601, 없으면 서버 현재 시각 사용
ageGroup?: string   // 연령대 문자열 (예: "20대")
```

**Supabase 테이블**: `visitors` (`device_id`, `visited_at`, `age_group`)

---

### 맵 (map)

| Method | Path | Guard | 설명 |
|--------|------|-------|------|
| `GET` | `/api/maps` | 없음 | 맵 목록 |
| `GET` | `/api/maps/:id` | 없음 | 개별 조회 |
| `POST` | `/api/maps` | JWT + Admin | 맵 생성 |
| `PATCH` | `/api/maps/:id` | JWT + Admin | 맵 수정 |
| `DELETE` | `/api/maps/:id` | JWT + Admin | 맵 삭제 |
| `POST` | `/api/maps/:id/upload` | JWT + Admin | 파일 업로드 (FileInterceptor) |

**파일 업로드**: `FileStorageService.upload()`로 `map-files` 버킷에 저장.  
**Supabase 테이블**: `maps` — `map_type TEXT DEFAULT 'immersal_map'` 컬럼 포함.

---

### 리뷰 (review)

| Method | Path | Guard | 설명 |
|--------|------|-------|------|
| `POST` | `/api/reviews` | 없음 | 리뷰 생성 |
| `GET` | `/api/reviews` | **없음** | 목록 조회 (선택: `?artwork_id=UUID`) |
| `DELETE` | `/api/reviews/:id` | JWT + Admin | 리뷰 삭제 |

**CreateReviewDto**:
```typescript
artwork_id?: string   // UUID, 선택
rating: number        // integer, 1~5, 필수
content?: string
nickname?: string     // 작성자 닉네임, 선택
```

**Supabase 테이블**: `reviews` — `nickname TEXT DEFAULT '익명'`, `artwork_id` NOT NULL 제거됨.

---

### 설문 (survey)

| Method | Path | Guard | 설명 |
|--------|------|-------|------|
| `POST` | `/api/surveys` | 없음 | 설문 생성 |
| `GET` | `/api/surveys` | JWT + Admin | 목록 조회 |

**CreateSurveyDto**:
```typescript
age_group: enum   // 'teens' | 'twenties' | 'thirties' | 'forties' | 'fifties_plus'
artwork_id?: string
```

> Unity 앱의 `ageGroup` 문자열("20대" 등)과 백엔드 enum 값("twenties" 등) 불일치 — 연동 전 매핑 확인 필요.

---

### 분석 (analytics) — 가드 임시 제거 상태 (인증 없이 호출 가능)

| Method | Path | 설명 |
|--------|------|------|
| `GET` | `/api/analytics/age-groups` | 연령대별 집계 — **visitors 테이블** 기준 |
| `GET` | `/api/analytics/user-count` | 총 방문자 수 — **visitors 테이블** 기준 |
| `GET` | `/api/analytics/artwork-stats` | 작품별 리뷰 수 및 평균 점수 |

**age-groups 응답 형식** (visitors.age_group 한국어 값 그대로 반환):
```json
{ "success": true, "data": { "20대": 45, "30대": 32, "10대": 15, "40대": 8 } }
```

> ⚠️ 이전에는 `surveys` 테이블 기준이었으나, 앱 데이터 수집이 `visitors` 테이블로 통합됨.  
> `surveys` 테이블과 `survey` 엔드포인트는 별도로 유지되지만 analytics에서는 미사용.

---

## 인증 / 가드 패턴

```typescript
@UseGuards(JwtAuthGuard)               // JWT 토큰 검증만
@UseGuards(JwtAuthGuard, AdminGuard)   // JWT 검증 + role === 'admin' 확인
```

- `AdminGuard`: `role !== 'admin'`이면 `ForbiddenException` 발생
- JWT 토큰 위치: `Authorization: Bearer <token>` 헤더

---

## FileStorageService (공통 파일 업로드)

`common/services/file-storage.service.ts` — artwork와 map 모듈에서 공유.

```typescript
upload(bucket: string, filePath: string, file: Multer.File, upsert = false): Promise<string>
buildPath(folder: string, id: string, originalname: string): string
// 반환: Supabase Storage 공개 URL
```

사용 모듈: `ArtworkModule`, `MapModule` — providers에 `FileStorageService` 추가 필요.

---

## React 정적 서빙 (ServeStaticModule)

`app.module.ts`에서 `build/` 폴더 존재 여부를 `fs.existsSync`로 체크:
- **build/ 없음 (개발)**: ServeStaticModule 로드 생략 → NestJS API만 실행
- **build/ 있음 (프로덕션)**: ServeStaticModule 활성화 → React + API 단일 서버

```
개발: NestJS(3000) + React dev(3001), package.json "proxy" 설정으로 /api 자동 전달
프로덕션: npm run build 후 NestJS 하나로 전체 서빙
```

---

## 환경변수

| 변수명 | 필수 | 설명 |
|--------|------|------|
| `SUPABASE_URL` | ✅ | `https://xxxx.supabase.co` (trailing slash 없이) |
| `SUPABASE_SERVICE_ROLE_KEY` | ✅ | Supabase service role key (anon key 아님) |
| `JWT_SECRET` | ✅ | JWT 서명 키 (32자 이상) |
| `JWT_EXPIRATION` | 선택 | 기본값 86400초 (1일) |
| `PORT` | 선택 | 기본값 3000 |

---

## Supabase 스키마 현황

| 테이블 | 주요 컬럼 |
|--------|-----------|
| `admins` | id, username, password_hash, role |
| `artworks` | id, title, artist, image_url, feature, contents, ar_marker_id, pos_x, pos_z, floor_info, map_index (INTEGER DEFAULT 0) |
| `maps` | id, name, immersal_map_id, file_url, map_type (`'immersal_map'`\|`'floor_plan'`) |
| `routes` | id, route_id, route_name, destination, estimated_distance, estimated_time, waypoints(JSONB) |
| `reviews` | id, artwork_id(nullable), rating, content, nickname(DEFAULT '익명') |
| `surveys` | id, age_group(enum), artwork_id(nullable) |
| `visitors` | id, device_id, visited_at, age_group |

**Storage 버킷**: `artwork-images` (Public), `map-files` (Public)

---

## 개발 주의사항

- 새 모듈 추가 시 NestJS 표준 구조: module / controller / service / repository / dto
- 관리자 전용 엔드포인트는 반드시 `JwtAuthGuard + AdminGuard` 적용
- **Unity 앱 호환 엔드포인트**는 반드시 `@RawResponse()` 데코레이터 적용
- `FileStorageService` 사용 시 모듈 providers 배열에 추가 필요
- `ArtworkModule`은 `ArtworkRepository`를 exports — `ExhibitsModule`에서 import해서 사용
