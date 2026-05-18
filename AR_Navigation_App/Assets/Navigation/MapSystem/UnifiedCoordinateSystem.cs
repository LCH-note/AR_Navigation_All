using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 여러 Immersal 맵 간의 통합 좌표계를 관리하는 시스템
/// 각 맵의 로컬 좌표를 단일 월드 좌표계로 변환하여 크로스맵 네비게이션 지원
/// </summary>
public class UnifiedCoordinateSystem : MonoBehaviour
{
    [Header("앵커 설정")]
    [SerializeField] private List<Anchor> anchors = new List<Anchor>();

    [Header("좌표 범위 검증")]
    [SerializeField] private float maxCoordValue = 1000f;   // 허용 최대 좌표 절대값

    [Header("디버그")]
    [SerializeField] private bool enableDebugLog = true;    // 변환 과정 로그 출력 여부

    private void Awake()
    {
        // Inspector에 앵커가 없을 경우 기본값 초기화
        InitializeDefaultAnchors();
    }

    private void Start()
    {
        RunExampleConversions();
    }

    /// <summary>
    /// 기본 앵커 초기화 — Anchor1=(0,0,0), Anchor2=(20,0,0)
    /// </summary>
    private void InitializeDefaultAnchors()
    {
        if (anchors.Count > 0) return;

        // 맵1 기준점: 통합 좌표 원점 (0, 0, 0)
        anchors.Add(new Anchor(1, new Vector3(0f, 0f, 0f), "Anchor1"));
        // 맵2 기준점: 맵1 기준으로 X 방향 20m 이동
        anchors.Add(new Anchor(2, new Vector3(20f, 0f, 0f), "Anchor2"));

        if (enableDebugLog)
        {
            Debug.Log("[통합좌표계] 기본 앵커 초기화 완료");
            PrintAnchorInfo();
        }
    }

    /// <summary>
    /// 앵커 추가 (동일 맵 ID 기존 앵커 자동 교체)
    /// </summary>
    public void AddAnchor(Anchor anchor)
    {
        // 동일 맵 ID 앵커 제거 후 신규 등록
        anchors.RemoveAll(a => a.mapID == anchor.mapID);
        anchors.Add(anchor);

        if (enableDebugLog)
            Debug.Log($"[통합좌표계] 앵커 등록: {anchor}");
    }

    /// <summary>
    /// 맵 ID로 앵커 조회
    /// </summary>
    public Anchor GetAnchor(int mapID)
    {
        return anchors.Find(a => a.mapID == mapID);
    }

    /// <summary>
    /// 로컬 좌표 → 통합 좌표 변환
    /// 통합좌표 = 앵커의 통합좌표 + 맵 로컬 좌표
    /// </summary>
    public Vector3 LocalToUnified(int mapID, Vector3 localPos)
    {
        Anchor anchor = GetAnchor(mapID);
        if (anchor == null)
        {
            Debug.LogWarning($"[통합좌표계] 맵ID={mapID} 앵커 없음 — 변환 실패");
            return Vector3.zero;
        }

        Vector3 unifiedPos = anchor.unifiedPosition + localPos;

        if (enableDebugLog)
            Debug.Log($"[통합좌표계] 로컬→통합: 맵{mapID} {localPos} → 통합{unifiedPos}");

        ValidateCoordinate(unifiedPos);
        return unifiedPos;
    }

    /// <summary>
    /// 통합 좌표 → 로컬 좌표 변환
    /// 로컬좌표 = 통합좌표 - 앵커의 통합좌표
    /// </summary>
    public Vector3 UnifiedToLocal(int mapID, Vector3 unifiedPos)
    {
        Anchor anchor = GetAnchor(mapID);
        if (anchor == null)
        {
            Debug.LogWarning($"[통합좌표계] 맵ID={mapID} 앵커 없음 — 역변환 실패");
            return Vector3.zero;
        }

        Vector3 localPos = unifiedPos - anchor.unifiedPosition;

        if (enableDebugLog)
            Debug.Log($"[통합좌표계] 통합→로컬: 맵{mapID} 통합{unifiedPos} → 로컬{localPos}");

        return localPos;
    }

    /// <summary>
    /// 3D 통합 좌표 → 2D 평면 좌표 변환 (Y축 제거, XZ 평면만 사용)
    /// </summary>
    public Vector2 LocalTo2D(Vector3 unifiedPos)
    {
        Vector2 pos2D = new Vector2(unifiedPos.x, unifiedPos.z);

        if (enableDebugLog)
            Debug.Log($"[통합좌표계] 3D→2D: {unifiedPos} → {pos2D}");

        return pos2D;
    }

    /// <summary>
    /// 좌표 범위 유효성 검증
    /// </summary>
    private bool ValidateCoordinate(Vector3 pos)
    {
        bool inRange = Mathf.Abs(pos.x) <= maxCoordValue &&
                       Mathf.Abs(pos.y) <= maxCoordValue &&
                       Mathf.Abs(pos.z) <= maxCoordValue;

        if (!inRange)
            Debug.LogWarning($"[통합좌표계] 범위 초과 좌표 감지: {pos} (최대 ±{maxCoordValue})");

        return inRange;
    }

    /// <summary>
    /// 현재 등록된 앵커 정보 전체 출력
    /// </summary>
    public void PrintAnchorInfo()
    {
        Debug.Log($"[통합좌표계] 등록된 앵커: {anchors.Count}개");
        foreach (var a in anchors)
            Debug.Log($"  └─ {a}");
    }

    /// <summary>
    /// 사전 설정된 박물관 예시 좌표 변환 테스트
    /// </summary>
    private void RunExampleConversions()
    {
        if (!enableDebugLog) return;

        Debug.Log("=== 통합 좌표계 변환 예시 (박물관 전시실) ===");

        Debug.Log("── 맵1 포인트 ──");
        LocalToUnified(1, new Vector3(5, 0, 10));
        LocalToUnified(1, new Vector3(10, 0, 15));

        Debug.Log("── 맵2 포인트 ──");
        LocalToUnified(2, new Vector3(5, 0, 5));
        LocalToUnified(2, new Vector3(10, 0, 10));

        Debug.Log("=============================================");
    }
}
