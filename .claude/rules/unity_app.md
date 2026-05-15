# Unity AR 앱 작업 규칙

> `AR_Navigation_App/` 폴더 작업 시에만 이 파일을 참고한다.

---

## 파일 구조

```
AR_Navigation_App/Assets/
├── Navigation/
│   ├── ARNavigationController.cs   (837줄) 핵심 컨트롤러
│   ├── NavRouteData.cs             (~100줄) 데이터 모델 + 사용자 경로 생성 유틸리티
│   ├── RouteSelectController.cs    (245줄) 경로 선택 UI
│   ├── RouteSelectUserController.cs(315줄) 사용자 커스텀 경로 선택 UI
│   └── UserReviewController.cs     (156줄) 리뷰/연령대 UI
├── Network/
│   ├── ApiClient.cs                (201줄) HTTP 싱글톤
│   ├── DataSyncManager.cs          (387줄) 초기화/캐싱/방문자/리뷰
│   └── ApiModels.cs                (90줄)  백엔드 DTO
├── Models/              3D 모델 및 Prefab
├── Maps/                Immersal 맵 메타데이터
└── Scenes/SampleScene   메인 씬 (하나뿐)
```

---

## 데이터 모델 (NavRouteData.cs)

### NavWaypoint [Serializable]
| 필드 | 타입 | 설명 |
|------|------|------|
| `localPosition` | Vector3 | Immersal 맵 로컬 좌표 |
| `instruction` | string | 도달 시 안내 문구 |
| `displayName` | string | 목적지 이름 |

### NavRoute [Serializable]
| 필드 | 타입 |
|------|------|
| `routeId` | string |
| `routeName` | string |
| `destination` | string |
| `description` | string |
| `estimatedDistance` | string |
| `estimatedTime` | string |
| `waypoints` | NavWaypoint[] |

### Exhibit [Serializable]
| 필드 | 타입 | 비고 |
|------|------|------|
| `exhibitId` | string | |
| `name` | string | |
| `artist` | string | |
| `hall` | string | |
| `docentText` | string | UI 미구현 — 웨이포인트 도달 시 팝업 표시 예정 |
| `localPosition` | Vector3 | Immersal 맵 로컬 좌표 |

### MockExhibits (유틸리티 — NavRouteData.cs)
`MockRoutes` 클래스는 삭제됨. `MockExhibits`는 아래 정적 메서드만 유지:

```csharp
// 사용자가 선택한 Exhibit[] → NavRoute 생성 (RouteSelectUserController에서 호출)
static NavRoute CreateUserRoute(Exhibit[] selectedExhibits)
```

> 실제 경로·전시물 데이터는 Supabase DB에서 서버 API로 수신. Mock 폴백 없음.
> 서버 통신 실패 시 빈 배열 반환 → 화면에 "등록된 경로/전시품이 없습니다" 표시.

---

## 백엔드 DTO (ApiModels.cs)

### VisitorRequest
```csharp
string deviceId;   // SystemInfo.deviceUniqueIdentifier
string visitedAt;  // ISO 8601 형식 (DateTime.UtcNow)
string ageGroup;   // 한국어 문자열 그대로 전송 ("10대", "20대" … "60대 이상")
                   // visitors 테이블 age_group 컬럼은 TEXT 타입 — enum 변환 불필요
```

### ReviewRequest
```csharp
int rating;        // 1~5
string comment;
```

### RouteDto / WaypointDto / ExhibitDto
백엔드 응답 역직렬화용. `ConvertRoutes()` / `ConvertExhibits()`로 NavRoute / Exhibit으로 변환.

**백엔드 실제 반환 필드 (GET /api/routes 아이템)**:
```
routeId, routeName, destination, description, estimatedDistance, estimatedTime
waypoints: [{ x, y, z, displayName, instruction }]  // camelCase, Immersal 로컬 좌표
```

**백엔드 실제 반환 필드 (GET /api/exhibits 아이템)**:
```
exhibitId    // artworks.ar_marker_id 또는 artworks.id
name         // artworks.title
artist       // artworks.artist
hall         // artworks.floor_info
docentText   // artworks.contents 또는 artworks.description
x, y, z      // artworks.pos_x, 0, artworks.pos_z (parseFloat)
```

### AssetUrlResponse
```csharp
string fileUrl;    // Supabase Storage 공개 URL
string version;    // maps.updated_at 타임스탬프 (캐시 무효화용)
```

### 래퍼 클래스
- `RouteListWrapper { RouteDto[] items }` — GET /api/routes 응답 (GetArrayAsync 내부 래핑)
- `ExhibitListWrapper { ExhibitDto[] items }` — GET /api/exhibits 응답 (GetArrayAsync 내부 래핑)

---

## ApiClient.cs (싱글톤)

### Inspector 필드
| 필드 | 기본값 | 설명 |
|------|--------|------|
| `baseUrl` | `"http://localhost:3000"` | 서버 기본 URL — 배포 시 변경 필요 |
| `timeoutSeconds` | 15 | 일반 요청 타임아웃 |
| `downloadTimeoutSeconds` | 60 | 파일 다운로드 타임아웃 |

### 주요 메서드
```csharp
GetAsync<T>(string endpoint, Action<T, string> callback)
GetArrayAsync<TItem, TWrap>(string endpoint, Func<TWrap, TItem[]> selector, Action<TItem[], string> callback)
PostAsync<TReq>(string endpoint, TReq body, Action<bool, string> callback)
DownloadBytesAsync(string url, Action<byte[], string> callback)
DownloadTextureAsync(string url, Action<Texture2D, string> callback)
```

---

## DataSyncManager.cs (싱글톤)

### 정적 프로퍼티
| 프로퍼티 | 타입 | 설명 |
|----------|------|------|
| `LoadedRoutes` | NavRoute[] | |
| `LoadedExhibits` | Exhibit[] | |
| `MapFilePath` | string | persistentDataPath 내 경로 |
| `FloorPlanTexture` | Texture2D | |
| `IsDataReady` | bool | |

### 정적 이벤트
| 이벤트 | 시그니처 | 발화 시점 |
|--------|----------|----------|
| `OnDataReady` | Action | 전체 데이터 로드 완료 |
| `OnLoadError` | Action<string> | 오류 발생 |

### PlayerPrefs 키
| 상수 | 값 |
|------|----|
| `KeyVisitorCounted` | "visitor_counted_v1" |
| `KeyMapVersion` | "cached_map_version" |
| `KeyFloorVersion` | "cached_floor_version" |
| `MapFileName` | "immersal_map_data.bytes" |
| `FloorPlanFileName` | "floor_plan.png" |

### 초기화 순서 (LoadAllDataAsync)
```
1. GET /api/routes         → RouteDto[] → NavRoute[] → LoadedRoutes
2. GET /api/exhibits       → ExhibitDto[] → Exhibit[] → LoadedExhibits
3. GET /api/assets/map     → fileUrl 비어있으면 스킵, 있으면 버전 비교 후 조건부 bytes 다운로드
4. GET /api/assets/floor-plan → fileUrl 비어있으면 스킵, 있으면 버전 비교 후 조건부 Texture2D 다운로드
```
> **✅ 모두 구현 완료** — 백엔드 Supabase 연동 완료.  
> API 실패 시 Mock 폴백 없음 → 빈 배열(`new NavRoute[0]` / `new Exhibit[0]`) 사용.  
> 에셋(map/floor-plan) 미등록 시 서버가 `{ fileUrl: '', version: '' }` 반환 → 앱이 로컬 캐시 또는 스킵 처리.

### 백엔드 응답 형식 (Unity 호환)
Unity 엔드포인트(`/routes`, `/exhibits`)는 `@RawResponse()` 데코레이터로 NestJS의 `{ success, data }` 래핑을 생략하고 순수 배열 `[...]`을 반환한다.  
`GetArrayAsync`가 이 배열을 `{"items": [...]}` 형태로 래핑하므로 `RouteListWrapper.items` / `ExhibitListWrapper.items`로 접근.

### 방문자 등록 (SubmitVisitorAsync)
- `PlayerPrefs.GetInt(KeyVisitorCounted)` == 0인 경우만 `POST /api/visitors` 호출
- 성공 시 `PlayerPrefs.SetInt(KeyVisitorCounted, 1)` 저장
- `ageGroup`: 한국어 문자열 그대로 전송 ("10대", "20대" … "60대 이상")
  - `visitors.age_group` 컬럼은 TEXT 타입이므로 enum 변환 불필요
  - `analytics/age-groups`도 `visitors` 테이블 기준으로 동일한 한국어 키 반환

---

## ARNavigationController.cs

### Inspector 필드 (전체)
| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `arCamera` | Camera | — | AR 카메라 참조 |
| `arrowPrefab` | GameObject | — | ArrowModel.fbx 기반 Prefab |
| `waypointReachDistance` | float | 2.0 | 웨이포인트 도달 판정 거리 (m) |
| `floatForwardDistance` | float | 1.5 | 카메라 앞 화살표 거리 (m) |
| `floatHeightOffset` | float | -0.75 | 화살표 높이 오프셋 |
| `rotationSpeed` | float | 6 | 화살표 회전 보간 속도 |
| `arrowScale` | float | 1.0 | 화살표 스케일 |
| `arrowRotationOffset` | Vector3 | **(0, 0, -90)** | 모델 팁 방향 보정 — 절대 변경 금지 |
| `pathLineWidth` | float | 0.06 | 유도선 굵기 |
| `pathLineHeightOffset` | float | 0.05 | 유도선 높이 오프셋 |
| `pathLineVisibleOnStart` | bool | true | 시작 시 유도선 표시 여부 |
| `useImmersalPositioning` | bool | true | Immersal 좌표계 사용 여부 |
| `immersalXRSpace` | Transform | — | Immersal XRSpace 참조 |

### 색상 상수
| 상수 | RGBA |
|------|------|
| `ColorArrow` | (0.10, 0.45, 1.00) 딥 블루 |
| `ColorLineStart` | (0.16, 0.27, 0.62, 0.85) |
| `ColorLineEnd` | (0.39, 0.55, 0.90, 0.85) |

### 주요 메서드
```csharp
StartNavigation(NavRoute route)                              // 경로 안내 시작
StartNavigationTo(Vector3 dest, string name)                 // 단일 목적지 (NavMesh 자동 경로)
StartNavigationTo(Vector3[] dests, string[] names)           // 다중 목적지 순차
StartNavigationToAll(Vector3[] dests, string[] names)        // 전체 목적지 NavMesh 한 번에 계산
StopNavigation()                                             // 안내 종료
TogglePathLine()                                             // 유도선 표시 토글
```

### 좌표 시스템
- **실기기**: `XRSpace.TransformPoint(waypoint.localPosition)` 매 프레임 호출
- **에디터**: `Start()`에서 카메라 로컬→월드 행렬 `_localToWorld` 고정, 이후 변경 없음
- **Immersal 맵 ID**: `144383` (oneroom)

---

## RouteSelectController.cs

### UI 요소 (USS 셀렉터)
| 요소 | 이름 |
|------|------|
| 루트 | RouteSelectScreenInstance |
| ScrollView | route-scroll |
| 시작 버튼 | btn-start-navigation |

### 주요 메서드
```csharp
OnScreenShown()          // 화면 표시 시 호출 — 경로 로드 및 카드 동적 생성
GetSelectedRoute()       // 선택된 NavRoute 반환
```
데이터 소스: `DataSyncManager.LoadedRoutes` (null이면 빈 배열 — Mock 폴백 없음).

---

## RouteSelectUserController.cs

### UI 요소 (USS 셀렉터)
| 요소 | 이름 |
|------|------|
| 루트 | RouteSelectUserScreenInstance |
| ScrollView | exhibit-scroll |
| 시작 버튼 | btn-start-user-navigation |
| 초기화 버튼 | btn-reset-exhibit |
| 전체 선택 버튼 | btn-select-all |
| 선택 수 라벨 | label-selected-count |

### 선택 로직
- 선택 순서를 `_selectedOrder (List<int>)`로 유지 → NavRoute 웨이포인트 순서에 반영
- 카드 재클릭 시 선택 해제, 뱃지 번호 재정렬

---

## UserReviewController.cs

### UI 요소 (USS 셀렉터)
| 요소 | 이름 |
|------|------|
| 별 라벨 | star-1 ~ star-5 |
| 별점 라벨 | review-rating-label |
| 입력 필드 | review-text-field |

### 동작
- 같은 별 재클릭 시 별점 해제 (0으로 리셋)
- 별점 표시: `★` (선택), `☆` (미선택)
- 플레이스홀더: "의견을 남겨주세요"
- 제출 시 `DataSyncManager.SubmitReviewAsync(rating, comment, callback)` 호출

---

## 주요 패키지 버전

| 패키지 | 버전 |
|--------|------|
| `com.immersal.core` | github/HEAD |
| `com.unity.xr.arfoundation` | 5.2.0 |
| `com.unity.xr.arcore` | 5.2.0 |
| `com.unity.xr.arkit` | 5.2.0 |
| `com.unity.xr.interaction.toolkit` | 3.1.2 |
| `com.unity.ai.navigation` | 1.1.5 |
| `com.unity.render-pipelines.universal` | 14.0.12 |
| `com.unity.textmeshpro` | 3.0.9 |

---

## 주의사항

- `arrowRotationOffset = (0, 0, -90)` — 실기기 확인 완료, 절대 변경 금지
- 씬 파일은 `Assets/Scenes/SampleScene.unity` 하나뿐
- 에디터에서 AR 기능 테스트 시 Immersal 비활성 모드로 동작 (`useImmersalPositioning = false`로 간주)
- 실기기 필수 테스트 항목: Immersal 측위, 화살표 방향, AR 카메라
- `ApiClient.baseUrl` 기본값 `"http://localhost:3000/api"` (글로벌 프리픽스 `/api` 포함)
  - Inspector에서 직접 변경 필요 (SerializeField — 코드 기본값보다 Inspector 저장값 우선)
  - 배포 시 실제 서버 주소 + `/api` 로 변경
- `ageGroup` 전송: 한국어 문자열 그대로 (`"20대"` 등) — enum 변환 불필요
- `routes` 테이블에 데이터 없으면 경로 선택 화면에 "등록된 경로가 없습니다" 표시
- 백엔드 모든 Unity API 경로는 `/api` 프리픽스 포함 (`/routes` → baseUrl에 이미 `/api` 포함됨)
