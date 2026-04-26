/*
    파일명: UIManager.cs
    역할: 앱 내 모든 화면(Screen) 전환을 책임지는 중앙 UI 관리자
    관리 화면:
      StartScreen           → 앱 시작 화면
      MainScreen            → 메인 메뉴 (지도 보기 / 경로 선택)
      MapScreen             → 전체 지도 화면
      RouteSelectScreen     → 추천 경로 선택 화면
      RouteSelectUserScreen → 전시품 직접 선택 화면 (다중 선택 → 경유 경로 생성)
      ARMapScreen           → AR 내비게이션 실행 화면
    화면 전환 방식:
      hidden CSS 클래스 추가/제거로 표시/숨김 제어
    ARMapScreen 특이사항:
      화면 전환 시 ARMapScreenController.SetActive() 를 호출해
      가속도계/나침반 폴링을 활성화/비활성화
*/

using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    // ── Inspector 노출 필드 ───────────────────────────────────────────
    [Header("UI Document 연결")]
    [Tooltip("씬의 UIDocument 컴포넌트를 직접 연결 (다른 GameObject에 있으므로 GetComponent 불가)")]
    [SerializeField] private UIDocument uiDocument;

    [Header("AR 화면 컨트롤러")]
    [Tooltip("ARMapScreen 의 센서·지도 로직을 담당하는 컴포넌트")]
    [SerializeField] private ARMapScreenController arMapScreenController;

    [Header("내비게이션 컨트롤러")]
    [Tooltip("추천 경로 선택 화면 UI 로직")]
    [SerializeField] private RouteSelectController routeSelectController;

    [Tooltip("전시품 직접 선택 화면 UI 로직")]
    [SerializeField] private RouteSelectUserController routeSelectUserController;

    [Tooltip("AR 화살표 내비게이션 실행 컨트롤러")]
    [SerializeField] private ARNavigationController arNavigationController;

    // ── UI 내부 참조 (런타임에 UIDocument 에서 쿼리) ─────────────────
    private UIDocument    _uiDocument;
    private VisualElement _root;

    // 각 화면 인스턴스 (AppStructure.uxml 의 Instance 요소)
    private VisualElement _startScreen;
    private VisualElement _mainScreen;
    private VisualElement _mapScreen;
    private VisualElement _routeSelectScreen;
    private VisualElement _routeSelectUserScreen; // 전시품 직접 선택 화면
    private VisualElement _arMapScreen;

    // 버튼 참조
    private Button _btnStart;
    private Button _btnViewMap;
    private Button _btnSelectRoute;
    private Button _btnBackMap;
    private Button _btnBackRoute;
    private Button _btnUserRoute;            // 추천 경로 화면 → 전시품 선택 화면
    private Button _btnStartNavigation;      // 추천 경로 선택 후 "안내 시작"
    private Button _btnBackUserRoute;        // 전시품 선택 화면 "Back"
    private Button _btnStartUserNavigation;  // 전시품 선택 후 "안내 시작"
    private Button _btnBackAR;              // AR 화면 "← 나가기"
    private Button _btnTogglePath;           // AR 화면 경로선 토글

    // ════════════════════════════════════════════════════════════════
    //  Unity 생명주기
    // ════════════════════════════════════════════════════════════════

    void OnEnable()
    {
        // UIDocument 컴포넌트 획득
        // ※ UIDocument 는 별도의 GameObject 에 있으므로 GetComponent 대신
        //    Inspector 에서 직접 연결한 SerializeField 참조를 사용
        _uiDocument = uiDocument;
        if (_uiDocument == null)
        {
            Debug.LogError("UIManager: UIDocument 가 연결되지 않았습니다! " +
                           "Inspector 에서 UIDocument 필드를 연결해주세요.");
            return;
        }

        _root = _uiDocument.rootVisualElement;

        // 1. 각 화면 인스턴스 참조 획득
        _startScreen           = _root.Q<VisualElement>("StartScreenInstance");
        _mainScreen            = _root.Q<VisualElement>("MainScreenInstance");
        _mapScreen             = _root.Q<VisualElement>("MapScreenInstance");
        _routeSelectScreen     = _root.Q<VisualElement>("RouteSelectScreenInstance");
        _routeSelectUserScreen = _root.Q<VisualElement>("RouteSelectUserScreenInstance");
        _arMapScreen           = _root.Q<VisualElement>("ARMapScreenInstance");

        // 2. 각 화면의 버튼에 이벤트 연결
        if (_startScreen != null)
            SetupButton(_startScreen, "btn-start", ref _btnStart, OnStartClicked);

        if (_mainScreen != null)
        {
            SetupButton(_mainScreen, "btn-view-map",     ref _btnViewMap,     OnViewMapClicked);
            SetupButton(_mainScreen, "btn-select-route", ref _btnSelectRoute, OnSelectRouteClicked);
        }

        if (_mapScreen != null)
            SetupButton(_mapScreen, "btn-back-map", ref _btnBackMap, OnBackToMainClicked);

        if (_routeSelectScreen != null)
        {
            SetupButton(_routeSelectScreen, "btn-back-route",       ref _btnBackRoute,       OnBackToMainClicked);
            SetupButton(_routeSelectScreen, "btn-user-route",        ref _btnUserRoute,       OnUserRouteClicked);
            SetupButton(_routeSelectScreen, "btn-start-navigation",  ref _btnStartNavigation, OnStartNavigationClicked);
        }

        if (_routeSelectUserScreen != null)
        {
            SetupButton(_routeSelectUserScreen, "btn-back-user-route",       ref _btnBackUserRoute,       OnBackToRouteSelectClicked);
            SetupButton(_routeSelectUserScreen, "btn-start-user-navigation", ref _btnStartUserNavigation, OnStartUserNavigationClicked);
        }

        if (_arMapScreen != null)
        {
            SetupButton(_arMapScreen, "btn-back-ar",     ref _btnBackAR,      OnBackFromARClicked);
            SetupButton(_arMapScreen, "btn-toggle-path", ref _btnTogglePath,  OnTogglePathClicked);
        }

        // 3. 초기 화면: 시작 화면만 표시
        ShowScreen(_startScreen);
    }

    void OnDisable()
    {
        // 이벤트 구독 해제 (메모리 누수 방지)
        if (_btnStart           != null) _btnStart.clicked           -= OnStartClicked;
        if (_btnViewMap         != null) _btnViewMap.clicked         -= OnViewMapClicked;
        if (_btnSelectRoute     != null) _btnSelectRoute.clicked     -= OnSelectRouteClicked;
        if (_btnBackMap         != null) _btnBackMap.clicked         -= OnBackToMainClicked;
        if (_btnBackRoute       != null) _btnBackRoute.clicked       -= OnBackToMainClicked;
        if (_btnUserRoute           != null) _btnUserRoute.clicked           -= OnUserRouteClicked;
        if (_btnStartNavigation    != null) _btnStartNavigation.clicked    -= OnStartNavigationClicked;
        if (_btnBackUserRoute      != null) _btnBackUserRoute.clicked      -= OnBackToRouteSelectClicked;
        if (_btnStartUserNavigation!= null) _btnStartUserNavigation.clicked-= OnStartUserNavigationClicked;
        if (_btnBackAR             != null) _btnBackAR.clicked             -= OnBackFromARClicked;
        if (_btnTogglePath         != null) _btnTogglePath.clicked         -= OnTogglePathClicked;
    }

    // ════════════════════════════════════════════════════════════════
    //  버튼 이벤트 핸들러
    // ════════════════════════════════════════════════════════════════

    // 시작 화면 "Start" → 메인 화면
    private void OnStartClicked() => ShowScreen(_mainScreen);

    // 메인 화면 "View Full Map" → 전체 지도 화면
    private void OnViewMapClicked() => ShowScreen(_mapScreen);

    // 메인 화면 "Select Route" → 추천 경로 선택 화면
    private void OnSelectRouteClicked()
    {
        Debug.Log("UIManager: OnSelectRouteClicked() 호출됨");

        try { routeSelectController?.OnScreenShown(); }
        catch (System.Exception e)
        { Debug.LogError($"UIManager: RouteSelectController.OnScreenShown() 예외: {e}"); }

        if (_routeSelectScreen == null)
        {
            Debug.LogError("UIManager: _routeSelectScreen 이 null 입니다.");
            return;
        }
        ShowScreen(_routeSelectScreen);
    }

    // 추천 경로 화면 "직접 전시품 선택하기" → 전시품 선택 화면
    private void OnUserRouteClicked()
    {
        Debug.Log("UIManager: OnUserRouteClicked() 호출됨");

        try { routeSelectUserController?.OnScreenShown(); }
        catch (System.Exception e)
        { Debug.LogError($"UIManager: RouteSelectUserController.OnScreenShown() 예외: {e}"); }

        if (_routeSelectUserScreen == null)
        {
            Debug.LogError("UIManager: _routeSelectUserScreen 이 null 입니다.");
            return;
        }
        ShowScreen(_routeSelectUserScreen);
    }

    // 지도/경로 선택 화면 "Back" → 메인 화면
    private void OnBackToMainClicked() => ShowScreen(_mainScreen);

    // 전시품 선택 화면 "Back" → 추천 경로 선택 화면
    private void OnBackToRouteSelectClicked() => ShowScreen(_routeSelectScreen);

    // 경로 선택 화면 "내비게이션 시작" → AR 내비게이션 화면
    private void OnStartNavigationClicked()
    {
        NavRoute selectedRoute = routeSelectController?.GetSelectedRoute();

        if (selectedRoute == null || selectedRoute.waypoints == null || selectedRoute.waypoints.Length == 0)
        {
            Debug.LogWarning("UIManager: 경로가 선택되지 않았습니다. 카드를 먼저 선택해주세요.");
            return;
        }

        ShowScreen(_arMapScreen);
        Debug.Log($"UIManager: AR 화면으로 전환 → {selectedRoute.routeName}");

        try
        {
            // 모든 경유지를 순서대로 방문하는 전체 NavMesh 경로 한번에 계산
            var positions = System.Array.ConvertAll(selectedRoute.waypoints, w => w.localPosition);
            var names     = System.Array.ConvertAll(selectedRoute.waypoints,
                w => !string.IsNullOrEmpty(w.displayName) ? w.displayName : w.instruction);
            arNavigationController?.StartNavigationToAll(positions, names);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UIManager: StartNavigationToAll 예외: {e.Message}");
        }

        SyncTogglePathButton();
    }

    // 전시품 선택 화면 "선택한 전시품 안내 시작" → AR 내비게이션 화면
    private void OnStartUserNavigationClicked()
    {
        NavRoute userRoute = routeSelectUserController?.GetUserRoute();

        if (userRoute == null || userRoute.waypoints == null || userRoute.waypoints.Length == 0)
        {
            Debug.LogWarning("UIManager: 전시품이 선택되지 않았습니다. 1개 이상 선택해주세요.");
            return;
        }

        ShowScreen(_arMapScreen);
        Debug.Log($"UIManager: 사용자 선택 경로로 AR 화면 전환 → {userRoute.routeName}");

        try
        {
            // 전시품 좌표·이름 배열 구성 → 전체 NavMesh 경로를 한번에 계산 후 단일 경로로 안내
            var positions = System.Array.ConvertAll(userRoute.waypoints, w => w.localPosition);
            var names     = System.Array.ConvertAll(userRoute.waypoints,
                w => !string.IsNullOrEmpty(w.displayName) ? w.displayName : w.instruction);
            arNavigationController?.StartNavigationToAll(positions, names);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UIManager: StartNavigationToAll 예외: {e.Message}");
        }

        SyncTogglePathButton();
    }

    // AR 화면 "← 나가기" → 메인 화면 (내비게이션 종료 포함)
    private void OnBackFromARClicked()
    {
        // 진행 중인 내비게이션 종료 및 화살표·유도선 오브젝트 삭제
        arNavigationController?.StopNavigation();
        ShowScreen(_mainScreen);
    }

    // AR 화면 "경로선 보기/숨기기" 토글 버튼
    private void OnTogglePathClicked()
    {
        if (arNavigationController == null) return;
        arNavigationController.TogglePathLine();
        SyncTogglePathButton();
    }

    // 토글 버튼 텍스트·스타일을 현재 IsPathLineVisible 값에 맞게 동기화
    private void SyncTogglePathButton()
    {
        if (_btnTogglePath == null || arNavigationController == null) return;

        bool visible = arNavigationController.IsPathLineVisible;
        _btnTogglePath.text = visible ? "경로선 숨기기" : "경로선 보기";
        if (visible)
            _btnTogglePath.RemoveFromClassList("ar-toggle-path-button--hidden");
        else
            _btnTogglePath.AddToClassList("ar-toggle-path-button--hidden");
    }

    // ════════════════════════════════════════════════════════════════
    //  화면 전환 핵심 메서드
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 지정한 화면만 표시하고 나머지는 모두 숨깁니다.
    /// ARMapScreen 전환 시에는 ARMapScreenController 의 활성 상태도 함께 제어합니다.
    /// </summary>
    private void ShowScreen(VisualElement screenToShow)
    {
        if (screenToShow == null)
        {
            Debug.LogWarning("UIManager: null 화면을 표시하려고 했습니다.");
            return;
        }

        // AR 화면에서 다른 화면으로 이동할 때 컨트롤러 비활성화
        bool wasARScreen = _arMapScreen != null &&
                           !_arMapScreen.ClassListContains("hidden");
        if (wasARScreen && screenToShow != _arMapScreen)
            arMapScreenController?.SetActive(false);

        // 모든 화면 숨김
        _startScreen?          .AddToClassList("hidden");
        _mainScreen?           .AddToClassList("hidden");
        _mapScreen?            .AddToClassList("hidden");
        _routeSelectScreen?    .AddToClassList("hidden");
        _routeSelectUserScreen?.AddToClassList("hidden");
        _arMapScreen?          .AddToClassList("hidden");

        // 선택한 화면만 표시
        screenToShow.RemoveFromClassList("hidden");

        // AR 화면으로 전환 시 컨트롤러 활성화 (센서 폴링 시작)
        if (screenToShow == _arMapScreen)
            arMapScreenController?.SetActive(true);

        Debug.Log($"UIManager: 화면 전환 → {screenToShow.name}");
    }

    // ════════════════════════════════════════════════════════════════
    //  공통 유틸리티
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 지정한 컨테이너 내에서 버튼을 찾고 클릭 이벤트를 연결합니다.
    /// 버튼을 찾지 못하면 경고 로그를 출력합니다.
    /// </summary>
    private void SetupButton(
        VisualElement container,
        string        buttonName,
        ref Button    buttonVar,
        System.Action action)
    {
        buttonVar = container.Q<Button>(buttonName);
        if (buttonVar != null)
        {
            buttonVar.clicked += action;
        }
        else
        {
            Debug.LogWarning($"UIManager: '{buttonName}' 버튼을 '{container.name}' 에서 찾을 수 없습니다.");
        }
    }
}
