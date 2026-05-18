using System.Collections;
using UnityEngine;

/// <summary>
/// 모든 네비게이션 서브시스템을 통합하는 완전한 AR 네비게이션 시뮬레이션 컨트롤러
/// UnifiedCoordinateSystem → ImmersalLocationConverter → CoordinateDisplay → HybridLocationTracker
/// 10프레임 시뮬레이션으로 맵1→맵2 전환 전체 흐름을 검증
/// </summary>
public class ARNavigationSystem : MonoBehaviour
{
    [Header("서브시스템 참조")]
    [SerializeField] private UnifiedCoordinateSystem coordinateSystem;
    [SerializeField] private ImmersalLocationConverter locationConverter;
    [SerializeField] private NavigationPathManager pathManager;
    [SerializeField] private CoordinateDisplay coordinateDisplay;
    [SerializeField] private HybridLocationTracker locationTracker;

    [Header("시뮬레이션 설정")]
    [SerializeField] private int totalFrames = 10;      // 총 시뮬레이션 프레임
    [SerializeField] private float frameInterval = 1f;  // 프레임 간격 (초)

    private void Awake()
    {
        // 같은 GameObject에 붙어 있을 경우 자동 연결
        if (coordinateSystem == null) coordinateSystem = GetComponent<UnifiedCoordinateSystem>();
        if (locationConverter == null) locationConverter = GetComponent<ImmersalLocationConverter>();
        if (pathManager == null)       pathManager = GetComponent<NavigationPathManager>();
        if (coordinateDisplay == null) coordinateDisplay = GetComponent<CoordinateDisplay>();
        if (locationTracker == null)   locationTracker = GetComponent<HybridLocationTracker>();
    }

    private void Start()
    {
        Debug.Log("=== AR 네비게이션 통합 시스템 시작 ===");
        StartCoroutine(RunSimulation());
    }

    /// <summary>
    /// totalFrames 동안 1초 간격으로 전체 파이프라인 실행
    /// </summary>
    private IEnumerator RunSimulation()
    {
        for (int frame = 1; frame <= totalFrames; frame++)
        {
            ProcessFrame(frame);
            yield return new WaitForSeconds(frameInterval);
        }

        Debug.Log("=== 시뮬레이션 완료 ===");
    }

    /// <summary>
    /// 각 프레임: Immersal 위치 → 통합 좌표 → 2D 지도 → Canvas → 영역/정확도 판단 → 출력
    /// </summary>
    private void ProcessFrame(int frame)
    {
        Debug.Log($"\n[프레임 {frame}]");

        // 1. Immersal 위치 가져오기
        if (locationConverter == null)
        {
            Debug.Log("└─ ❌ ImmersalLocationConverter 없음");
            return;
        }

        Vector3 immersalPos = locationConverter.GetImmersalPosition();
        int mapID = locationConverter.GetActiveMapID();

        // 2. Localization 유효성 체크
        if (!locationConverter.HasValidPose())
        {
            Debug.Log($"├─ Immersal 위치: {immersalPos} (맵{mapID})");
            Debug.Log($"└─ ⚠️ Localization 불가 — 맵 전환 중");
            return;
        }

        // 3. 통합 좌표 변환
        Vector3 unifiedPos = Vector3.zero;
        if (coordinateSystem != null)
            unifiedPos = coordinateSystem.LocalToUnified(mapID, immersalPos);

        // 4. 2D 지도 좌표 변환
        Vector2 pos2D = Vector2.zero;
        Vector2 canvasPos = Vector2.zero;
        if (coordinateDisplay != null)
        {
            pos2D = coordinateDisplay.UnifiedTo2D(unifiedPos);
            canvasPos = coordinateDisplay.WorldToCanvasPosition(pos2D);

            // 범위 초과 처리
            if (!coordinateDisplay.IsWithinCanvas(canvasPos))
            {
                canvasPos = coordinateDisplay.ClampToCanvas(canvasPos);
                Debug.Log($"├─ ⚠️ Canvas 범위 초과 — 클램핑 적용");
            }
        }

        // 5. 영역 판단 및 정확도 결정
        bool inScanned = locationTracker != null && locationTracker.IsInScannedRegion(pos2D);
        string regionName = inScanned ? "전시실" : "미스캔 영역";
        string accuracyText = inScanned ? "정확함 ✅" : "추정 ⚠️";

        // 6. 요구사항 형식으로 출력
        Debug.Log($"├─ Immersal 위치: {immersalPos}");
        Debug.Log($"├─ 통합 좌표: {unifiedPos}");
        Debug.Log($"├─ 2D 지도: ({pos2D.x:F1}, {pos2D.y:F1})");
        Debug.Log($"├─ Canvas: ({canvasPos.x:F1}, {canvasPos.y:F1})");
        Debug.Log($"├─ 영역: {regionName}");
        Debug.Log($"└─ 정확도: {accuracyText}");
    }
}
