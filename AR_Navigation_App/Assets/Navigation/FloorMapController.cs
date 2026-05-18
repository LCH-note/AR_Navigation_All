/*
    파일명: Assets/Navigation/FloorMapController.cs
    역할: 전체 지도 화면 (MapScreen.uxml) — 플로어별 2D 평면도 표시 컨트롤러
    연동: UIManager → OnScreenShown() 호출
         DataSyncManager.FloorPlanTextures 딕셔너리에서 Texture2D 읽어옴
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class FloorMapController : MonoBehaviour
{
    // ── 플로어 탭 버튼 이름 매핑 ──────────────────────────────────────
    private static readonly string[] FloorKeys   = { "B1", "1F", "2F", "3F" };
    private static readonly string[] TabBtnNames = { "btn-floor-B1", "btn-floor-1F", "btn-floor-2F", "btn-floor-3F" };

    // ── UI 요소 참조 ──────────────────────────────────────────────────
    private VisualElement              _root;
    private VisualElement              _mapImage;
    private Label                      _noMapLabel;
    private readonly List<Button>      _tabButtons = new List<Button>();

    // ── 현재 선택 플로어 ──────────────────────────────────────────────
    private string _activeFloor = "1F";

    // ── 초기화 (UIManager에서 화면 루트를 전달해 호출) ────────────────
    public void Initialize(VisualElement mapScreen)
    {
        _root       = mapScreen;
        _mapImage   = _root.Q<VisualElement>("map-image");
        _noMapLabel = _root.Q<Label>("label-no-map");

        // 탭 버튼 참조 수집 + 클릭 핸들러 등록
        _tabButtons.Clear();
        for (int i = 0; i < FloorKeys.Length; i++)
        {
            string floor   = FloorKeys[i];
            Button btn     = _root.Q<Button>(TabBtnNames[i]);
            if (btn == null) continue;
            _tabButtons.Add(btn);
            btn.clicked += () => SelectFloor(floor);
        }

        // 데이터가 이미 준비된 경우 즉시 반영
        if (DataSyncManager.IsDataReady)
            ShowFloor(_activeFloor);
        else
            DataSyncManager.OnDataReady += OnDataReady;
    }

    // ── 화면이 표시될 때 UIManager가 호출 ─────────────────────────────
    public void OnScreenShown()
    {
        if (DataSyncManager.IsDataReady)
            ShowFloor(_activeFloor);
    }

    // ── 데이터 로드 완료 콜백 ─────────────────────────────────────────
    private void OnDataReady()
    {
        DataSyncManager.OnDataReady -= OnDataReady;
        ShowFloor(_activeFloor);
    }

    // ── 플로어 탭 선택 ────────────────────────────────────────────────
    private void SelectFloor(string floor)
    {
        _activeFloor = floor;

        // 탭 활성 스타일 갱신
        for (int i = 0; i < FloorKeys.Length; i++)
        {
            if (i >= _tabButtons.Count) break;
            bool isActive = FloorKeys[i] == floor;
            if (isActive)
                _tabButtons[i].AddToClassList("floor-tab--active");
            else
                _tabButtons[i].RemoveFromClassList("floor-tab--active");
        }

        ShowFloor(floor);
    }

    // ── 해당 플로어 평면도 이미지 표시 ───────────────────────────────
    private void ShowFloor(string floor)
    {
        if (_mapImage == null || _noMapLabel == null) return;

        var textures = DataSyncManager.FloorPlanTextures;

        if (textures != null && textures.TryGetValue(floor, out Texture2D tex) && tex != null)
        {
            // 텍스처를 backgroundImage로 설정
            _mapImage.style.backgroundImage = new StyleBackground(tex);
            _mapImage.RemoveFromClassList("hidden");
            _noMapLabel.AddToClassList("hidden");
        }
        else
        {
            // 해당 플로어 평면도 없음
            _mapImage.style.backgroundImage = StyleKeyword.None;
            _mapImage.AddToClassList("hidden");
            _noMapLabel.RemoveFromClassList("hidden");
        }
    }

    private void OnDestroy()
    {
        DataSyncManager.OnDataReady -= OnDataReady;
    }
}
