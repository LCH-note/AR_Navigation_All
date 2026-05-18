using UnityEngine;

/// <summary>
/// 맵 기준점(앵커) 정보를 저장하는 클래스
/// 통합 좌표계에서 각 Immersal 맵의 원점 역할
/// </summary>
[System.Serializable]
public class Anchor
{
    [Header("앵커 기본 정보")]
    public int mapID;                   // 어느 맵에 속하는지
    public Vector3 unifiedPosition;     // 통합 좌표계에서의 기준 위치
    public string name;                 // 앵커 식별 이름

    public Anchor() { }

    public Anchor(int mapID, Vector3 unifiedPosition, string name)
    {
        this.mapID = mapID;
        this.unifiedPosition = unifiedPosition;
        this.name = name;
    }

    public override string ToString()
    {
        return $"Anchor[{name}] 맵ID={mapID}, 통합좌표={unifiedPosition}";
    }
}
