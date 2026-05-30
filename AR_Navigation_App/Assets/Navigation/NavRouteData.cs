/*
    파일명: Assets/Navigation/NavRouteData.cs
    역할: AR 내비게이션에 사용할 경로·전시품 데이터 클래스 정의
    구조:
      NavWaypoint  → 경로 상의 안내 지점 1개 (맵 로컬 좌표 + 안내 문구)
      NavRoute     → 출발지~목적지 전체 경로 (웨이포인트 배열 포함)
      Exhibit      → 전시품 데이터 (서버 API 응답으로 수신)
      MockExhibits → 사용자 선택 경로 생성 유틸리티 (CreateUserRoute)
    좌표계:
      웨이포인트 localPosition 은 Immersal 맵 로컬 좌표 (XRSpace 기준)
      Unity 씬의 AR Space > Map > XRSpace Transform 을 기준으로 하는 로컬 좌표.
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

    // 소속 맵 인덱스: 0 = 맵 A (145962, AR Space), 1 = 맵 B (145963, AR Space 2)
    public int mapIndex;

    // 연결된 전시품 ID (Exhibit.exhibitId 와 대응). 없으면 빈 문자열.
    public string exhibitId;
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
    public string docentText;     // 도슨트 설명 텍스트 (백엔드 API 에서 수신)
    public string imageUrl;       // 전시품 이미지 URL (Supabase Storage)
    public string feature;        // 전시품 특징
    public Vector3 localPosition; // Immersal 맵 로컬 좌표 (XRSpace 기준)
    public int mapIndex;          // 소속 맵 인덱스: 0 = 맵 A (145962, AR Space), 1 = 맵 B (145963, AR Space 2)
}

// ── 사용자 선택 경로 생성 유틸리티 ────────────────────────────────
public static class MockExhibits
{
    /// <summary>
    /// 사용자가 선택한 전시품 목록으로 NavRoute 를 생성합니다.
    /// Nearest Neighbor 알고리즘으로 전시품 간 이동 거리를 최소화하는 순서를 자동 계산합니다.
    /// </summary>
    public static NavRoute CreateUserRoute(Exhibit[] selectedExhibits)
    {
        if (selectedExhibits == null || selectedExhibits.Length == 0)
            return null;

        // 최적 방문 순서 계산
        var optimized = OptimizeVisitOrder(selectedExhibits);

        var waypoints = new NavWaypoint[optimized.Length];
        for (int i = 0; i < optimized.Length; i++)
        {
            waypoints[i] = new NavWaypoint
            {
                localPosition = optimized[i].localPosition,
                displayName   = optimized[i].name,
                instruction   = $"{optimized[i].name} 도착",
                mapIndex      = optimized[i].mapIndex,
                exhibitId     = optimized[i].exhibitId
            };
        }

        // 최적화된 순서로 경로 이름 생성 (최대 2개 표시 후 '외 N개')
        string routeName;
        if (optimized.Length <= 2)
            routeName = string.Join(" → ", System.Array.ConvertAll(optimized, e => e.name));
        else
            routeName = $"{optimized[0].name} → {optimized[1].name} 외 {optimized.Length - 2}개";

        return new NavRoute
        {
            routeId           = "route_user_custom",
            routeName         = routeName,
            destination       = optimized[optimized.Length - 1].name,
            description       = $"선택한 전시품 {optimized.Length}개 최적 경로",
            estimatedDistance = "경로에 따라 다름",
            estimatedTime     = "경로에 따라 다름",
            waypoints         = waypoints
        };
    }

    // Nearest Neighbor 알고리즘 — 현재 위치에서 가장 가까운 미방문 전시품을 순서대로 선택
    private static Exhibit[] OptimizeVisitOrder(Exhibit[] exhibits)
    {
        if (exhibits.Length <= 2) return exhibits;

        bool[]    visited = new bool[exhibits.Length];
        Exhibit[] result  = new Exhibit[exhibits.Length];

        // 첫 번째 전시품을 시작점으로 고정
        visited[0] = true;
        result[0]  = exhibits[0];

        for (int step = 1; step < exhibits.Length; step++)
        {
            float minDist = float.MaxValue;
            int   nearest = -1;

            for (int j = 0; j < exhibits.Length; j++)
            {
                if (visited[j]) continue;

                float dist = Vector3.Distance(result[step - 1].localPosition, exhibits[j].localPosition);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = j;
                }
            }

            if (nearest >= 0)
            {
                visited[nearest] = true;
                result[step]     = exhibits[nearest];
            }
        }

        return result;
    }
}
