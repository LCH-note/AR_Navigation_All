# 앞으로 해결해야 할 과제

> 업데이트: 2026-05-16 (지도화면 작업)  
> 현재 브랜치: `merge`

---

## 우선순위 높음

### [APP] Immersal 실내 측위 정밀도 검증
- **현황**: 듀얼 맵(145962, 145963) 씬 구성 완료, 실기기 현장 테스트 미확인
- **과제**: 두 맵 각각의 웨이포인트 도달 판정 정확도 및 맵 경계 전환 확인
- **체크포인트**: 2.0m 도달 반경이 공간에 맞는지 조정 필요 가능성

---

## 우선순위 중간

### [WEB] 관리자 인증 흐름 구현
- **현황**: 웹 대시보드에서 인증 미구현 상태. `POST /api/artworks`, `DELETE /api/artworks/:id`, `GET /api/reviews` 가드가 임시 제거됨
- **과제**: NestJS JWT 로그인 → 토큰 저장 → 요청 헤더 전송 흐름 구현 후 가드 복원
- **복원 대상 가드**: `POST /artworks`, `DELETE /artworks/:id`, `GET /reviews`, `DELETE /reviews/:id`
- **주의**: `DELETE /api/reviews/:id`에 리뷰 삭제 버튼이 추가됐으나 JWT 인증 구현 전까지는 403 반환됨

### [APP] 전시품 상세 도슨트 기능
- **현황**: `DocentManager.cs` 구현 완료. AR 화면에서 "도슨트 ON/OFF" 버튼으로 전체 전시품 패널 일괄 표시/숨김. 텍스트 렌더링 버그 수정 완료 (아래 완료 항목 참고).
- **잔여 과제**: 현재 구현은 전체 패널 동시 토글 방식. 원래 기획대로 웨이포인트 도달 시 해당 전시품 패널만 자동 표시하려면 `ARNavigationController.CheckWaypointReached()` → `DocentManager.ShowPanelForExhibit(exhibitId)` 연동 필요
- **주의**: `MalgunGothic SDF.asset`은 UIToolkit 전용 폰트로 `TMP_FontAsset` 필드에 연결 불가. `Docent Font TTF` 필드에 `MalgunGothic.ttf` 연결 유지

### [APP] 평면도(Floor Plan) 화면 연동
- **현황**: Unity 코드 완성 (`FloorMapController`, `MapScreen.uxml`, `DataSyncManager.LoadAllFloorPlansAsync` 모두 구현). 백엔드 `GET /api/assets/floor-plans` 구현됨.
- **잔여 과제**: Supabase `maps` 테이블에 `floor` TEXT 컬럼 추가 필요 (아래 SQL 실행)
  ```sql
  ALTER TABLE maps ADD COLUMN IF NOT EXISTS floor TEXT;
  ```
  이후 웹 대시보드 공간관리 > 맵 관리에서 2D 평면도 업로드 시 플로어(B1/1F/2F/3F)를 지정하면 앱 전체 지도 화면에 표시됨.
- **주의**: `maps.floor` 컬럼 없으면 `getFloorPlans()` 쿼리가 에러 또는 빈 배열 반환 → 앱 지도 화면 전층 "미등록" 표시

### [APP] NavMesh 경로 계산 현장 적용
- **현황**: NavMesh 베이킹 완료 (Integrated AR Map 바운드 기준). 실기기 현장 경로 계산 테스트 미확인
- **과제**: 실내 공간 장애물(벽, 전시케이스) NavMesh 반영 및 에이전트 파라미터 조정
- **씬 설정 현황**:
  - NavMeshOrigin: (-1.61, 0, -6.62), collectObjects = Children
  - NavFloor Scale: (6.654, 1, 7.085) → 실제 크기 66.54 × 70.85 m (AR Map 바운드 + 10% 마진)
  - 에디터 유틸리티: `Assets/Editor/NavMeshFitToARMap.cs` — "AR Navigation > Fit NavMesh to Integrated AR Map" 메뉴로 재조정 가능

### [WEB] 분석 대시보드 차트 구현
- **현황**: `User.jsx`에 방문자 통계 페이지 존재
- **과제**: 연령대별/시간대별 방문자 수 차트 (Recharts 등 라이브러리 활용)

---

## 우선순위 낮음 / 개선

### [APP] 에디터 시뮬레이션 모드 개선
- **현황**: 에디터 모드에서 카메라 기준 행렬이 시작 시점에 고정됨
- **과제**: 에디터에서도 카메라 이동에 따른 동적 좌표 업데이트 테스트 가능하도록 개선

### [APP] 방문자 카운팅 서버 검증
- **현황**: PlayerPrefs + deviceId 로컬 중복 방지, 기기 초기화 시 우회 가능
- **과제**: 서버 측에서 deviceId 중복 체크로 신뢰도 향상

### [APP] 다국어 지원 (한/영)
- **현황**: 전체 UI 한국어 고정
- **과제**: 외국인 관람객 대응을 위한 다국어 지원 검토

### [BACKEND] API 문서화 (Swagger)
- **현황**: NestJS에 Swagger 미설정
- **과제**: `@nestjs/swagger` 연동으로 자동 API 문서 생성

### [APP] 빌드 최적화
- **현황**: 개발 빌드 상태
- **과제**: Android/iOS Release 빌드 테스트, 앱 크기 및 시작 속도 최적화

---

## 완료된 기능 (참고)

- [x] AR 플로팅 화살표 + 경로 유도선
- [x] Immersal SDK 통합 (XRSpace 좌표 변환)
- [x] 다중 웨이포인트 순차 안내
- [x] 경로 선택 UI (UI Toolkit)
- [x] 방문자 카운팅 (PlayerPrefs)
- [x] 리뷰 및 연령대 조사 UI
- [x] 백엔드 HTTP 통신 (ApiClient 싱글톤)
- [x] 데이터 캐싱 (버전 기반 조건부 다운로드)
- [x] 화살표 회전 보정 (0, 0, -90) 실기기 확인
- [x] 웹-백엔드 서버 통합 (Express+MySQL → NestJS+Supabase 단일 서버)
  - `artworks`: 웹 전용 필드(`feature`, `contents`, `ar_marker_id`, `pos_x`, `pos_z`, `floor_info`) 추가
  - `reviews`: `nickname` 추가, `artwork_id` 선택값으로 변경
  - 이미지: 로컬 `uploads/` → Supabase Storage `artwork-images` 버킷
  - React `Content.jsx`, `User.jsx`: NestJS API 엔드포인트 및 응답 포맷 적용
  - 개발용 proxy(`"proxy": "http://localhost:3000"`) 및 프로덕션 정적 서빙(`ServeStaticModule`) 구성
- [x] 백엔드-DB 연결 (Supabase 환경변수 설정, 스키마 마이그레이션 실행)
  - `admins`, `artworks`, `reviews`, `surveys`, `maps`, `routes`, `visitors` 테이블 생성
  - `artwork-images`, `map-files` Storage 버킷 생성
  - `@nestjs/serve-static` 설치, 개발 환경 조건부 로드 처리
- [x] Unity 앱 전용 API 구현 (`/routes`, `/exhibits`, `/assets/map`, `/assets/floor-plan`, `/visitors`)
  - `@RawResponse()` 데코레이터로 Unity/웹 응답 형식 분리
  - `routes`: `GET` 순수 배열 반환 (Unity `RouteListWrapper` 호환)
  - `exhibits`: artworks 테이블 → ExhibitDto 변환 후 순수 배열 반환
  - `assets`: maps 테이블 → `{ fileUrl, version }` 직접 반환. 파일 미등록 시 `{ fileUrl: '', version: '' }` 반환 (404 없음)
  - `visitors`: `POST /visitors` — deviceId, visitedAt, ageGroup 수신
- [x] 실제 경로 데이터 DB 연동 및 Mock 제거
  - Supabase `routes` 테이블에 경로 데이터 입력 완료
  - `NavRouteData.cs`: `MockRoutes` 클래스 삭제, `MockExhibits.GetAllExhibits()` 삭제
  - `DataSyncManager.cs`: API 실패 시 Mock 폴백 → 빈 배열 처리로 변경
  - `RouteSelectController.cs` / `RouteSelectUserController.cs`: Mock 폴백 제거
  - `ApiClient.baseUrl` 기본값 `http://localhost:3000/api` 로 수정 (글로벌 프리픽스 `/api` 포함)
- [x] 방문자 연령대·카운트 DB 연동 완료
  - `analytics.repository.ts`: `surveys` → `visitors` 테이블 기준으로 전환
  - `visitors.repository.ts`: `.order('created_at')` → `.order('visited_at')` 수정
  - `Home.jsx`: labelMap 영어 enum 변환 제거, `visitors.age_group` 한국어 값 그대로 표시
  - 앱에서 `POST /api/visitors` 호출 시 `deviceId`, `visitedAt`(UTC), `ageGroup`(한국어) 저장
- [x] 리뷰 등록 기능 완성
  - `ApiModels.cs` `ReviewRequest.comment` → `content` 필드명 수정 (백엔드 `CreateReviewDto.content`와 일치, 의견 텍스트 DB 저장 버그 수정)
  - `DataSyncManager.cs`: `ReviewRequest` 생성 시 `comment = comment` → `content = comment` 수정
  - `UserReviewController.cs`: 리뷰 제출 성공/실패 피드백 UI 추가
    - 성공 시 "리뷰가 등록되었습니다. 감사합니다! ★" 표시 후 2초 대기 → 자동 리셋
    - 실패 시 "리뷰 등록에 실패했습니다. 다시 시도해주세요." 표시
    - `review-status-label` UXML 요소 없을 때 null 안전 처리
  - `User.jsx`: 리뷰 목록에 삭제 버튼(휴지통 아이콘) 추가 + `DELETE /api/reviews/:id` 연동
    - JWT 인증 구현 전까지는 403 반환 (인증 구현 후 정상 동작)
- [x] 경로 유도선 버튼-상태 불일치 수정 및 높이 조정
  - `ARNavigationController.cs`: `CreatePathLine()`에서 `_pathLineVisible = false` 하드코딩 — `pathLineVisibleOnStart` Inspector 값과 무관하게 항상 숨김으로 시작
  - Inspector `pathLineHeightOffset`: `0.05` → `0.01` (바닥 바로 위)로 변경 후 씬 저장
  - 버튼(비활성 = "경로선 보기")과 실제 경로선(숨김) 초기 상태 일치
- [x] 경로 유도선 시각 개선
  - 색상: 앱 테마 딥 블루(`rgb(41,69,159)`) → 밝은 파랑(`rgb(100,140,230)`) 그라디언트로 통일
  - 투명도: alpha `0.85` → `0.5` (반투명)
  - 꺾임 라운드: `numCornerVertices` 4 → 10, `numCapVertices` 4 → 6 (부드러운 곡선)
- [x] 사용자 선택 경로 최적 순서 자동 계산
  - `NavRouteData.cs`: `OptimizeVisitOrder()` 추가 — Nearest Neighbor 알고리즘으로 전시품 간 이동 거리 최소화
  - `MockExhibits.CreateUserRoute()`: 선택 순서 그대로 사용 → 최적화 후 웨이포인트 생성으로 변경
  - `RouteSelectUserController.cs`: `_selectedOrder: List<int>` → `_selectedIndices: HashSet<int>` 로 교체 (순서 추적 불필요)
  - UI 뱃지에서 순서 번호 제거, 선택/미선택 체크마크만 표시
  - 상태바 텍스트: "순서대로 선택" → "선택하세요 (자동 최적 경로 안내)"
  - 관리자 생성 경로(`RouteSelectController`)는 변경 없음 — 의도한 순서 그대로 안내
- [x] 경로 유도선 흐름 애니메이션 추가 (커스텀 셰이더)
  - `Assets/Navigation/PathLineFlow.shader` 신규 생성 (`Custom/PathLineFlow`)
    - `_Time.y` 기반 UV 스크롤 — C# 업데이트 루프 없이 GPU에서 자동 애니메이션
    - 대시 패턴: 카메라(시작) → 목적지(끝) 방향으로 흐름
    - 머리 부분 발광 하이라이트 + 꼬리 지수 페이드
    - 선 너비 가장자리 페이드 (소프트 발광선 효과)
    - `ZTest Always`, `ZWrite Off` 하드코딩 — AR 바닥 메시에 가려지지 않음
    - `FallBack "Universal Render Pipeline/Unlit"` 폴백 설정
  - `ARNavigationController.cs` 수정
    - Inspector 필드 4개 추가: `pathLineFlowSpeed(1.5)`, `pathLineDashFrequency(2.5)`, `pathLineDashRatio(0.65)`, `pathLineGlowIntensity(1.3)`
    - `CreatePathLine()`: `textureMode = LineTextureMode.Tile` 추가 (UV.x = 길이 방향 누적 거리)
    - `CreatePathLineMaterial()`: `Custom/PathLineFlow` 우선 로드, 실패 시 URP Unlit 폴백
    - 기존 `colorGradient`(딥 블루→밝은 파랑) 유지 — 버텍스 컬러로 셰이더에 전달됨
- [x] 듀얼 맵 지원 구현 (mapId 145962 + 145963)
  - **배경**: 공간이 넓어 두 개의 Immersal 맵으로 분할 스캔. 무료 플랜으로 포털 스티칭 불가 → Unity 멀티맵 방식 적용
  - `NavWaypoint.mapIndex` 필드 추가 — `0` = 맵 145962 (AR Space), `1` = 맵 145963 (AR Space 2)
  - `Exhibit.mapIndex` 필드 추가 (사용자 선택 경로 생성 시 XRSpace 분기 용도)
  - `WaypointDto.mapIndex`, `ExhibitDto.mapIndex` 추가 (백엔드 DTO → 앱 변환 시 전달)
  - `ARNavigationController.cs`
    - `immersalXRSpaceB` Inspector 필드 추가 (AR Space 2 연결)
    - `GetXRSpace(int mapIndex)` 헬퍼 추가
    - `LocalToWorldPoint(Vector3, int mapIndex)` — mapIndex로 XRSpace 분기
    - `CheckWaypointReached()` — 월드 좌표 직접 비교 방식으로 변경 (단일 좌표계 의존 제거)
  - `DataSyncManager.cs`
    - `mapAssetA`, `mapAssetB` TextAsset SerializeField 추가 — Inspector 연결 시 백엔드 다운로드 스킵
    - `WriteBuiltInMap()` 헬퍼 — TextAsset 바이트를 persistentDataPath에 기록
    - `MapFilePath2` 프로퍼티 추가 (맵 B 파일 경로)
  - `assets.service.ts` / `assets.controller.ts`
    - `map_type = 'immersal_map_b'` 추가
    - `GET /api/assets/map-b` 엔드포인트 추가 (TextAsset 미연결 시 폴백용)
  - **씬 구성** (`SampleScene.unity`)
    - `AR Space 2` (XRSpace) 신규 생성 → `XR Map 145963-real2` (XRMap, mapId=145963, 145963-real2.bytes) 자식으로 추가
    - `ARNavigationController.immersalXRSpaceB` → AR Space 2 연결
    - `DataSyncManager.mapAssetA` → 145962-real.bytes, `mapAssetB` → 145963-real2.bytes 연결
  - **캘리브레이션 완료**: AR Space 2 Transform을 에디터에서 수동으로 조정하여 두 맵 공간 정렬 완료
  - **웨이포인트 데이터**: Supabase `routes` 테이블 waypoints JSONB에 `"mapIndex": 0 또는 1` 추가 필요
- [x] 지도 에디터 평면도 이미지 표시 개선 (`SpaceEditor.jsx`)
  - 이미지 변경 시 원본 비율을 유지하도록 컨테이너 크기 동적 조정
    - `canvasSize` 상태 추가 (초기값 800×560)
    - `handleImageLoad`: `naturalWidth / naturalHeight` 읽어 최대 800×700 범위 내에서 비율 유지 크기 계산
    - 컨테이너 고정 클래스(`w-[800px] h-[560px]`) → `canvasSize` 기반 인라인 스타일로 교체
    - 이미지 `object-cover` → `object-fill` (컨테이너가 이미지 비율에 맞게 조정되므로 왜곡 없음)
  - 이미지 불투명도 `opacity-40` 제거 → 원본 색상 그대로 표시
- [x] 도슨트 패널 구현 (`DocentManager.cs`)
  - AR 공간 각 전시품 위치에 World Space Canvas 패널 생성 (이미지 + 이름 + 특징 + 작가 + 도슨트 텍스트)
  - "도슨트 ON/OFF" 버튼으로 전체 패널 일괄 표시/숨김 (빌보드 효과 적용, AR 바닥 메시에 가려지지 않도록 ZTest Always 설정)
  - 씬 앵커(`DocentAnchorEntry[]`) 연결 지원 — 앵커 있을 시 고정 위치, 없을 시 Immersal 좌표 변환으로 매 프레임 갱신
  - 이미지 비동기 다운로드 후 `RawImage`에 적용, `AspectRatioFitter`로 비율 유지
- [x] 도슨트 패널 텍스트 렌더링 버그 수정
  - **원인**: `TMP_FontAsset.CreateFontAsset(Font)`로 생성된 동적 폰트는 초기 atlas가 비어 있음. `SetActive(false)` 상태에서 `ContentSizeFitter`가 `preferredHeight = 0`을 계산 → `VerticalLayoutGroup`이 텍스트 박스 높이를 0으로 설정해 텍스트 전체가 사라짐. 이미지는 `LayoutElement` 고정 높이로 영향 없음
  - `DocentManager.AddText()`: 폰트 먼저 설정 후 텍스트 지정하고 `ForceMeshUpdate(ignoreActiveState: true)` 추가 — 비활성 상태에서도 한국어 글리프를 즉시 atlas에 로드
  - `DocentManager.ToggleDocents()`: 활성화 후 `RebuildAllPanelLayouts()` 코루틴 추가 — 2프레임 대기 후 레이아웃 강제 재계산
  - `DocentManager.LoadFont()`: 에디터 폴백에 `MalgunGothic SDF.asset` 직접 로드 시도 추가 (UIToolkit 폰트이므로 null 반환, TTF 경로로 진행)
- [x] NavMesh 베이킹 범위 재조정 (Integrated AR Map 기준)
  - `Assets/Editor/NavMeshFitToARMap.cs` 신규 생성 — Integrated AR Map 전체 Renderer 바운드를 자동 계산해 NavMeshOrigin 위치·NavFloor 스케일을 일괄 재조정하는 에디터 유틸리티
  - NavMeshOrigin 위치: (0,0,0) → (-1.61, 0, -6.62) (AR Map XZ 중심)
  - NavFloor Scale: (1.1, 1, 1.4) → (6.654, 1, 7.085) (66.54 × 70.85 m)
  - collectObjects = Children 모드 적용 — Volume 모드에서 NavFloor가 베이킹 박스 경계(Y=0)에 걸려 절반만 베이킹되는 버그 수정
- [x] 전체 지도화면 코드 완성 (웹 + 백엔드)
  - `DELETE /api/routes/:id` 엔드포인트 추가 (`routes.controller/service/repository.ts`)
  - `findAll()` 응답에 `id`(UUID) 필드 추가 (웹 대시보드 삭제용)
  - `SpaceRoute.jsx`: 경로 삭제 기능 구현 (DELETE 요청 + 확인 다이얼로그 + 로딩 표시)
  - `SpaceMap.jsx`: MapCard에 floor 뱃지 추가 (amber 색상) + 미지정 안내 표시
  - **잔여**: Supabase `maps` 테이블에 `floor TEXT` 컬럼 추가 SQL 실행 필요 (session.md 참고)
