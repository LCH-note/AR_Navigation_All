-- ============================================================
-- AR Navigation Backend — Supabase 스키마 (실제 운영 DB 기준)
-- 마지막 갱신: 2026-05-21
-- ============================================================

-- ──────────────────────────────────────────────
-- 테이블
-- ──────────────────────────────────────────────

CREATE TABLE public.admins (
  id            UUID        NOT NULL DEFAULT gen_random_uuid(),
  username      TEXT        NOT NULL,
  password_hash TEXT        NOT NULL,
  role          TEXT        NOT NULL DEFAULT 'admin',
  created_at    TIMESTAMPTZ          DEFAULT NOW(),
  CONSTRAINT admins_pkey         PRIMARY KEY (id),
  CONSTRAINT admins_username_key UNIQUE      (username)
) TABLESPACE pg_default;

CREATE TABLE public.artworks (
  id               UUID             NOT NULL DEFAULT gen_random_uuid(),
  title            TEXT             NOT NULL,
  description      TEXT,
  artist           TEXT,
  latitude         DOUBLE PRECISION,
  longitude        DOUBLE PRECISION,
  immersal_anchors JSONB,
  image_url        TEXT,
  created_at       TIMESTAMPTZ               DEFAULT NOW(),
  updated_at       TIMESTAMPTZ               DEFAULT NOW(),
  -- 웹 대시보드 전용 필드
  feature          TEXT,
  contents         TEXT,
  ar_marker_id     TEXT,
  pos_x            TEXT,
  pos_z            TEXT,
  floor_info       TEXT                      DEFAULT 'Museum 1F',
  -- 듀얼 맵 지원: 0 = 맵 145962, 1 = 맵 145963
  map_index        INTEGER                   DEFAULT 0,
  CONSTRAINT artworks_pkey PRIMARY KEY (id)
) TABLESPACE pg_default;

CREATE TABLE public.maps (
  id              UUID        NOT NULL DEFAULT gen_random_uuid(),
  name            TEXT        NOT NULL,
  description     TEXT,
  immersal_map_id TEXT        NOT NULL,
  file_url        TEXT,
  metadata        JSONB,
  created_at      TIMESTAMPTZ          DEFAULT NOW(),
  updated_at      TIMESTAMPTZ          DEFAULT NOW(),
  -- 'immersal_map' | 'immersal_map_b' | 'floor_plan' | '3d_model'
  map_type        TEXT                 DEFAULT 'immersal_map',
  -- 층 정보: 'B1' | '1F' | '2F' | '3F' (floor_plan / 3d_model 구분용)
  floor           TEXT,
  CONSTRAINT maps_pkey PRIMARY KEY (id)
) TABLESPACE pg_default;

CREATE TABLE public.reviews (
  id         UUID        NOT NULL DEFAULT gen_random_uuid(),
  -- artwork_id 는 선택값 (Unity 앱에서 전체 리뷰 제출 시 null 가능)
  artwork_id UUID,
  rating     INTEGER     NOT NULL,
  content    TEXT,
  created_at TIMESTAMPTZ          DEFAULT NOW(),
  nickname   TEXT                 DEFAULT '익명',
  CONSTRAINT reviews_pkey        PRIMARY KEY (id),
  CONSTRAINT reviews_artwork_id_fkey FOREIGN KEY (artwork_id)
    REFERENCES artworks (id) ON DELETE CASCADE,
  CONSTRAINT reviews_rating_check CHECK (rating >= 1 AND rating <= 5)
) TABLESPACE pg_default;

CREATE TABLE public.routes (
  id                UUID        NOT NULL DEFAULT gen_random_uuid(),
  route_id          TEXT        NOT NULL,
  route_name        TEXT        NOT NULL,
  destination       TEXT,
  description       TEXT,
  estimated_distance TEXT,
  estimated_time    TEXT,
  -- 웨이포인트 예시: [{ "x": 1.17, "y": 0, "z": -1.60, "displayName": "...", "instruction": "...", "mapIndex": 0 }]
  waypoints         JSONB       NOT NULL DEFAULT '[]',
  created_at        TIMESTAMPTZ          DEFAULT NOW(),
  updated_at        TIMESTAMPTZ          DEFAULT NOW(),
  CONSTRAINT routes_pkey PRIMARY KEY (id)
) TABLESPACE pg_default;

CREATE TABLE public.surveys (
  id         UUID        NOT NULL DEFAULT gen_random_uuid(),
  age_group  TEXT        NOT NULL,
  artwork_id UUID,
  created_at TIMESTAMPTZ          DEFAULT NOW(),
  CONSTRAINT surveys_pkey           PRIMARY KEY (id),
  CONSTRAINT surveys_artwork_id_fkey FOREIGN KEY (artwork_id)
    REFERENCES artworks (id) ON DELETE SET NULL,
  CONSTRAINT surveys_age_group_check CHECK (
    age_group = ANY (ARRAY[
      'teens', 'twenties', 'thirties', 'forties', 'fifties_plus'
    ])
  )
) TABLESPACE pg_default;

-- 방문자 기록 (Unity 앱 POST /api/visitors 에서 저장)
-- age_group: 한국어 문자열 그대로 저장 ("10대", "20대", ... "60대 이상")
-- analytics/age-groups 및 analytics/user-count 의 집계 기준 테이블
CREATE TABLE public.visitors (
  id         UUID        NOT NULL DEFAULT gen_random_uuid(),
  device_id  TEXT        NOT NULL,
  visited_at TIMESTAMPTZ,
  age_group  TEXT,
  created_at TIMESTAMPTZ          DEFAULT NOW(),
  CONSTRAINT visitors_pkey PRIMARY KEY (id)
) TABLESPACE pg_default;

-- ──────────────────────────────────────────────
-- Storage 버킷
-- (Supabase 대시보드에서 생성하거나 아래 SQL 실행)
-- ──────────────────────────────────────────────

-- 전시물 이미지 버킷 (Public)
-- POST /api/artworks 이미지 업로드 시 사용: artwork-images/{id}/{timestamp}{ext}
INSERT INTO storage.buckets (id, name, public)
VALUES ('artwork-images', 'artwork-images', true)
ON CONFLICT (id) DO NOTHING;

-- 맵 파일 버킷 (Public)
-- Immersal 맵(.bytes), 평면도 이미지, 3D 전체도(.glb) 업로드 시 사용
INSERT INTO storage.buckets (id, name, public)
VALUES ('map-files', 'map-files', true)
ON CONFLICT (id) DO NOTHING;

-- ──────────────────────────────────────────────
-- 초기 관리자 계정 (배포 전 비밀번호 해시 교체 필수)
-- bcrypt 해시 생성: node -e "require('bcryptjs').hash('비밀번호', 12).then(console.log)"
-- ──────────────────────────────────────────────

INSERT INTO public.admins (username, password_hash, role)
VALUES ('admin', '$2b$12$0kz0xnzukhe1/VzsUITKZ.19thrhkkha9dPpMy.vYC8ktOjCCnet.', 'admin');
