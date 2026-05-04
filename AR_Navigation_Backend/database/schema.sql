-- AR Navigation Backend — Supabase Schema

CREATE TABLE admins (
  id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  username      TEXT UNIQUE NOT NULL,
  password_hash TEXT NOT NULL,
  role          TEXT NOT NULL DEFAULT 'admin',
  created_at    TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE artworks (
  id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  title            TEXT NOT NULL,
  description      TEXT,
  artist           TEXT NOT NULL,
  latitude         DOUBLE PRECISION NOT NULL,
  longitude        DOUBLE PRECISION NOT NULL,
  immersal_anchors JSONB,
  image_url        TEXT,
  created_at       TIMESTAMPTZ DEFAULT NOW(),
  updated_at       TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE maps (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name            TEXT NOT NULL,
  description     TEXT,
  immersal_map_id TEXT NOT NULL,
  file_url        TEXT,
  metadata        JSONB,
  created_at      TIMESTAMPTZ DEFAULT NOW(),
  updated_at      TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE reviews (
  id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  artwork_id UUID REFERENCES artworks(id) ON DELETE CASCADE,
  rating     INTEGER NOT NULL CHECK (rating BETWEEN 1 AND 5),
  content    TEXT,
  created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE surveys (
  id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  age_group  TEXT NOT NULL CHECK (age_group IN ('teens','twenties','thirties','forties','fifties_plus')),
  artwork_id UUID REFERENCES artworks(id) ON DELETE SET NULL,
  created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Supabase Storage bucket for map files
-- Create manually in dashboard or via:
-- INSERT INTO storage.buckets (id, name, public) VALUES ('map-files', 'map-files', true);

-- Seed admin (password: change_me — run bcrypt hash and replace)
-- INSERT INTO admins (username, password_hash) VALUES ('admin', '$2a$12:...');
