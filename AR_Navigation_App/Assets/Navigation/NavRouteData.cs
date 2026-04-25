/*
    파일명: Assets/Navigation/NavRouteData.cs
    역할: AR 내비게이션에 사용할 경로 데이터 클래스 및 목업 데이터 저장소
    구조:
      NavWaypoint  → 경로 상의 안내 지점 1개 (로컬 좌표 + 안내 문구)
      NavRoute     → 출발지~목적지 전체 경로 (웨이포인트 배열 포함)
      MockRoutes   → 테스트용 목업 경로 3개 정의
    좌표계:
      웨이포인트 localPosition 은 내비게이션 시작 시점 카메라 기준 로컬 좌표
      Z+ = 출발 시 카메라 정면 방향, X+ = 오른쪽, Y = 높이(m)
*/

using UnityEngine;

// ── 웨이포인트 1개 ─────────────────────────────────────────────────
[System.Serializable]
public class NavWaypoint
{
    // 내비게이션 시작 카메라 기준 로컬 좌표 (미터 단위)
    public Vector3 localPosition;

    // 이 웨이포인트 도달 시 하단 HUD 에 표시할 안내 문구
    public string instruction;
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
    public Vector3 localPosition; // 내비게이션 시작 카메라 기준 로컬 좌표
}

// ── 목업 전시품 데이터 저장소 ──────────────────────────────────────
// 실제 서비스 시 서버 API 응답으로 교체 예정
public static class MockExhibits
{
    public static Exhibit[] GetAllExhibits()
    {
        return new Exhibit[]
        {
            new Exhibit
            {
                exhibitId     = "ex_001",
                name          = "인상, 해돋이",
                artist        = "클로드 모네",
                hall          = "A관 1층",
                localPosition = new Vector3(0f, 0f, 8f)
            },
            new Exhibit
            {
                exhibitId     = "ex_002",
                name          = "별이 빛나는 밤",
                artist        = "빈센트 반 고흐",
                hall          = "A관 1층",
                localPosition = new Vector3(5f, 0f, 12f)
            },
            new Exhibit
            {
                exhibitId     = "ex_003",
                name          = "게르니카",
                artist        = "파블로 피카소",
                hall          = "B관 1층",
                localPosition = new Vector3(10f, 0f, 10f)
            },
            new Exhibit
            {
                exhibitId     = "ex_004",
                name          = "물랭 드 라 갈레트",
                artist        = "피에르 오귀스트 르누아르",
                hall          = "B관 1층",
                localPosition = new Vector3(14f, 0f, 6f)
            },
            new Exhibit
            {
                exhibitId     = "ex_005",
                name          = "진주 귀걸이를 한 소녀",
                artist        = "요하네스 페르메이르",
                hall          = "C관 2층",
                localPosition = new Vector3(-3f, 3f, 18f)
            },
            new Exhibit
            {
                exhibitId     = "ex_006",
                name          = "수련",
                artist        = "클로드 모네",
                hall          = "C관 2층",
                localPosition = new Vector3(2f, 3f, 22f)
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
public static class MockRoutes
{
    public static NavRoute[] GetAllRoutes()
    {
        return new NavRoute[]
        {
            CreateRoute_Lobby(),
            CreateRoute_ExhibitB(),
            CreateRoute_Cafe2F(),
        };
    }

    // ── 경로 0: 입구 → 1층 로비 (직진) ─────────────────────────
    private static NavRoute CreateRoute_Lobby()
    {
        return new NavRoute
        {
            routeId             = "route_lobby",
            routeName           = "입구 → 1층 로비",
            destination         = "1층 로비",
            description         = "입구에서 중앙 로비로 직진",
            estimatedDistance   = "약 20m",
            estimatedTime       = "약 1분",
            waypoints = new NavWaypoint[]
            {
                new NavWaypoint
                {
                    localPosition = new Vector3(0f, 0f, 3f),
                    instruction   = "직진하세요"
                },
                new NavWaypoint
                {
                    localPosition = new Vector3(0f, 0f, 8f),
                    instruction   = "직진하세요"
                },
                new NavWaypoint
                {
                    localPosition = new Vector3(0f, 0f, 14f),
                    instruction   = "조금만 더 직진하세요"
                },
                new NavWaypoint
                {
                    localPosition = new Vector3(0f, 0f, 20f),
                    instruction   = "1층 로비 도착!"
                },
            }
        };
    }

    // ── 경로 1: 로비 → B관 전시실 (우회전 포함) ─────────────────
    private static NavRoute CreateRoute_ExhibitB()
    {
        return new NavRoute
        {
            routeId             = "route_exhibit_b",
            routeName           = "로비 → B관 전시실",
            destination         = "B관 전시실",
            description         = "로비에서 우회전 후 B관 전시실로",
            estimatedDistance   = "약 35m",
            estimatedTime       = "약 2분",
            waypoints = new NavWaypoint[]
            {
                new NavWaypoint
                {
                    localPosition = new Vector3(0f,   0f, 5f),
                    instruction   = "직진하세요"
                },
                new NavWaypoint
                {
                    localPosition = new Vector3(0f,   0f, 10f),
                    instruction   = "우회전하세요"
                },
                new NavWaypoint
                {
                    localPosition = new Vector3(6f,   0f, 10f),
                    instruction   = "직진하세요"
                },
                new NavWaypoint
                {
                    localPosition = new Vector3(12f,  0f, 10f),
                    instruction   = "직진하세요"
                },
                new NavWaypoint
                {
                    localPosition = new Vector3(18f,  0f, 10f),
                    instruction   = "B관 전시실 도착!"
                },
            }
        };
    }

    // ── 경로 2: 입구 → 2층 카페 (좌회전 + 계단) ─────────────────
    private static NavRoute CreateRoute_Cafe2F()
    {
        return new NavRoute
        {
            routeId             = "route_cafe_2f",
            routeName           = "입구 → 2층 카페",
            destination         = "2층 카페",
            description         = "계단을 통해 2층 카페로 이동",
            estimatedDistance   = "약 50m",
            estimatedTime       = "약 3분",
            waypoints = new NavWaypoint[]
            {
                new NavWaypoint
                {
                    localPosition = new Vector3(0f,   0f,  5f),
                    instruction   = "직진하세요"
                },
                new NavWaypoint
                {
                    localPosition = new Vector3(-4f,  0f,  5f),
                    instruction   = "좌회전하세요"
                },
                new NavWaypoint
                {
                    localPosition = new Vector3(-4f,  0f,  12f),
                    instruction   = "계단이 보입니다"
                },
                new NavWaypoint
                {
                    localPosition = new Vector3(-4f,  3f,  16f),
                    instruction   = "계단을 올라가세요"
                },
                new NavWaypoint
                {
                    localPosition = new Vector3(-4f,  3f,  22f),
                    instruction   = "2층 카페 도착!"
                },
            }
        };
    }
}
