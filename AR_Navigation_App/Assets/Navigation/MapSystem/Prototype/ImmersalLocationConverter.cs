using UnityEngine;

/// <summary>
/// Immersal SDK의 위치 정보를 통합 좌표계(UnifiedCoordinateSystem)로 변환하는 컴포넌트
/// 실기기에서는 Immersal SDK 연동, 에디터에서는 시뮬레이션 모드로 동작
/// </summary>
public class ImmersalLocationConverter : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private UnifiedCoordinateSystem coordinateSystem;  // 통합 좌표계

    [Header("현재 상태 (읽기 전용)")]
    [SerializeField] private int currentActiveMapID = 1;    // 현재 활성 Immersal 맵 ID
    [SerializeField] private bool hasValidPose = false;     // 유효한 Localization 여부

    [Header("시뮬레이션 설정")]
    [SerializeField] private bool useSimulation = true;             // 시뮬레이션 모드 사용 여부
    [SerializeField] private float simulationMoveSpeed = 0.1f;     // 시뮬레이션 이동 속도 (m/s)
    [SerializeField] private float mapSwitchInterval = 5f;         // 맵 전환 주기 (초)

    // 시뮬레이션 내부 상태
    private Vector3 _simLocalPosition;  // 시뮬레이션 로컬 좌표
    private float _elapsed = 0f;        // 경과 시간
    private int _prevMapID = -1;        // 이전 맵 ID (맵 전환 감지용)

    private void Awake()
    {
        if (coordinateSystem == null)
            coordinateSystem = GetComponent<UnifiedCoordinateSystem>();

        // 시뮬레이션 초기 위치: 맵1, (3, 0, 7)
        _simLocalPosition = new Vector3(3f, 0f, 7f);
        currentActiveMapID = 1;
        _prevMapID = 1;
    }

    private void Start()
    {
        hasValidPose = true;
        Debug.Log("[ImmersalConverter] 초기화 완료 — 맵1 시작 위치: (3, 0, 7)");
    }

    private void Update()
    {
        if (!useSimulation) return;

        _elapsed += Time.deltaTime;
        SimulateMapSwitch();
        SimulateMovement();
        DetectMapChange();
    }

    /// <summary>
    /// Immersal에서 현재 사용자 로컬 좌표 가져오기
    /// 시뮬레이션 모드: 내부 시뮬레이션 좌표 반환
    /// 실기기 모드: Immersal SDK XRSpace 로컬 포지션 반환 (TODO)
    /// </summary>
    public Vector3 GetImmersalPosition()
    {
        if (useSimulation)
            return _simLocalPosition;

        // TODO: 실기기 연동 시 아래 코드 활성화
        // var space = FindObjectOfType<Immersal.XR.XRSpace>();
        // return space != null ? space.transform.localPosition : Vector3.zero;
        return Vector3.zero;
    }

    /// <summary>
    /// 현재 활성 Immersal 맵 ID 반환
    /// 시뮬레이션: 타이머 기반 전환 / 실기기: SDK에서 직접 조회 (TODO)
    /// </summary>
    public int GetActiveMapID()
    {
        return currentActiveMapID;
    }

    /// <summary>
    /// Immersal 로컬 위치를 통합 좌표로 변환하여 반환
    /// </summary>
    public Vector3 ConvertToUnified()
    {
        if (!HasValidPose())
        {
            Debug.LogWarning("[ImmersalConverter] 유효한 Pose 없음 — 변환 불가 (맵 전환 중?)");
            return Vector3.zero;
        }

        if (coordinateSystem == null)
        {
            Debug.LogError("[ImmersalConverter] UnifiedCoordinateSystem 참조 없음");
            return Vector3.zero;
        }

        Vector3 localPos = GetImmersalPosition();
        int mapID = GetActiveMapID();
        Vector3 unified = coordinateSystem.LocalToUnified(mapID, localPos);

        Debug.Log($"[ImmersalConverter] 맵{mapID} 로컬{localPos} → 통합{unified}");
        return unified;
    }

    /// <summary>
    /// 유효한 Localization 위치 정보가 있는지 확인
    /// </summary>
    public bool HasValidPose()
    {
        return hasValidPose && coordinateSystem != null;
    }

    /// <summary>
    /// mapSwitchInterval 주기로 맵1↔맵2 전환 시뮬레이션
    /// </summary>
    private void SimulateMapSwitch()
    {
        float cycle = mapSwitchInterval * 2f;
        int newMapID = (_elapsed % cycle < mapSwitchInterval) ? 1 : 2;

        if (newMapID == currentActiveMapID) return;

        currentActiveMapID = newMapID;

        // 맵별 초기 위치: 맵1→(3,0,7), 맵2→(4,0,6)
        _simLocalPosition = currentActiveMapID == 1
            ? new Vector3(3f, 0f, 7f)
            : new Vector3(4f, 0f, 6f);
    }

    /// <summary>
    /// 매 프레임 시뮬레이션 위치 미세 이동 (XZ 방향)
    /// </summary>
    private void SimulateMovement()
    {
        _simLocalPosition += new Vector3(simulationMoveSpeed, 0f, simulationMoveSpeed) * Time.deltaTime;
    }

    /// <summary>
    /// 맵 전환 감지 시 Localization 재수렴 시뮬레이션
    /// </summary>
    private void DetectMapChange()
    {
        if (_prevMapID == currentActiveMapID) return;

        Debug.Log($"[ImmersalConverter] 맵 전환: {_prevMapID} → {currentActiveMapID}");

        // 전환 직후 잠시 위치 불확실 상태로 설정
        hasValidPose = false;
        Invoke(nameof(RestorePose), 0.5f);
        _prevMapID = currentActiveMapID;
    }

    /// <summary>
    /// 맵 전환 후 Localization 복구
    /// </summary>
    private void RestorePose()
    {
        hasValidPose = true;
        Debug.Log($"[ImmersalConverter] 맵{currentActiveMapID} Localization 완료");
    }
}
