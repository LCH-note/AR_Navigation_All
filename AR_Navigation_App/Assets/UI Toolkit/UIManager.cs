/*
    파일명: UIManager.cs
    역할: 앱 내 모든 화면(Screen) 전환을 책임지는 중앙 UI 관리자
    관리 화면:
      StartScreen      → 앱 시작 화면
      MainScreen       → 메인 메뉴 (지도 보기 / 경로 선택)
      MapScreen        → 전체 지도 화면
      RouteSelectScreen→ 경로(목적지) 선택 화면
      ARMapScreen      → AR 내비게이션 실행 화면 (신규)
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
    [Tooltip("경로 선택 화면 UI 로직 (경로 카드 선택 관리)")]
    [SerializeField] private RouteSelectController routeSelectController;

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
    private VisualElement _arMapScreen;       // AR 내비게이션 화면 (신규)

    // 버튼 참조
    private Button _btnStart;
    private Button _btnViewMap;
    private Button _btnSelectRoute;
    private Button _btnBackMap;
    private Button _btnBackRoute;
    private Button _btnStartNavigation; // 경로 선택 화면의 "내비게이션 시작" 버튼
    private Button _btnBackAR;          // AR 화면의 "← 나가기" 버튼

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
        _startScreen       = _root.Q<VisualElement>("StartScreenInstance");
        _mainScreen        = _root.Q<VisualElement>("MainScreenInstance");
        _mapScreen         = _root.Q<VisualElement>("MapScreenInstance");
        _routeSelectScreen = _root.Q<VisualElement>("RouteSelectScreenInstance");
        _arMapScreen       = _root.Q<VisualElement>("ARMapScreenInstance"); // 신규

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
            SetupButton(_routeSelectScreen, "btn-back-route",        ref _btnBackRoute,        OnBackToMainClicked);
            // "내비게이션 시작" 버튼 → AR 내비게이션 화면으로 이동
            SetupButton(_routeSelectScreen, "btn-start-navigation",  ref _btnStartNavigation,  OnStartNavigationClicked);
        }

        if (_arMapScreen != null)
            // AR 화면 "← 나가기" 버튼 → 메인 화면으로 복귀
            SetupButton(_arMapScreen, "btn-back-ar", ref _btnBackAR, OnBackFromARClicked);

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
        if (_btnStartNavigation != null) _btnStartNavigation.clicked -= OnStartNavigationClicked;
        if (_btnBackAR          != null) _btnBackAR.clicked          -= OnBackFromARClicked;
    }

    // ════════════════════════════════════════════════════════════════
    //  버튼 이벤트 핸들러
    // ════════════════════════════════════════════════════════════════

    // 시작 화면 "Start" → 메인 화면
    private void OnStartClicked() => ShowScreen(_mainScreen);

    // 메인 화면 "View Full Map" → 전체 지도 화면
    private void OnViewMapClicked() => ShowScreen(_mapScreen);

    // 메인 화면 "Select Route" → 경로 선택 화면 (RouteSelectController 초기화 포함)
    private void OnSelectRouteClicked()
    {
        Debug.Log("UIManager: OnSelectRouteClicked() 호출됨");

        // 경로 선택 화면이 열릴 때 UI 바인딩 및 선택 상태 초기화
        try
        {
            routeSelectController?.OnScreenShown();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UIManager: RouteSelectController.OnScreenShown() 에서 예외 발생: {e}");
        }

        if (_routeSelectScreen == null)
        {
            Debug.LogError("UIManager: _routeSelectScreen 이 null 입니다! " +
                           "'RouteSelectScreenInstance' 를 찾을 수 없습니다.");
            return;
        }

        ShowScreen(_routeSelectScreen);
    }

    // 지도/경로 선택 화면 "Back" → 메인 화면
    private void OnBackToMainClicked() => ShowScreen(_mainScreen);

    // 경로 선택 화면 "내비게이션 시작" → AR 내비게이션 화면
    private void OnStartNavigationClicked()
    {
        // 선택된 경로 데이터 가져오기
        NavRoute selectedRoute = routeSelectController?.GetSelectedRoute();

        if (selectedRoute == null)
        {
            Debug.LogWarning("UIManager: 경로가 선택되지 않았습니다. 카드를 먼저 선택해주세요.");
            return;
        }

        // 화면 전환을 먼저 실행 (StartNavigation 내부 예외와 무관하게 화면이 전환되도록)
        ShowScreen(_arMapScreen);
        Debug.Log($"UIManager: AR 화면으로 전환 → {selectedRoute.routeName}");

        // 화면 전환 후 내비게이션 시작 (예외가 발생해도 화면 전환에는 영향 없음)
        try
        {
            arNavigationController?.StartNavigation(selectedRoute);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UIManager: StartNavigation 예외 발생 (화살표 배치 실패): {e.Message}");
        }
    }

    // AR 화면 "← 나가기" → 메인 화면 (내비게이션 종료 포함)
    private void OnBackFromARClicked()
    {
        // 진행 중인 내비게이션 종료 및 화살표 오브젝트 삭제
        arNavigationController?.StopNavigation();
        ShowScreen(_mainScreen);
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
        _startScreen?      .AddToClassList("hidden");
        _mainScreen?       .AddToClassList("hidden");
        _mapScreen?        .AddToClassList("hidden");
        _routeSelectScreen?.AddToClassList("hidden");
        _arMapScreen?      .AddToClassList("hidden");

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
