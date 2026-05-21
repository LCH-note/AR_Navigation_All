/*
    파일명: Assets/Navigation/FloorMapController.cs
    역할: 전체 지도 화면 (MapScreen.uxml) — 2D/3D 뷰 전환 및 플로어별 지도 표시 컨트롤러
    연동: UIManager → Initialize() / OnScreenShown() 호출
         DataSyncManager.FloorPlanTextures (2D 평면도 딕셔너리)
         DataSyncManager.ThreeDModelUrls   (3D 전체도 URL 딕셔너리)
         Map3DViewController               (RenderTexture 3D 뷰어)
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class FloorMapController : MonoBehaviour
{
    // ── Inspector 필드 ──────────────────────────────────────────────
    [Header("3D 전체도 뷰어")]
    [Tooltip("Map3DViewController 컴포넌트 — Inspector 에서 직접 연결 또는 자동 탐색")]
    [SerializeField] private Map3DViewController map3DViewController;

    // ── 플로어 탭 버튼 이름 매핑 ──────────────────────────────────────
    private static readonly string[] FloorKeys   = { "B1", "1F", "2F", "3F" };
    private static readonly string[] TabBtnNames = { "btn-floor-B1", "btn-floor-1F", "btn-floor-2F", "btn-floor-3F" };

    // ── UI 요소 참조 ──────────────────────────────────────────────────
    private VisualElement         _root;
    private VisualElement         _mapImageArea;   // 2D 뷰 컨테이너
    private VisualElement         _mapImage;
    private Label                 _noMapLabel;
    private VisualElement         _floorTabBar;
    private VisualElement         _map3dArea;      // 3D 뷰 컨테이너
    private VisualElement         _map3dView;      // map-3d-view (RenderTexture 출력)
    private Label                 _no3dMapLabel;
    private Button                _btnView2D;
    private Button                _btnView3D;
    private readonly List<Button> _tabButtons = new List<Button>();

    // ── 상태 ─────────────────────────────────────────────────────────
    private string _activeFloor = "1F";
    private bool   _is3DMode    = false;

    // ════════════════════════════════════════════════════════════════
    //  Unity 생명주기
    // ════════════════════════════════════════════════════════════════

    void Awake()
    {
        // Inspector 에서 연결되지 않은 경우 씬에서 자동 탐색
        if (map3DViewController == null)
            map3DViewController = FindObjectOfType<Map3DViewController>();
    }

    void OnDestroy()
    {
        DataSyncManager.OnDataReady -= OnDataReady;
    }

    // ════════════════════════════════════════════════════════════════
    //  초기화 (UIManager 에서 화면 루트를 전달해 호출)
    // ════════════════════════════════════════════════════════════════

    public void Initialize(VisualElement mapScreen)
    {
        _root         = mapScreen;
        _mapImageArea = _root.Q<VisualElement>("map-image-area");
        _mapImage     = _root.Q<VisualElement>("map-image");
        _noMapLabel   = _root.Q<Label>("label-no-map");
        _floorTabBar  = _root.Q<VisualElement>("floor-tab-bar");
        _map3dArea    = _root.Q<VisualElement>("map-3d-area");
        _map3dView    = _root.Q<VisualElement>("map-3d-view");
        _no3dMapLabel = _root.Q<Label>("label-no-3d-map");

        // 2D/3D 탭 버튼 연결
        _btnView2D = _root.Q<Button>("btn-view-2d");
        _btnView3D = _root.Q<Button>("btn-view-3d");
        if (_btnView2D != null) _btnView2D.clicked += () => SetViewMode(false);
        if (_btnView3D != null) _btnView3D.clicked += () => SetViewMode(true);

        // 3D 뷰어 초기화
        if (map3DViewController != null && _map3dView != null)
            map3DViewController.Initialize(_map3dView, _no3dMapLabel);

        // 플로어 탭 버튼 참조 수집 + 클릭 핸들러 등록
        _tabButtons.Clear();
        for (int i = 0; i < FloorKeys.Length; i++)
        {
            string floor = FloorKeys[i];
            Button btn   = _root.Q<Button>(TabBtnNames[i]);
            if (btn == null) continue;
            _tabButtons.Add(btn);
            btn.clicked += () => SelectFloor(floor);
        }

        // 데이터 준비 상태에 따라 초기 화면 반영
        if (DataSyncManager.IsDataReady)
            RefreshCurrentView();
        else
            DataSyncManager.OnDataReady += OnDataReady;
    }

    // ── 화면이 표시될 때 UIManager 가 호출 ───────────────────────────
    public void OnScreenShown()
    {
        if (DataSyncManager.IsDataReady)
            RefreshCurrentView();
    }

    // ── 데이터 로드 완료 콜백 ─────────────────────────────────────────
    private void OnDataReady()
    {
        DataSyncManager.OnDataReady -= OnDataReady;
        RefreshCurrentView();
    }

    // ════════════════════════════════════════════════════════════════
    //  뷰 모드 전환 (2D ↔ 3D)
    // ════════════════════════════════════════════════════════════════

    private void SetViewMode(bool is3D)
    {
        _is3DMode = is3D;

        // 탭 버튼 활성 스타일 갱신
        if (is3D)
        {
            _btnView2D?.RemoveFromClassList("view-mode-tab--active");
            _btnView3D?.AddToClassList("view-mode-tab--active");
        }
        else
        {
            _btnView2D?.AddToClassList("view-mode-tab--active");
            _btnView3D?.RemoveFromClassList("view-mode-tab--active");
        }

        // 영역 표시 / 숨김 전환
        if (is3D)
        {
            _floorTabBar?.AddToClassList("hidden");
            _mapImageArea?.AddToClassList("hidden");
            _map3dArea?.RemoveFromClassList("hidden");
            map3DViewController?.SetActive(true);
            Show3DFloor(_activeFloor);
        }
        else
        {
            _floorTabBar?.RemoveFromClassList("hidden");
            _mapImageArea?.RemoveFromClassList("hidden");
            _map3dArea?.AddToClassList("hidden");
            map3DViewController?.SetActive(false);
            ShowFloor2D(_activeFloor);
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  플로어 탭 선택
    // ════════════════════════════════════════════════════════════════

    private void SelectFloor(string floor)
    {
        _activeFloor = floor;

        // 탭 활성 스타일 갱신
        for (int i = 0; i < FloorKeys.Length; i++)
        {
            if (i >= _tabButtons.Count) break;
            bool isActive = FloorKeys[i] == floor;
            if (isActive) _tabButtons[i].AddToClassList("floor-tab--active");
            else          _tabButtons[i].RemoveFromClassList("floor-tab--active");
        }

        if (_is3DMode)
            Show3DFloor(floor);
        else
            ShowFloor2D(floor);
    }

    // ════════════════════════════════════════════════════════════════
    //  현재 모드에 맞는 뷰 갱신
    // ════════════════════════════════════════════════════════════════

    private void RefreshCurrentView()
    {
        if (_is3DMode)
            Show3DFloor(_activeFloor);
        else
            ShowFloor2D(_activeFloor);
    }

    // ── 2D 평면도 표시 ────────────────────────────────────────────────
    private void ShowFloor2D(string floor)
    {
        if (_mapImage == null || _noMapLabel == null) return;

        var textures = DataSyncManager.FloorPlanTextures;
        if (textures != null && textures.TryGetValue(floor, out Texture2D tex) && tex != null)
        {
            _mapImage.style.backgroundImage = new StyleBackground(tex);
            _mapImage.RemoveFromClassList("hidden");
            _noMapLabel.AddToClassList("hidden");
        }
        else
        {
            _mapImage.style.backgroundImage = StyleKeyword.None;
            _mapImage.AddToClassList("hidden");
            _noMapLabel.RemoveFromClassList("hidden");
        }
    }

    // ── 3D 전체도 표시 ────────────────────────────────────────────────
    private void Show3DFloor(string floor)
    {
        if (map3DViewController == null) return;

        var urls = DataSyncManager.ThreeDModelUrls;
        if (urls != null && urls.TryGetValue(floor, out string url))
            map3DViewController.ShowModel(url);
        else
            map3DViewController.ShowNoModelMessage();
    }
}
