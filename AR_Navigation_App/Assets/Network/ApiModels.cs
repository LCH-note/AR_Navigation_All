/*
    파일명: Assets/Network/ApiModels.cs
    역할: NestJS 백엔드 API 요청/응답 데이터 모델 (DTO)

    엔드포인트 매핑:
      POST /visitors          — VisitorRequest
      POST /reviews           — ReviewRequest
      GET  /routes            — RouteDto[] (배열)
      GET  /exhibits          — ExhibitDto[] (배열)
      GET  /assets/map        — AssetUrlResponse
      GET  /assets/floor-plan — AssetUrlResponse

    JSON 배열 직렬화 주의:
      JsonUtility는 루트 배열([...])을 역직렬화하지 못하므로
      배열 응답은 ApiClient.GetArrayAsync 를 통해 래퍼 클래스로 파싱합니다.
*/

using System;

// ── 방문자 등록 요청 ────────────────────────────────────────────────
[Serializable]
public class VisitorRequest
{
    public string deviceId;   // SystemInfo.deviceUniqueIdentifier
    public string visitedAt;  // ISO 8601 형식 (예: "2026-05-03T10:30:00Z")
    public string ageGroup;   // 연령대 (예: "20대", "60대 이상")
}

// ── 리뷰 제출 요청 ─────────────────────────────────────────────────
[Serializable]
public class ReviewRequest
{
    public int    rating;    // 1~5 별점
    public string content;   // 의견 텍스트 — 백엔드 CreateReviewDto의 content 필드와 일치 (빈 문자열 허용)
}

// ── 경유 웨이포인트 DTO ─────────────────────────────────────────────
// Vector3는 서버 JSON 필드 이름이 다를 수 있으므로 x/y/z 분리
[Serializable]
public class WaypointDto
{
    public float  x;
    public float  y;
    public float  z;
    public string displayName;   // 목적지 이름
    public string instruction;   // 도달 시 안내 문구
    public int    mapIndex;      // 소속 맵 인덱스: 0 = 맵 A (145962, AR Space), 1 = 맵 B (145963, AR Space 2)
    public string exhibitId;     // 연결된 전시품 ID (Exhibit.exhibitId 와 대응). DB에 없으면 null.
}

// ── 경로 DTO (GET /routes 배열 아이템) ─────────────────────────────
[Serializable]
public class RouteDto
{
    public string        routeId;
    public string        routeName;
    public string        destination;
    public string        description;
    public string        estimatedDistance;
    public string        estimatedTime;
    public WaypointDto[] waypoints;
}

// ── 전시물 DTO (GET /exhibits 배열 아이템) ─────────────────────────
[Serializable]
public class ExhibitDto
{
    public string exhibitId;
    public string name;
    public string artist;
    public string hall;
    public string docentText;  // 도슨트 설명 텍스트
    public string imageUrl;    // 전시품 이미지 URL (Supabase Storage)
    public string feature;     // 전시품 특징
    public float  x;
    public float  y;
    public float  z;
    public int    mapIndex;    // 소속 맵 인덱스: 0 = 맵 A (145962, AR Space), 1 = 맵 B (145963, AR Space 2)
}

// ── 에셋 URL 응답 (맵 데이터 / 2D 평면도) ─────────────────────────
[Serializable]
public class AssetUrlResponse
{
    public string fileUrl;   // Supabase Storage 공개 URL
    public string version;   // 캐시 무효화용 버전 식별자
}

// ── 플로어별 2D 평면도 DTO (GET /assets/floor-plans 배열 아이템) ────
[Serializable]
public class FloorPlanDto
{
    public string floor;    // 플로어 키: "B1", "1F", "2F", "3F"
    public string fileUrl;  // Supabase Storage 공개 URL
    public string version;  // 캐시 무효화용 버전 (maps.updated_at)
}

// ── 플로어별 3D 전체도 DTO (GET /assets/3d-models 배열 아이템) ──────
[Serializable]
public class ThreeDModelDto
{
    public string floor;    // 플로어 키: "B1", "1F", "2F", "3F"
    public string fileUrl;  // Supabase Storage 공개 URL (.glb 파일)
    public string version;  // 캐시 무효화용 버전 (maps.updated_at)
}

// ── JSON 배열 래퍼 클래스 ──────────────────────────────────────────
// JsonUtility는 루트 배열을 지원하지 않으므로 {"items":[...]} 형태로 래핑
[Serializable]
public class RouteListWrapper        { public RouteDto[]       items; }
[Serializable]
public class ExhibitListWrapper      { public ExhibitDto[]     items; }
[Serializable]
public class FloorPlanListWrapper    { public FloorPlanDto[]   items; }
[Serializable]
public class ThreeDModelListWrapper  { public ThreeDModelDto[] items; }
