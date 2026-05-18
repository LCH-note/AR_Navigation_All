using UnityEngine;

/// <summary>
/// Immersal로 스캔된 영역과 미스캔 영역의 바운딩 박스 정보를 저장하는 클래스
/// 하이브리드 위치 추적 시 정확도 판단에 사용
/// </summary>
[System.Serializable]
public class ScannedRegion
{
    [Header("영역 기본 정보")]
    public string regionName;           // 영역 이름 (예: 전시실, 로비, 복도)
    public bool isScanned;              // Immersal 스캔 완료 여부

    [Header("2D 평면도 바운딩 박스")]
    public Vector2 topLeft;             // 좌상단 좌표 (X, Z)
    public Vector2 bottomRight;         // 우하단 좌표 (X, Z)

    [Header("포함된 Immersal 맵 ID들")]
    public int[] immersalMapIDs;        // 이 영역을 구성하는 Immersal 맵 ID 배열

    public ScannedRegion() { }

    public ScannedRegion(string regionName, Vector2 topLeft, Vector2 bottomRight,
                         bool isScanned, int[] immersalMapIDs)
    {
        this.regionName = regionName;
        this.topLeft = topLeft;
        this.bottomRight = bottomRight;
        this.isScanned = isScanned;
        this.immersalMapIDs = immersalMapIDs;
    }

    /// <summary>
    /// 주어진 2D 좌표가 이 영역의 바운딩 박스 안에 있는지 확인
    /// </summary>
    public bool Contains(Vector2 point)
    {
        return point.x >= topLeft.x && point.x <= bottomRight.x &&
               point.y >= topLeft.y && point.y <= bottomRight.y;
    }
}
