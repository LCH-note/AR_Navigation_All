using UnityEngine;

/// <summary>
/// 네비게이션 경로의 경유 지점 데이터 클래스
/// 기준 앵커(Anchor) 대비 상대 좌표로 위치를 관리
/// NavWaypoint(Immersal 로컬 좌표 기반)와 구분되는 앵커 상대 좌표 기반 웨이포인트
/// </summary>
[System.Serializable]
public class NavigationWaypoint
{
    [Header("웨이포인트 기본 정보")]
    public string id;                   // 웨이포인트 고유 ID
    public int mapID;                   // 어느 맵에 속하는지

    [Header("위치 정보 (Anchor 대비 상대 좌표)")]
    public Vector3 relativePosition;    // 해당 맵 앵커 기준 로컬 상대 좌표

    public NavigationWaypoint() { }

    public NavigationWaypoint(string id, int mapID, Vector3 relativePosition)
    {
        this.id = id;
        this.mapID = mapID;
        this.relativePosition = relativePosition;
    }
}
