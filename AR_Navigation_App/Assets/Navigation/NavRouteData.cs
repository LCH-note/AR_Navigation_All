/*
    파일명: Assets/Navigation/NavRouteData.cs
    역할: AR 내비게이션에 사용할 경로 데이터 클래스 및 목업 데이터 저장소
    구조:
      NavWaypoint  → 경로 상의 안내 지점 1개 (맵 로컬 좌표 + 안내 문구)
      NavRoute     → 출발지~목적지 전체 경로 (웨이포인트 배열 포함)
      MockRoutes   → 테스트용 목업 경로 3개 정의 (실제 맵 좌표로 교체 필요)
    좌표계:
      웨이포인트 localPosition 은 Immersal 맵 로컬 좌표 (XRSpace 기준)
      Unity 씬의 AR Space > Map > XRSpace Transform 을 기준으로 하는 로컬 좌표.
      실제 좌표 값은 Unity 씬에서 sparse.ply 포인트 클라우드를 시각화한 뒤
      XRSpace 하위에 빈 GameObject 를 배치해 Inspector 로컬 좌표 값을 읽어 입력.
*/

using UnityEngine;

// ── 웨이포인트 1개 ─────────────────────────────────────────────────
[System.Serializable]
public class NavWaypoint
{
    // Immersal 맵 로컬 좌표 (XRSpace 기준, 미터 단위)
    public Vector3 localPosition;

    // 이 웨이포인트 도달 시 표시할 안내 문구
    public string instruction;

    // 목적지 이름 (경로 계산·HUD 표시에 사용)
    public string displayName;
}

// ── 경로 전체 데이터 ───────────────────────────────────────────────
[System.Serializable]
public class NavRoute
{
    public string routeId;           // 경로 고유 ID
    public string routeName;         // UI 표시용 경로 이름
    public string destination;       // 목적지 이름
    public string description;       // 경로 설명 (1줄)
    public string estimatedDistance; // 예상 거리 (예: "약 50m")
    public string estimatedTime;     // 예상 시간 (예: "약 1분")
    public NavWaypoint[] waypoints;  // 경유 웨이포인트 배열 (순서대로 안내)
}

// ── 전시품 데이터 ──────────────────────────────────────────────────
[System.Serializable]
public class Exhibit
{
    public string exhibitId;      // 전시품 고유 ID
    public string name;           // 전시품 이름 (UI 표시용)
    public string artist;         // 작가/제작자
    public string hall;           // 전시관 위치 (예: "A관 1층")
    public Vector3 localPosition; // Immersal 맵 로컬 좌표 (XRSpace 기준)
}

// ── 목업 전시품 데이터 저장소 ──────────────────────────────────────
// 실제 서비스 시 서버 API 응답으로 교체 예정
// 좌표: Immersal 맵 로컬 공간 (144383-oneroom 기준), 실기기 실측값
public static class MockExhibits
{
    public static Exhibit[] GetAllExhibits()
    {
        return new Exhibit[]
        {
            new Exhibit
            {
                exhibitId     = "ex_001",
                name          = "전시품 A",
                artist        = "작가 미상",
                hall          = "1구역",
                localPosition = new Vector3(1.173f, 0f, -1.596f)   // 실측값 (기준점)
            },
            new Exhibit
            {
                exhibitId     = "ex_002",
                name          = "전시품 B",
                artist        = "작가 미상",
                hall          = "2구역",
                localPosition = new Vector3(1.71f, 0f, -0.02f)    // 실측값
            },
            new Exhibit
            {
                exhibitId     = "ex_003",
                name          = "전시품 C",
                artist        = "작가 미상",
                hall          = "3구역",
                localPosition = new Vector3(-1.77f, 0f, -0.13f)      // 실측값
            },
        };
    }

    /// <summary>
    /// 사용자가 선택한 전시품 목록으로 NavRoute 를 생성합니다.
    /// 선택 순서가 경유 순서가 됩니다.
    /// </summary>
    public static NavRoute CreateUserRoute(Exhibit[] selectedExhibits)
    {
        if (selectedExhibits == null || selectedExhibits.Length == 0)
            return null;

        var waypoints = new NavWaypoint[selectedExhibits.Length];
        for (int i = 0; i < selectedExhibits.Length; i++)
        {
            waypoints[i] = new NavWaypoint
            {
                localPosition = selectedExhibits[i].localPosition,
                displayName   = selectedExhibits[i].name,
                instruction   = $"{selectedExhibits[i].name} 도착"
            };
        }

        // 전시품 이름 목록으로 경로 이름 생성 (최대 2개 표시 후 '외 N개')
        string routeName;
        if (selectedExhibits.Length <= 2)
            routeName = string.Join(" → ", System.Array.ConvertAll(selectedExhibits, e => e.name));
        else
            routeName = $"{selectedExhibits[0].name} → {selectedExhibits[1].name} 외 {selectedExhibits.Length - 2}개";

        return new NavRoute
        {
            routeId           = "route_user_custom",
            routeName         = routeName,
            destination       = selectedExhibits[selectedExhibits.Length - 1].name,
            description       = $"선택한 전시품 {selectedExhibits.Length}개 경유",
            estimatedDistance = "경로에 따라 다름",
            estimatedTime     = "경로에 따라 다름",
            waypoints         = waypoints
        };
    }
}

// ── 목업 경로 데이터 저장소 ───────────────────────────────────────
// 실제 서비스 시 서버 API 응답으로 교체 예정
// 좌표: Immersal 맵 로컬 공간 (144383-oneroom 기준), 실기기 실측값
// 여기에 등록된 경로만 RouteSelectScreen(추천 경로 화면)에 표시됨
public static class MockRoutes
{
    public static NavRoute[] GetAllRoutes()
    {
        return new NavRoute[]
        {
            CreateRoute_AllStops(),
            // 추후 백엔드 API 응답으로 교체 시 이 배열을 서버 데이터로 대체
        };
    }

    // ── 전체 순회: A → B → C ──────────────────────────────────────
    private static NavRoute CreateRoute_AllStops()
    {
        return new NavRoute
        {
            routeId           = "route_all_stops",
            routeName         = "전체 순회 (A → B → C)",
            destination       = "전시품 C",
            description       = "모든 전시품을 순서대로 방문합니다",
            estimatedDistance = "약 5m",
            estimatedTime     = "약 3분",
            waypoints = new NavWaypoint[]
            {
                new NavWaypoint
                {
                    localPosition = new Vector3(1.173f, 0f, -1.596f),   // 전시품 A (실측값)
                    displayName   = "전시품 A",
                    instruction   = "전시품 A 도착! 다음: 전시품 B"
                },
                new NavWaypoint
                {
                    localPosition = new Vector3(1.71f, 0f, -0.02f),    // 전시품 B (실측값)
                    displayName   = "전시품 B",
                    instruction   = "전시품 B 도착! 다음: 전시품 C"
                },
                new NavWaypoint
                {
                    localPosition = new Vector3(-1.77f, 0f, -0.13f),      // 전시품 C (실측값)
                    displayName   = "전시품 C",
                    instruction   = "전시품 C 도착! 전체 순회 완료"
                },
            }
        };
    }
}
