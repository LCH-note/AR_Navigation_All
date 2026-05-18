using UnityEngine;

/// <summary>
/// AR 공간에 배치되는 도슨트 패널의 데이터 정보를 저장하는 클래스
/// 기준 앵커(Anchor) 대비 상대 좌표로 위치를 관리
/// </summary>
[System.Serializable]
public class DocentPanel
{
    [Header("도슨트 패널 기본 정보")]
    public string id;                   // 패널 고유 ID
    public int mapID;                   // 어느 맵에 배치되는지

    [Header("위치 정보 (Anchor 대비 상대 좌표)")]
    public Vector3 relativePosition;    // 해당 맵 앵커 기준 로컬 상대 좌표

    [Header("콘텐츠")]
    [TextArea(3, 8)]
    public string content;              // 전시품 설명 텍스트

    public DocentPanel() { }

    public DocentPanel(string id, int mapID, Vector3 relativePosition, string content)
    {
        this.id = id;
        this.mapID = mapID;
        this.relativePosition = relativePosition;
        this.content = content;
    }
}
