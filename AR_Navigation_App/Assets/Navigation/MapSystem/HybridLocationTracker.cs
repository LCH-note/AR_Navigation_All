using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스캔된 영역(Immersal 정밀 측위)과 미스캔 영역(추정 위치)을 구분하여
/// 사용자의 현재 위치와 추적 정확도를 관리하는 하이브리드 위치 추적 시스템
/// </summary>
public class HybridLocationTracker : MonoBehaviour
{
    /// <summary>
    /// 위치 추적 정확도 레벨
    /// </summary>
    public enum LocationAccuracy
    {
        None,       // 위치 추적 불가 (Localization 실패)
        Estimated,  // 추정 위치 (미스캔 영역, ±1m 이상 오차)
        Accurate    // 정확한 위치 (스캔 영역, ±10cm 이내)
    }

    [Header("참조")]
    [SerializeField] private ImmersalLocationConverter locationConverter;

    [Header("영역 정의")]
    [SerializeField] private List<ScannedRegion> regions = new List<ScannedRegion>();

    [Header("현재 추적 상태 (읽기 전용)")]
    [SerializeField] private Vector2 currentPosition2D;         // 현재 2D 추적 위치
    [SerializeField] private LocationAccuracy currentAccuracy;  // 현재 정확도

    [Header("디버그")]
    [SerializeField] private bool enableDebugLog = true;

    private void Awake()
    {
        if (locationConverter == null)
            locationConverter = GetComponent<ImmersalLocationConverter>();

        InitializeRegions();
    }

    private void Start()
    {
        if (enableDebugLog)
            RunSimulationTest();
    }

    /// <summary>
    /// 기본 영역 정의 초기화 (전시실, 로비, 복도)
    /// </summary>
    private void InitializeRegions()
    {
        if (regions.Count > 0) return;

        // 스캔된 영역: 전시실 (5,5) ~ (35,25)
        regions.Add(new ScannedRegion(
            "전시실",
            new Vector2(5f, 5f),
            new Vector2(35f, 25f),
            true,
            new int[] { 1, 2 }
        ));

        // 미스캔 영역: 로비 (0,0) ~ (5,40)
        regions.Add(new ScannedRegion(
            "로비",
            new Vector2(0f, 0f),
            new Vector2(5f, 40f),
            false,
            new int[] { }
        ));

        // 미스캔 영역: 복도 (35,0) ~ (50,40)
        regions.Add(new ScannedRegion(
            "복도",
            new Vector2(35f, 0f),
            new Vector2(50f, 40f),
            false,
            new int[] { }
        ));
    }

    /// <summary>
    /// 주어진 2D 좌표가 스캔된 영역 안에 있는지 확인
    /// </summary>
    public bool IsInScannedRegion(Vector2 position2D)
    {
        foreach (var region in regions)
        {
            if (region.isScanned && region.Contains(position2D))
                return true;
        }
        return false;
    }

    /// <summary>
    /// ImmersalLocationConverter를 통해 현재 사용자 2D 위치 반환
    /// Localization 실패 시 마지막 유효 위치 유지
    /// </summary>
    public Vector2 GetUserPosition()
    {
        if (locationConverter == null || !locationConverter.HasValidPose())
            return currentPosition2D;  // 마지막 유효 위치 유지

        Vector3 unified3D = locationConverter.ConvertToUnified();
        currentPosition2D = new Vector2(unified3D.x, unified3D.z);
        return currentPosition2D;
    }

    /// <summary>
    /// 스캔 영역 여부를 기반으로 현재 위치 정확도 판단
    /// </summary>
    public LocationAccuracy DetermineAccuracy()
    {
        if (locationConverter == null || !locationConverter.HasValidPose())
        {
            currentAccuracy = LocationAccuracy.None;
            return LocationAccuracy.None;
        }

        Vector2 pos = GetUserPosition();
        currentAccuracy = IsInScannedRegion(pos)
            ? LocationAccuracy.Accurate
            : LocationAccuracy.Estimated;

        return currentAccuracy;
    }

    /// <summary>현재 정확도 상태를 외부에서 읽을 수 있는 프로퍼티</summary>
    public LocationAccuracy CurrentAccuracy => currentAccuracy;

    /// <summary>
    /// ARNavigationController 등 외부에서 실시간 계산된 2D 위치를 직접 전달합니다.
    /// ImmersalLocationConverter 없이도 정확도 상태를 유지합니다.
    /// </summary>
    public void UpdateFromExternalPosition(Vector2 pos2D)
    {
        currentPosition2D = pos2D;
        currentAccuracy = IsInScannedRegion(pos2D)
            ? LocationAccuracy.Accurate
            : LocationAccuracy.Estimated;
    }

    /// <summary>
    /// 정확도에 따른 마커 색상 반환
    /// Accurate=초록, Estimated=노랑, None=빨강
    /// </summary>
    public Color GetMarkerColor()
    {
        return currentAccuracy switch
        {
            LocationAccuracy.Accurate  => Color.green,
            LocationAccuracy.Estimated => Color.yellow,
            LocationAccuracy.None      => Color.red,
            _                          => Color.gray
        };
    }

    /// <summary>
    /// 주어진 좌표에서의 상태 로그 출력
    /// </summary>
    private void PrintStatusAt(Vector2 pos)
    {
        string region = GetRegionNameAt(pos);
        bool inScanned = IsInScannedRegion(pos);
        string accuracy = inScanned ? "정확함 (±10cm)" : "추정 (±1m 이상)";
        string color = inScanned ? "Green" : "Yellow";

        Debug.Log($"위치: {pos.x:F1}, {pos.y:F1}");
        Debug.Log($"영역: {region}");
        Debug.Log($"정확도: {accuracy}");
        Debug.Log($"마커색: {color}");
        Debug.Log("---");
    }

    /// <summary>
    /// 2D 좌표가 속한 영역 이름 반환
    /// </summary>
    private string GetRegionNameAt(Vector2 pos)
    {
        foreach (var region in regions)
        {
            if (region.Contains(pos))
                return region.regionName;
        }
        return "알 수 없는 영역";
    }

    /// <summary>
    /// 3가지 위치 시뮬레이션 테스트 (전시실, 로비, 복도)
    /// </summary>
    private void RunSimulationTest()
    {
        Debug.Log("=== 하이브리드 위치 추적 시뮬레이션 ===");

        // 테스트1: 전시실 중앙 (스캔 영역)
        Debug.Log("[테스트1] 전시실 중앙 (20, 15)");
        PrintStatusAt(new Vector2(20f, 15f));

        // 테스트2: 로비 (미스캔 영역)
        Debug.Log("[테스트2] 로비 (2, 20)");
        PrintStatusAt(new Vector2(2f, 20f));

        // 테스트3: 복도 (미스캔 영역)
        Debug.Log("[테스트3] 복도 (40, 10)");
        PrintStatusAt(new Vector2(40f, 10f));

        Debug.Log("========================================");
    }
}
