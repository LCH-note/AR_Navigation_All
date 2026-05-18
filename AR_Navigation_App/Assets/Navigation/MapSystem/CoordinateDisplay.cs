using UnityEngine;

/// <summary>
/// 3D 통합 좌표를 2D 지도 픽셀 좌표로 변환하는 디스플레이 시스템
/// 실제 미터 단위 좌표 ↔ Canvas 픽셀 좌표 간 양방향 변환 지원
/// </summary>
public class CoordinateDisplay : MonoBehaviour
{
    [Header("맵 설정")]
    [SerializeField] private MapSettings mapSettings = new MapSettings();

    [Header("맵 원점 (2D 좌표계 기준점)")]
    [SerializeField] private Vector2 mapOrigin = Vector2.zero;  // 맵 (0,0) 실제 위치

    [Header("디버그")]
    [SerializeField] private bool enableDebugLog = true;

    private void Start()
    {
        RunExampleConversions();
    }

    /// <summary>
    /// 3D 통합 좌표 → 2D 좌표 변환 (Y축 무시, XZ 평면 사용)
    /// </summary>
    public Vector2 UnifiedTo2D(Vector3 unifiedPos)
    {
        Vector2 pos2D = new Vector2(unifiedPos.x, unifiedPos.z);

        if (enableDebugLog)
            Debug.Log($"[좌표표시] 3D→2D: {unifiedPos} → {pos2D}");

        return pos2D;
    }

    /// <summary>
    /// 실제 거리(2D 월드 좌표) → Canvas 픽셀 좌표 변환
    /// 맵 크기 대비 비율로 Canvas 좌표 계산
    /// </summary>
    public Vector2 WorldToCanvasPosition(Vector2 worldPos2D)
    {
        // 맵 원점 기준 상대 위치
        Vector2 rel = worldPos2D - mapOrigin;

        // 실제 크기 대비 비율로 픽셀 환산
        float cx = (rel.x / mapSettings.mapWidth) * mapSettings.canvasWidth;
        float cy = (rel.y / mapSettings.mapHeight) * mapSettings.canvasHeight;

        Vector2 canvasPos = new Vector2(cx, cy);
        bool inRange = IsWithinCanvas(canvasPos);

        if (enableDebugLog)
            Debug.Log($"[좌표표시] 2D→Canvas: {worldPos2D} → {canvasPos} ({(inRange ? "범위 내" : "범위 초과")})");

        return canvasPos;
    }

    /// <summary>
    /// Canvas 픽셀 좌표 → 실제 거리(2D 월드 좌표) 역변환
    /// </summary>
    public Vector2 CanvasToWorld(Vector2 canvasPos)
    {
        float wx = (canvasPos.x / mapSettings.canvasWidth) * mapSettings.mapWidth;
        float wz = (canvasPos.y / mapSettings.canvasHeight) * mapSettings.mapHeight;

        Vector2 worldPos = new Vector2(wx, wz) + mapOrigin;

        if (enableDebugLog)
            Debug.Log($"[좌표표시] Canvas→2D: {canvasPos} → {worldPos}");

        return worldPos;
    }

    /// <summary>
    /// Canvas 픽셀 좌표가 유효 범위(0~W, 0~H) 안에 있는지 확인
    /// </summary>
    public bool IsWithinCanvas(Vector2 canvasPos)
    {
        return canvasPos.x >= 0 && canvasPos.x <= mapSettings.canvasWidth &&
               canvasPos.y >= 0 && canvasPos.y <= mapSettings.canvasHeight;
    }

    /// <summary>
    /// 범위를 벗어난 Canvas 좌표를 유효 범위로 클램핑
    /// </summary>
    public Vector2 ClampToCanvas(Vector2 canvasPos)
    {
        return new Vector2(
            Mathf.Clamp(canvasPos.x, 0f, mapSettings.canvasWidth),
            Mathf.Clamp(canvasPos.y, 0f, mapSettings.canvasHeight)
        );
    }

    /// <summary>
    /// 예시 좌표로 변환 파이프라인 테스트
    /// </summary>
    private void RunExampleConversions()
    {
        if (!enableDebugLog) return;

        Debug.Log("=== 좌표 변환 예시 ===");

        // 포인트1: (5, 0, 10) 3D → Canvas
        Vector3 p1_3D = new Vector3(5f, 0f, 10f);
        Vector2 p1_2D = UnifiedTo2D(p1_3D);
        Vector2 p1_cv = WorldToCanvasPosition(p1_2D);
        Debug.Log($"포인트1: 3D{p1_3D} → 2D{p1_2D} → Canvas{p1_cv}");

        // 포인트2: (25, 0, 15) 3D → Canvas
        Vector3 p2_3D = new Vector3(25f, 0f, 15f);
        Vector2 p2_2D = UnifiedTo2D(p2_3D);
        Vector2 p2_cv = WorldToCanvasPosition(p2_2D);
        Debug.Log($"포인트2: 3D{p2_3D} → 2D{p2_2D} → Canvas{p2_cv}");

        // 역변환 검증
        Debug.Log("── 역변환 검증 ──");
        Vector2 back1 = CanvasToWorld(p1_cv);
        Debug.Log($"역변환1: Canvas{p1_cv} → 2D{back1} (원본: {p1_2D})");

        Debug.Log("====================");
    }
}
