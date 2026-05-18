# Supabase 보안 설정 가이드

> AR Navigation All 프로젝트용 Supabase 보안 설정 문서  
> 배포 전 반드시 이 문서의 체크리스트를 확인하세요.

---

## 1. RLS(Row Level Security) 활성화 및 정책 예시

> Supabase 대시보드 → Table Editor → 각 테이블 → RLS 탭에서 활성화  
> 또는 SQL Editor에서 아래 SQL을 직접 실행하세요.

### 1.1 모든 테이블 RLS 활성화

```sql
-- 모든 테이블의 RLS 활성화
ALTER TABLE admins ENABLE ROW LEVEL SECURITY;
ALTER TABLE artworks ENABLE ROW LEVEL SECURITY;
ALTER TABLE routes ENABLE ROW LEVEL SECURITY;
ALTER TABLE reviews ENABLE ROW LEVEL SECURITY;
ALTER TABLE surveys ENABLE ROW LEVEL SECURITY;
ALTER TABLE visitors ENABLE ROW LEVEL SECURITY;
ALTER TABLE maps ENABLE ROW LEVEL SECURITY;
```

---

### 1.2 `admins` 테이블

- `SELECT`: `authenticated` 역할만 허용 (관리자 본인 정보 조회)
- `INSERT / UPDATE / DELETE`: `service_role`만 허용 (백엔드 서버에서만 조작)

```sql
-- admins: 조회는 인증된 사용자만
CREATE POLICY "admins_select_authenticated"
  ON admins FOR SELECT
  TO authenticated
  USING (true);

-- admins: INSERT/UPDATE/DELETE는 service_role만 (백엔드 전용)
-- service_role은 RLS를 우회하므로 별도 정책 불필요
-- anon 역할의 모든 접근 차단
CREATE POLICY "admins_deny_anon"
  ON admins FOR ALL
  TO anon
  USING (false);
```

---

### 1.3 `artworks` 테이블

- `SELECT`: 익명 포함 전체 허용 (전시물 공개 조회)
- `INSERT / UPDATE / DELETE`: `authenticated` 역할만 허용 (관리자)

```sql
-- artworks: 전체 공개 조회
CREATE POLICY "artworks_select_public"
  ON artworks FOR SELECT
  TO anon, authenticated
  USING (true);

-- artworks: 생성은 인증된 관리자만
CREATE POLICY "artworks_insert_authenticated"
  ON artworks FOR INSERT
  TO authenticated
  WITH CHECK (true);

-- artworks: 수정은 인증된 관리자만
CREATE POLICY "artworks_update_authenticated"
  ON artworks FOR UPDATE
  TO authenticated
  USING (true)
  WITH CHECK (true);

-- artworks: 삭제는 인증된 관리자만
CREATE POLICY "artworks_delete_authenticated"
  ON artworks FOR DELETE
  TO authenticated
  USING (true);
```

---

### 1.4 `routes` 테이블

- `SELECT`: 익명 포함 전체 허용 (Unity 앱 공개 조회)
- `INSERT / UPDATE / DELETE`: `authenticated` 역할만 허용

```sql
-- routes: 전체 공개 조회 (Unity 앱용)
CREATE POLICY "routes_select_public"
  ON routes FOR SELECT
  TO anon, authenticated
  USING (true);

-- routes: 생성은 인증된 관리자만
CREATE POLICY "routes_insert_authenticated"
  ON routes FOR INSERT
  TO authenticated
  WITH CHECK (true);

-- routes: 수정은 인증된 관리자만
CREATE POLICY "routes_update_authenticated"
  ON routes FOR UPDATE
  TO authenticated
  USING (true)
  WITH CHECK (true);

-- routes: 삭제는 인증된 관리자만
CREATE POLICY "routes_delete_authenticated"
  ON routes FOR DELETE
  TO authenticated
  USING (true);
```

---

### 1.5 `reviews` 테이블

```sql
-- reviews: 전체 공개 조회
CREATE POLICY "reviews_select_public"
  ON reviews FOR SELECT
  TO anon, authenticated
  USING (true);

-- reviews: 생성은 익명도 허용 (Unity 앱에서 인증 없이 리뷰 등록)
CREATE POLICY "reviews_insert_anon"
  ON reviews FOR INSERT
  TO anon, authenticated
  WITH CHECK (true);

-- reviews: 삭제는 인증된 관리자만
CREATE POLICY "reviews_delete_authenticated"
  ON reviews FOR DELETE
  TO authenticated
  USING (true);
```

---

### 1.6 `visitors` 테이블

```sql
-- visitors: 조회는 인증된 관리자만
CREATE POLICY "visitors_select_authenticated"
  ON visitors FOR SELECT
  TO authenticated
  USING (true);

-- visitors: 등록은 익명도 허용 (Unity 앱에서 인증 없이 방문자 등록)
CREATE POLICY "visitors_insert_anon"
  ON visitors FOR INSERT
  TO anon, authenticated
  WITH CHECK (true);
```

---

### 1.7 `maps` 테이블

```sql
-- maps: 조회는 공개 허용 (Unity 앱 에셋 URL 조회)
CREATE POLICY "maps_select_public"
  ON maps FOR SELECT
  TO anon, authenticated
  USING (true);

-- maps: 생성/수정/삭제는 인증된 관리자만
CREATE POLICY "maps_write_authenticated"
  ON maps FOR ALL
  TO authenticated
  USING (true)
  WITH CHECK (true);
```

---

## 2. Storage Policy 설정 가이드

> Supabase 대시보드 → Storage → 버킷 선택 → Policies 탭에서 설정

### 2.1 `artwork-images` 버킷

| 작업 | 허용 역할 |
|------|-----------|
| SELECT (다운로드) | anon, authenticated |
| INSERT (업로드) | authenticated |
| UPDATE | authenticated |
| DELETE | authenticated |

```sql
-- artwork-images: 다운로드 공개 허용
CREATE POLICY "artwork_images_public_download"
  ON storage.objects FOR SELECT
  TO anon, authenticated
  USING (bucket_id = 'artwork-images');

-- artwork-images: 업로드/수정/삭제는 인증된 관리자만
CREATE POLICY "artwork_images_authenticated_write"
  ON storage.objects FOR INSERT
  TO authenticated
  WITH CHECK (bucket_id = 'artwork-images');

CREATE POLICY "artwork_images_authenticated_update"
  ON storage.objects FOR UPDATE
  TO authenticated
  USING (bucket_id = 'artwork-images');

CREATE POLICY "artwork_images_authenticated_delete"
  ON storage.objects FOR DELETE
  TO authenticated
  USING (bucket_id = 'artwork-images');
```

---

### 2.2 `map-files` 버킷

| 작업 | 허용 역할 |
|------|-----------|
| SELECT (다운로드) | anon, authenticated |
| INSERT (업로드) | authenticated |
| UPDATE | authenticated |
| DELETE | authenticated |

```sql
-- map-files: 다운로드 공개 허용 (Unity 앱 맵 데이터 다운로드)
CREATE POLICY "map_files_public_download"
  ON storage.objects FOR SELECT
  TO anon, authenticated
  USING (bucket_id = 'map-files');

-- map-files: 업로드/수정/삭제는 인증된 관리자만
CREATE POLICY "map_files_authenticated_write"
  ON storage.objects FOR INSERT
  TO authenticated
  WITH CHECK (bucket_id = 'map-files');

CREATE POLICY "map_files_authenticated_update"
  ON storage.objects FOR UPDATE
  TO authenticated
  USING (bucket_id = 'map-files');

CREATE POLICY "map_files_authenticated_delete"
  ON storage.objects FOR DELETE
  TO authenticated
  USING (bucket_id = 'map-files');
```

---

## 3. 배포 전 보안 체크리스트

### 환경변수

- [ ] `SUPABASE_URL` — `.env` 파일에 설정, 코드에 하드코딩 금지
- [ ] `SUPABASE_SERVICE_ROLE_KEY` — 절대 프론트엔드에 노출 금지, `.gitignore`에 `.env` 포함 확인
- [ ] `JWT_SECRET` — 32자 이상 랜덤 문자열, 코드에 하드코딩 금지
- [ ] `ALLOWED_ORIGINS` — 실제 Vercel 도메인으로 설정 (예: `https://your-app.vercel.app`)
- [ ] `NODE_ENV=production` — 배포 환경에 설정

### Supabase 설정

- [ ] 모든 테이블 RLS 활성화 확인
- [ ] 각 테이블 Policy 적용 확인 (anon 역할 과도한 권한 없음)
- [ ] Storage 버킷 Policy 적용 확인
- [ ] Supabase 대시보드 → Settings → API → `anon` key와 `service_role` key 구분 확인
- [ ] `service_role` key가 프론트엔드 빌드에 포함되지 않음을 확인

### NestJS 백엔드

- [ ] CORS `ALLOWED_ORIGINS`에 실제 도메인만 포함
- [ ] JWT 쿠키 `secure: true` (HTTPS 환경에서 자동 적용)
- [ ] `sameSite: 'strict'` — CSRF 방어 확인
- [ ] Rate Limiting 활성화 확인 (`ThrottlerModule`)
- [ ] 파일 업로드 MIME Type 및 크기 제한 확인 (`upload.utils.ts`)
- [ ] `ValidationPipe` `whitelist: true` 적용 확인

### React 웹 대시보드

- [ ] `REACT_APP_` 접두사 환경변수에 시크릿 없음 확인
- [ ] `authFetch` 사용 시 `credentials: 'include'` 포함
- [ ] localStorage에 JWT 토큰 저장 없음 (로그인 플래그만 저장)
- [ ] `Content-Security-Policy` 헤더 설정 (Vercel `vercel.json` 또는 NestJS 미들웨어)

### Git & 배포

- [ ] `.env` 파일이 `.gitignore`에 포함됨
- [ ] `.env.example`에 실제 값 없음 (예시값만)
- [ ] Render / Vercel 환경변수에 실제 시크릿 설정
- [ ] `npm audit` 결과 high/critical 취약점 없음
