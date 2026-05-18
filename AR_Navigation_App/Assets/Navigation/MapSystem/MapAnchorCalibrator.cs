using UnityEngine;

/// <summary>
/// AR Space와 AR Space 2의 자식 앵커 오브젝트 위치를 읽어
/// UnifiedCoordinateSystem의 맵 오프셋을 자동 보정하는 컴포넌트.
///
/// 원리: Anchor1과 Anchor2는 현실 공간에서 동일한 지점을 가리킨다.
/// - Anchor1.localPosition = 맵1 로컬 좌표계에서의 기준점 위치
/// - Anchor2.localPosition = 맵2 로컬 좌표계에서의 기준점 위치
/// - 맵2 통합좌표 오프셋 = Anchor1.localPosition - Anchor2.localPosition
/// </summary>
public class MapAnchorCalibrator : MonoBehaviour
{
    [Header("앵커 오브젝트 참조")]
    [Tooltip("AR Space의 자식 — 맵1 기준점 (Anchor 1 GameObject)")]
    [SerializeField] private Transform anchor1Transform;   // AR Space 자식

    [Tooltip("AR Space 2의 자식 — 맵2 기준점 (Anchor 2 GameObject)")]
    [SerializeField] private Transform anchor2Transform;   // AR Space 2 자식

    [Header("보정 대상")]
    [SerializeField] private UnifiedCoordinateSystem coordinateSystem;

    [Header("맵 ID 설정")]
    [SerializeField] private int map1ID = 1;   // AR Space (145962)
    [SerializeField] private int map2ID = 2;   // AR Space 2 (145963)

    [Header("디버그")]
    [SerializeField] private bool enableDebugLog = true;

    private void Awake()
    {
        if (coordinateSystem == null)
            coordinateSystem = GetComponent<UnifiedCoordinateSystem>();

        if (coordinateSystem == null)
        {
            Debug.LogError("[앵커보정] UnifiedCoordinateSystem 참조 없음");
            return;
        }

        Calibrate();
    }

    /// <summary>
    /// 앵커 오브젝트 위치로 통합좌표계 오프셋을 계산하고 등록한다.
    /// </summary>
    public void Calibrate()
    {
        if (anchor1Transform == null || anchor2Transform == null)
        {
            Debug.LogWarning("[앵커보정] Anchor1 또는 Anchor2 Transform이 연결되지 않았습니다.");
            return;
        }

        // 맵1의 통합좌표 오프셋: 원점 (0, 0, 0) 고정
        Vector3 map1Offset = Vector3.zero;

        // 맵2의 통합좌표 오프셋 계산
        // Anchor1(맵1 로컬) - Anchor2(맵2 로컬) = 맵2의 원점이 통합좌표에서 어디 있는지
        Vector3 a1Local = anchor1Transform.localPosition;  // AR Space 기준 로컬 좌표
        Vector3 a2Local = anchor2Transform.localPosition;  // AR Space 2 기준 로컬 좌표
        Vector3 map2Offset = a1Local - a2Local;

        // 맵1 앵커 등록 (항상 원점)
        coordinateSystem.AddAnchor(new Anchor(map1ID, map1Offset, "Anchor1_Map1"));

        // 맵2 앵커 등록 (계산된 오프셋)
        coordinateSystem.AddAnchor(new Anchor(map2ID, map2Offset, "Anchor2_Map2"));

        if (enableDebugLog)
        {
            Debug.Log($"[앵커보정] 완료");
            Debug.Log($"  Anchor1 로컬: {a1Local}  (맵{map1ID} 기준점)");
            Debug.Log($"  Anchor2 로컬: {a2Local}  (맵{map2ID} 기준점)");
            Debug.Log($"  맵{map1ID} 통합좌표 오프셋: {map1Offset}");
            Debug.Log($"  맵{map2ID} 통합좌표 오프셋: {map2Offset}");
        }
    }

    /// <summary>
    /// 인스펙터 우클릭 메뉴: Anchor 2의 월드 위치를 Anchor 1과 동일하게 맞춘다.
    /// </summary>
    [ContextMenu("Anchor 2를 Anchor 1 위치에 맞추기")]
    private void SnapAnchor2ToAnchor1()
    {
        if (anchor1Transform == null || anchor2Transform == null)
        {
            Debug.LogWarning("[앵커보정] Anchor1 또는 Anchor2가 연결되지 않았습니다.");
            return;
        }

#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(anchor2Transform, "Snap Anchor2 to Anchor1");
#endif
        // 월드 좌표(position)를 직접 맞추면 로컬 좌표는 Unity가 자동 계산
        anchor2Transform.position = anchor1Transform.position;

        Debug.Log($"[앵커보정] Anchor2 위치 조정 완료 → 월드: {anchor1Transform.position}  " +
                  $"Anchor2 로컬: {anchor2Transform.localPosition}");
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터 전용: 앵커 간 연결선을 Gizmo로 시각화
    /// </summary>
    private void OnDrawGizmos()
    {
        if (anchor1Transform == null || anchor2Transform == null) return;

        // 각 앵커 위치에 구체 표시
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(anchor1Transform.position, 0.1f);
        Gizmos.DrawSphere(anchor2Transform.position, 0.1f);

        // 두 앵커를 연결하는 선
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(anchor1Transform.position, anchor2Transform.position);

        // 라벨 표시 위치
        UnityEditor.Handles.Label(anchor1Transform.position + Vector3.up * 0.2f, "Anchor1 (맵1)");
        UnityEditor.Handles.Label(anchor2Transform.position + Vector3.up * 0.2f, "Anchor2 (맵2)");
    }
#endif
}
