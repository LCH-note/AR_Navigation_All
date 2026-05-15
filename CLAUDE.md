# AR Navigation All — 코드베이스 가이드

## 파트별 작업 규칙

각 서브프로젝트 작업 시 해당 rules 파일을 추가로 참고한다. 다른 파트 작업 중에는 로드하지 않는다.

| 작업 대상 폴더           | 참고 파일                         |
| ------------------------ | --------------------------------- |
| `AR_Navigation_App/`     | `.claude/rules/unity_app.md`      |
| `AR_Navigation_All-Web/` | `.claude/rules/react_web.md`      |
| `AR_Navigation_Backend/` | `.claude/rules/nestjs_backend.md` |

---

## 프로젝트 개요

Immersal SDK 기반 실내 AR 네비게이션 시스템. 전시관 방문자가 AR 앱으로 전시품까지 길 안내를 받고, 방문 데이터가 웹 대시보드에 집계된다.

**모노리포 구조 — 3개 서브프로젝트:**

| 폴더                     | 기술                | 역할                |
| ------------------------ | ------------------- | ------------------- |
| `AR_Navigation_App/`     | Unity 6 + C#        | AR 앱 (Android/iOS) |
| `AR_Navigation_Backend/` | NestJS + TypeScript | REST API 서버       |
| `AR_Navigation_All-Web/` | React 19            | 관리자 웹 대시보드  |

---

## 응답 규칙 (필수 준수)

- **언어**: 한국어로만 응답
- **코드 주석**: 모든 코드에 한국어 주석 필수

---

## 1. AR 앱 (Unity)

### 경로

```
AR_Navigation_App/Assets/
├── Navigation/          경로 안내 핵심 로직
├── Network/             백엔드 HTTP 통신
├── Models/              3D 모델 및 Prefab
├── Maps/                Immersal 맵 메타데이터
└── Scenes/SampleScene   메인 씬
```

### 주요 스크립트

| 파일                                      | 줄수 | 역할                               |
| ----------------------------------------- | ---- | ---------------------------------- |
| `Navigation/ARNavigationController.cs`    | 837  | AR 경로 안내 핵심 컨트롤러         |
| `Navigation/NavRouteData.cs`              | 182  | 경로/전시품 데이터 모델 + 목업     |
| `Navigation/RouteSelectController.cs`     | 244  | 경로 선택 UI (UI Toolkit)          |
| `Navigation/RouteSelectUserController.cs` | —    | 사용자 커스텀 경로 선택            |
| `Navigation/UserReviewController.cs`      | —    | 리뷰 및 연령대 조사 UI             |
| `Network/ApiClient.cs`                    | 200  | HTTP 싱글톤 (GET/POST/Download)    |
| `Network/DataSyncManager.cs`              | 387  | 앱 시작 시 데이터 일괄 로드 & 캐싱 |
| `Network/ApiModels.cs`                    | —    | 백엔드 DTO 정의                    |

### ARNavigationController 핵심 동작

- **플로팅 화살표**: 카메라 앞 1.5m 고정, 다음 웨이포인트 방향으로 Y축 회전
- **경로 유도선**: LineRenderer로 현재 위치 → 남은 웨이포인트 연결
- **웨이포인트 도달 판정**: 2.0m 이내 접근 시 다음으로 진행
- **Immersal 좌표 변환**: 매 프레임 `XRSpace.TransformPoint(localPos)` → 월드 좌표
- **에디터 모드**: 시작 시점 카메라 기준 행렬 고정 (Immersal 비활성)

### 화살표 회전 보정

`ArrowModel.fbx`의 팁 방향 보정값:

```
arrowRotationOffset = (0, 0, -90)
```

실기기 확인 완료. 에디터에서 변경 시 이 값 유지.

### 좌표 시스템

- **Immersal 모드 (실기기)**: 웨이포인트는 XRSpace 기준 로컬 좌표. 매 프레임 동적 변환.
- **에디터 모드**: 시작 시점 카메라 로컬→월드 행렬 고정.
- **Immersal 맵 ID**: `144383` (oneroom)

### DataSyncManager 초기화 순서

```
1. GET /routes         → 경로 목록
2. GET /exhibits       → 전시물 목록
3. GET /assets/map     → 버전 체크 후 조건부 다운로드
4. GET /assets/floor-plan → 버전 체크 후 조건부 다운로드
```

캐싱: PlayerPrefs로 버전 관리, 에셋 fileUrl이 빈 문자열이면 다운로드 스킵.  
API 통신 실패 시 Mock 폴백 없음 — 빈 배열 반환.  
방문자 카운팅: PlayerPrefs + deviceId로 기기당 1회 제한, `POST /api/visitors`로 서버 저장.

### 주요 패키지

| 패키지                                 | 버전        |
| -------------------------------------- | ----------- |
| `com.immersal.core`                    | github/HEAD |
| `com.unity.xr.arfoundation`            | 5.2.0       |
| `com.unity.xr.arcore`                  | 5.2.0       |
| `com.unity.xr.arkit`                   | 5.2.0       |
| `com.unity.xr.interaction.toolkit`     | 3.1.2       |
| `com.unity.ai.navigation`              | 1.1.5       |
| `com.unity.render-pipelines.universal` | 14.0.12     |
| `com.unity.textmeshpro`                | 3.0.9       |

---

## 2. 백엔드 (NestJS)

### 경로

```
AR_Navigation_Backend/src/
├── auth/          인증 (JWT)
├── artwork/       전시물 CRUD
├── map/           맵/평면도 에셋 관리
├── review/        리뷰 수집
├── survey/        방문자 조사 (연령대)
├── analytics/     통계
└── supabase/      Supabase 클라이언트
```

### 주요 API

| Method | Path                 | 설명                             |
| ------ | -------------------- | -------------------------------- |
| `POST` | `/auth/login`        | 관리자 로그인                    |
| `GET`  | `/routes`            | 경로 목록 (앱용)                 |
| `GET`  | `/artwork`           | 전시물 목록                      |
| `POST` | `/artwork`           | 전시물 생성 (관리자)             |
| `GET`  | `/assets/map`        | 맵 데이터 URL + 버전             |
| `GET`  | `/assets/floor-plan` | 평면도 URL + 버전                |
| `POST` | `/visitors`          | 방문자 등록 (deviceId, ageGroup) |
| `POST` | `/reviews`           | 리뷰 제출 (rating 1-5, comment)  |

### 기술 스택

- NestJS 11.0.1 (TypeScript)
- Supabase (PostgreSQL + Storage)
- Passport JWT + bcryptjs

---

## 3. 웹 대시보드 (React)

```
AR_Navigation_All-Web/src/
├── App.jsx         라우팅
└── pages/
    ├── Home.jsx    홈
    ├── Content.jsx 전시물 관리
    ├── Space.jsx   공간/맵 관리
    └── User.jsx    방문자 통계
```

- React 19.2.3 + React Router 7.11.0

---

## 4. 데이터 모델 요약

### NavRoute

```json
{
    "routeId": "route_all_stops",
    "routeName": "전체 순회 (A → B → C)",
    "estimatedDistance": "약 5m",
    "estimatedTime": "약 3분",
    "waypoints": [
        { "localPosition": { "x": 1.173, "y": 0, "z": -1.596 }, "displayName": "전시품 A", "instruction": "..." }
    ]
}
```

### Visitor (POST /visitors)

```json
{ "deviceId": "XXXX", "visitedAt": "2026-05-06T...", "ageGroup": "20대" }
```

> `ageGroup`: 한국어 문자열 그대로 저장 ("10대" ~ "60대 이상").  
> analytics `age-groups` / `user-count` 는 `visitors` 테이블 기준으로 집계.

### Review (POST /reviews)

```json
{ "rating": 4, "comment": "좋은 경험이었습니다" }
```

---

## 5. Git 브랜치 구조

| 브랜치              | 용도                         |
| ------------------- | ---------------------------- |
| `main`              | 메인 (기준)                  |
| `merge`             | 현재 작업 브랜치 (병합 작업) |
| `AR_Navigation_App` | 앱 개발 분기                 |

---

## 6. 개발 환경 주의사항

- Unity 씬은 `Assets/Scenes/SampleScene.unity` 하나
- 에디터에서 AR 기능 테스트 시 Immersal 비활성 모드로 동작
- 실기기 테스트 필수 기능: Immersal 측위, 화살표 방향, AR 카메라
- 백엔드 API 불통 시 `NavRouteData.cs`의 목업 데이터 자동 폴백
- `ArrowModel.fbx` 회전 보정값 `(0, 0, -90)` 반드시 유지
