# React 웹 대시보드 작업 규칙

> `AR_Navigation_All-Web/` 폴더 작업 시에만 이 파일을 참고한다.

---

## 파일 구조

```
AR_Navigation_All-Web/
├── public/
│   ├── index.html         Tailwind CSS CDN 설정 포함
│   └── images/            floor_1~3, floor_B1, mu1~4, arlogo, arPhoto, review, nophoto
├── src/
│   ├── index.js           BrowserRouter 래퍼
│   ├── App.jsx            라우트 정의
│   ├── App.css
│   └── pages/
│       ├── Home.jsx       메인 대시보드 (KPI, 지도, 차트, 전시품 테이블)
│       ├── Content.jsx    전시콘텐츠 관리 (CRUD, 이미지 업로드)
│       ├── Space.jsx      공간 관리 (SVG 맵 에디터)
│       └── User.jsx       사용자 리뷰 관리
└── server/                ⚠️ 구버전 Express 서버 (더 이상 사용 안 함)
    ├── server.js          (레거시 — NestJS로 통합됨)
    ├── db.js              (레거시 — MySQL 연결)
    └── package.json
```

> `server/` 폴더는 NestJS 통합 후 사용되지 않는다. 삭제하거나 무시할 것.

---

## 기술 스택

### 프론트엔드
- React 19.2.3
- React Router DOM 7.11.0
- react-scripts **5.0.1** (package.json에 명시 — `^0.0.0`은 설치 불가이므로 절대 사용 금지)
- Tailwind CSS (CDN, index.html에 inline 설정)
- Google Material Symbols Outlined (CDN 아이콘)
- 차트: 미구현 — 추가 시 Recharts 권장

> **패키지 설치 시 주의**: React 19가 CRA(react-scripts 5.x) 공식 지원 범위(React 18)를 초과하므로  
> 반드시 `npm install --legacy-peer-deps` 로 설치해야 함.

### 백엔드
> **✅ 통합 완료**: Express + MySQL → **NestJS + Supabase(PostgreSQL + Storage)** 단일 서버.
> Supabase 연동 및 스키마 마이그레이션 완료 상태.

- API 서버: `AR_Navigation_Backend/` (NestJS, 포트 3000)
- 이미지 저장: Supabase Storage `artwork-images` 버킷 (로컬 `uploads/` 폴더 대체)
- 개발 시 `package.json`의 `"proxy": "http://localhost:3000"` 설정으로
  React dev 서버(3001)의 `/api` 요청이 NestJS(3000)로 자동 전달됨

> **React에서 사용하지 않는 Unity 전용 API** (참고용):  
> `GET /api/routes`, `GET /api/exhibits`, `GET /api/assets/map`, `GET /api/assets/floor-plan`, `POST /api/visitors`  
> 이 엔드포인트들은 Unity 앱 전용이며 `@RawResponse()` 데코레이터로 래핑을 생략하므로 React에서 호출 시 `.data` 언래핑 불필요.

---

## 라우팅 (App.jsx)

```
/         → Home.jsx    메인 대시보드
/space    → Space.jsx   공간 관리
/content  → Content.jsx 전시콘텐츠 관리
/user     → User.jsx    사용자 리뷰 관리
```

---

## API 엔드포인트 (NestJS 기준)

> 모든 경로는 상대 URL (`/api/...`). 개발 시 proxy, 프로덕션 시 동일 오리진으로 동작.
> NestJS 응답은 `{ success: true, data: [...] }` 형태로 래핑됨 — 반드시 `.data`로 꺼낼 것.

### 전시물

| Method | Path | 인증 | 설명 |
|--------|------|------|------|
| `GET` | `/api/artworks` | 없음 | 전체 목록 |
| `POST` | `/api/artworks` | 없음 | 전시물 생성 (multipart/form-data) |
| `DELETE` | `/api/artworks/:id` | 없음 | 전시물 삭제 |

**응답 필드** (GET 아이템):
```
id            UUID (문자열)
title
feature
contents
ar_marker_id
pos_x
pos_z
floor_info
image_url     Supabase Storage 전체 URL (이전 image_path 대체)
created_at    ISO 타임스탬프
```

**POST 폼 필드** (multipart/form-data):
```
title, feature, contents, ar_marker_id, pos_x, pos_z, floor_info, image(파일)
```

### 리뷰

| Method | Path | 인증 | 설명 |
|--------|------|------|------|
| `GET` | `/api/reviews` | 없음 | 전체 목록 |

**응답 필드** (GET 아이템):
```
id            UUID (문자열, 이전 review_id 대체)
rating        integer 1~5 (이전 star 대체)
content
nickname      작성자 닉네임 (기본값 '익명')
created_at    ISO 타임스탬프
```

### 분석 (analytics)

> 가드 임시 제거 상태 — 인증 없이 호출 가능. 추후 JWT 로그인 구현 시 복원.

| Method | Path | 인증 | 설명 |
|--------|------|------|------|
| `GET` | `/api/analytics/user-count` | **없음** | visitors 테이블 총 방문자 수 |
| `GET` | `/api/analytics/artwork-stats` | **없음** | 작품별 리뷰 수 및 평균 점수 배열 |
| `GET` | `/api/analytics/age-groups` | **없음** | 연령대별 집계 — visitors 테이블 기준 |

**age-groups 응답 예시** (visitors.age_group 한국어 값 그대로 반환):
```json
{ "success": true, "data": { "20대": 45, "30대": 32, "10대": 15, "40대": 8 } }
```

> 키가 한국어이므로 `Home.jsx`에서 별도 `labelMap` 변환 없이 `key` 그대로 사용.

---

## 페이지별 기능 정리

### Home.jsx
- KPI 카드 (동적 데이터):
  - 총 관람객 수: `GET /api/analytics/user-count` → `r.data`
  - 등록된 전시품 수: `artifactsData.length` (artworks 목록 로드 후 자동)
  - AR 활성화 횟수: 데이터 소스 없음 — `"—"` 표시 (하드코딩 제거됨)
  - 사용자 만족도: `GET /api/analytics/artwork-stats` → 리뷰 수 가중 평균
- Museum 3D Map: 층별 지도 이미지 전환 (floor_B1, floor_1, floor_2, floor_3)
- 방문 연령대 차트: `GET /api/analytics/age-groups` → visitors 테이블 기준 한국어 키 그대로 표시. 데이터 없으면 "설문 데이터 없음" 표시
- 전시품 목록 테이블: `GET /api/artworks` → `result.data`, 페이지네이션 5개/페이지

**Home.jsx 상태 변수**:
```javascript
const [artifactsData, setArtifactsData] = useState([]);  // 전시품 목록
const [userCount, setUserCount] = useState(null);         // 총 관람객 수
const [avgRating, setAvgRating] = useState(null);         // 평균 만족도
const [ageGroups, setAgeGroups] = useState([]);           // 연령대 배열 (정렬됨)
```

### Content.jsx
- 좌측: 전시품 목록 (썸네일, 삭제 버튼)
- 우측: 등록/수정 폼
  - 기본정보: title, feature, contents
  - 위치정보: ar_marker_id, pos_x, pos_z, floor_info (드롭다운)
  - 이미지: 클릭 업로드 + `URL.createObjectURL()` 미리보기
- 이미지 없을 때: `/images/nophoto.png`
- 날짜: `toLocaleString("ko-KR")`
- `DELETE` 시 `window.confirm()` 확인 다이얼로그

### Space.jsx
- SVG 기반 맵 에디터 (노드, POI, 경로 오버레이)
- 확대/축소 컨트롤
- 우측 속성 패널

### User.jsx
- 리뷰 목록 테이블: `GET /api/reviews`, 페이지네이션 10개/페이지
- 별점 렌더링: `★` (filled), `☆` (empty) — `item.rating` 값 사용
- 날짜: `toLocaleDateString()`
- `useLocation`으로 현재 경로 감지 → active 상태 표시

---

## 공통 컴포넌트 패턴

> 별도 components/ 폴더 없음. 모든 컴포넌트가 해당 페이지 파일 내에 인라인 함수로 정의되어 있다.

**SidebarClock** (전 페이지 포함):
- 1초 단위 실시간 시계 (`setInterval` / `clearInterval`)
- 형식: `YYYY.MM.DD (요일) HH:mm`

**KpiCard** (Home):
- 아이콘 모드: 제목, 값, Material Symbol 아이콘
- 이미지 모드: 썸네일, 제목, 설명

---

## 상태 관리

전역 상태 관리 라이브러리 없음. 페이지별 `useState` 로컬 상태.

**주요 상태 패턴 (Content.jsx)**:
```
마운트 → useEffect → GET /api/artworks → result.data → setItems()
항목 클릭 → handleSelectItem() → setFormData() + setPreviewImage(item.image_url)
이미지 선택 → handleImageChange() → URL.createObjectURL() → setPreviewImage()
등록하기 → handleSave() → FormData 생성 → POST /api/artworks → fetchItems()
```

**주요 상태 패턴 (Home.jsx)**:
```
마운트 → useEffect → Promise.all([
    GET /api/artworks              → result.data → setArtifactsData()
    GET /api/analytics/user-count  → r.data     → setUserCount()
    GET /api/analytics/artwork-stats → 가중평균  → setAvgRating()
    GET /api/analytics/age-groups  → 정렬·슬라이스 → setAgeGroups()
])
```

**NestJS 응답 언래핑 패턴**:
```javascript
const result = await response.json();
setItems(result.data);   // result.data가 실제 배열
```

---

## 스타일 가이드

### 색상 팔레트 (Tailwind 커스텀, index.html에 정의)
| 이름 | 값 |
|------|----|
| primary | #135bec |
| background-dark | #101622 |
| surface-dark | #1e2430 |
| surface-darker | #111318 |
| surface-hover | #2a3241 |
| background-light | #f6f6f8 |

### 레이아웃 패턴
- 전체 레이아웃: `h-screen w-full flex`
- 그리드: `grid-cols-1 md:grid-cols-2 lg:grid-cols-3/4`
- 사이드바 숨김: `md:flex`

---

## 개발 주의사항

- **인증 미구현** — 관리자 로그인/JWT 연동 작업 필요 시 NestJS 백엔드와 연동해야 함
- 이미지는 로컬 저장이 아닌 **Supabase Storage**에 저장됨 (`image_url`이 전체 URL)
  - 이전 코드의 `serverHost + item.image_path` 패턴 사용 금지
  - `item.image_url` 을 직접 `<img src>` 에 사용
- `item.id`는 UUID 문자열 (이전 MySQL의 정수 auto-increment가 아님)
- 리뷰의 별점 필드는 `item.rating` (이전 `item.star` 아님)
- 리뷰의 고유 키는 `item.id` (이전 `item.review_id` 아님)
- 분석 차트 구현 시 Recharts 설치: `npm install recharts`
- `GET /api/analytics/*` 는 가드 임시 제거 상태. JWT 로그인 구현 후 복원 필요
- `analytics.controller.ts`에서 가드 복원 시 `AnalyticsModule`의 `AuthModule` import는 이미 존재하므로 가드만 추가하면 됨

## 실행 방법

**최초 설치** (node_modules 없을 때):
```bash
cd AR_Navigation_All-Web
npm install --legacy-peer-deps   # React 19 + react-scripts 5.x 호환성 문제로 --legacy-peer-deps 필수
```

**개발 환경** (터미널 2개):
```bash
# 터미널 1: NestJS API 서버
cd AR_Navigation_Backend && npm run start:dev

# 터미널 2: React 개발 서버 (proxy로 API 자동 전달)
cd AR_Navigation_All-Web && npm start
```

**프로덕션** (NestJS 단독 서빙):
```bash
cd AR_Navigation_All-Web && npm run build
cd AR_Navigation_Backend && npm run start:prod
# → http://localhost:3000 에서 React UI + API 모두 서빙
```
