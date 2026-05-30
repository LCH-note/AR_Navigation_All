/*
    파일명: Assets/Navigation/FloorMapController.cs
    역할: 전체 지도 화면 (MapScreen.uxml) — 층별 3D 전체도 표시 컨트롤러
    연동: UIManager → Initialize() / OnScreenShown() 호출
         DataSyncManager.ThreeDModelUrls (3D 전체도 URL 딕셔너리, 키: B1/1F/2F/3F)
         Map3DViewController             (RenderTexture 3D 뷰어)
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
    private VisualElement         _map3dView;      // map-3d-view (RenderTexture 출력)
    private Label                 _no3dMapLabel;
    private readonly List<Button> _tabButtons = new List<Button>();

    // ── 상태 ─────────────────────────────────────────────────────────
    private string _activeFloor = "1F";

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
        _map3dView    = _root.Q<VisualElement>("map-3d-view");
        _no3dMapLabel = _root.Q<Label>("label-no-3d-map");

        // 3D 뷰어 초기화 (아직 활성화하지 않음 — OnScreenShown() 에서 활성화)
        if (map3DViewController != null && _map3dView != null)
        {
            map3DViewController.Initialize(_map3dView, _no3dMapLabel);
            // SetActive(true) 는 OnScreenShown() 에서 호출 — 맵 화면이 실제로 표시될 때만 활성화
        }

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
            ShowFloor(_activeFloor);
        else
            DataSyncManager.OnDataReady += OnDataReady;
    }

    // ── 화면이 표시될 때 UIManager 가 호출 ───────────────────────────
    public void OnScreenShown()
    {
        // 3D 뷰어 활성화 (카메라 On, RawImage 캔버스 On, bounds 동기화 시작)
        map3DViewController?.SetActive(true);

        if (DataSyncManager.IsDataReady)
            ShowFloor(_activeFloor);
    }

    // ── 화면이 숨겨질 때 UIManager 가 호출 ───────────────────────────
    public void OnScreenHidden()
    {
        // 3D 뷰어 비활성화 (카메라 Off, RawImage 캔버스 Off → 다른 화면에 겹쳐 표시되지 않음)
        map3DViewController?.SetActive(false);
    }

    // ── 데이터 로드 완료 콜백 ─────────────────────────────────────────
    private void OnDataReady()
    {
        DataSyncManager.OnDataReady -= OnDataReady;
        ShowFloor(_activeFloor);
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
            if (FloorKeys[i] == floor)
                _tabButtons[i].AddToClassList("floor-tab--active");
            else
                _tabButtons[i].RemoveFromClassList("floor-tab--active");
        }

        ShowFloor(floor);
    }

    // ════════════════════════════════════════════════════════════════
    //  3D 모델 표시
    // ════════════════════════════════════════════════════════════════

    private void ShowFloor(string floor)
    {
        if (map3DViewController == null) return;

        var urls = DataSyncManager.ThreeDModelUrls;
        if (urls != null && urls.TryGetValue(floor, out string url) && !string.IsNullOrEmpty(url))
            map3DViewController.ShowModel(url);
        else
            map3DViewController.ShowNoModelMessage();
    }
}
