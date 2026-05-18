using UnityEngine;

/// <summary>
/// 전체 맵의 물리적 크기 및 Canvas 표시 영역 정보를 저장하는 설정 클래스
/// </summary>
[System.Serializable]
public class MapSettings
{
    [Header("맵 물리적 크기 (미터 단위)")]
    public float mapWidth = 50f;       // 맵 너비 (실제 미터)
    public float mapHeight = 40f;      // 맵 높이 (실제 미터)

    [Header("Canvas 표시 영역 (픽셀)")]
    public float canvasWidth = 800f;   // Canvas 표시 너비 (픽셀)
    public float canvasHeight = 600f;  // Canvas 표시 높이 (픽셀)
}
