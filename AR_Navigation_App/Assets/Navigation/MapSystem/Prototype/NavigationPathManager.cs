using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 두 개의 Immersal 맵을 연결하는 크로스맵 네비게이션 경로를 관리하는 시스템
/// 맵별 로컬 좌표를 통합 좌표로 변환하여 전체 경로를 하나의 좌표계로 표현
/// </summary>
public class NavigationPathManager : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private UnifiedCoordinateSystem coordinateSystem;

    [Header("맵1 경로 포인트 (로컬 좌표)")]
    [SerializeField] private Vector3[] map1PathPoints = new Vector3[]
    {
        new Vector3(5f, 0f, 5f),
        new Vector3(10f, 0f, 10f)
    };

    [Header("맵2 경로 포인트 (로컬 좌표)")]
    [SerializeField] private Vector3[] map2PathPoints = new Vector3[]
    {
        new Vector3(0f, 0f, 0f),
        new Vector3(5f, 0f, 5f)
    };

    [Header("디버그")]
    [SerializeField] private bool enableDebugLog = true;

    private void Awake()
    {
        if (coordinateSystem == null)
            coordinateSystem = GetComponent<UnifiedCoordinateSystem>();
    }

    private void Start()
    {
        if (enableDebugLog)
            PrintFullPathInfo();
    }

    /// <summary>
    /// 전체 크로스맵 경로 데이터 반환 (맵ID + 로컬좌표 쌍)
    /// </summary>
    public List<(int mapID, Vector3 localPos)> GetCrossMapPath()
    {
        var path = new List<(int mapID, Vector3 localPos)>();

        // 맵1 경로 추가
        foreach (var pt in map1PathPoints)
            path.Add((1, pt));

        // 맵2 경로 추가
        foreach (var pt in map2PathPoints)
            path.Add((2, pt));

        return path;
    }

    /// <summary>
    /// 경로 전체를 통합 좌표로 변환하여 반환
    /// </summary>
    public List<Vector3> ConvertPathToUnified()
    {
        var unifiedPath = new List<Vector3>();
        var crossMapPath = GetCrossMapPath();

        for (int i = 0; i < crossMapPath.Count; i++)
        {
            var (mapID, localPos) = crossMapPath[i];
            Vector3 unified = coordinateSystem.LocalToUnified(mapID, localPos);
            unifiedPath.Add(unified);

            if (enableDebugLog)
                Debug.Log($"[경로관리자] 경로 포인트 {i + 1}: 맵{mapID} 로컬{localPos} → 통합{unified}");
        }

        return unifiedPath;
    }

    /// <summary>
    /// 특정 맵의 경로 구간만 통합 좌표로 반환
    /// </summary>
    public List<Vector3> GetPathSegmentByMap(int mapID)
    {
        Vector3[] points = mapID == 1 ? map1PathPoints : map2PathPoints;
        var segment = new List<Vector3>();

        foreach (var pt in points)
            segment.Add(coordinateSystem.LocalToUnified(mapID, pt));

        if (enableDebugLog)
            Debug.Log($"[경로관리자] 맵{mapID} 구간: {segment.Count}개 포인트");

        return segment;
    }

    /// <summary>
    /// 통합 좌표 기준 전체 경로 총 거리 계산 (미터)
    /// </summary>
    public float CalculateTotalDistance()
    {
        List<Vector3> path = ConvertPathToUnified();
        float total = 0f;

        for (int i = 0; i < path.Count - 1; i++)
        {
            float seg = Vector3.Distance(path[i], path[i + 1]);
            total += seg;

            if (enableDebugLog)
                Debug.Log($"[경로관리자] 구간 {i + 1}→{i + 2} 거리: {seg:F2}m");
        }

        return total;
    }

    /// <summary>
    /// 경로 유효성 검증 — 포인트 수 및 앵커 존재 여부 확인
    /// </summary>
    public bool ValidatePath()
    {
        var path = GetCrossMapPath();

        if (path.Count < 2)
        {
            Debug.LogWarning("[경로관리자] 유효성 실패: 경로 포인트 2개 미만");
            return false;
        }

        foreach (var (mapID, _) in path)
        {
            if (coordinateSystem.GetAnchor(mapID) == null)
            {
                Debug.LogWarning($"[경로관리자] 유효성 실패: 맵{mapID} 앵커 없음");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 전체 경로 정보 출력 (통합 좌표, 총 거리, 유효성)
    /// </summary>
    public void PrintFullPathInfo()
    {
        Debug.Log("=== 크로스맵 네비게이션 경로 ===");

        List<Vector3> unified = ConvertPathToUnified();
        for (int i = 0; i < unified.Count; i++)
            Debug.Log($"경로 포인트 {i + 1}: {unified[i]}");

        float dist = CalculateTotalDistance();
        Debug.Log($"총 거리: {dist:F2}m");
        Debug.Log($"경로 유효성: {(ValidatePath() ? "✅ 유효" : "❌ 오류")}");
        Debug.Log("=================================");
    }
}
