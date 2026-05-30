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
      UserReviewScreen      → 리뷰 작성 화면 (별점 + 의견 입력)
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

    [Tooltip("리뷰 작성 화면 UI 로직")]
    [SerializeField] private UserReviewController userReviewController;

    [Tooltip("AR 공간 도슨트 패널 관리 컨트롤러")]
    [SerializeField] private DocentManager docentManager;

    [Tooltip("전체 지도 화면 플로어별 평면도 컨트롤러")]
    [SerializeField] private FloorMapController floorMapController;

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
    private VisualElement _reviewScreen;          // 리뷰 작성 화면

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
    private Button _btnExitApp;              // 메인 화면 "종료" (리뷰 화면으로 이동)
    private Button _btnBackAR;              // AR 화면 "← 나가기"
    private Button _btnTogglePath;           // AR 화면 경로선 토글
    private Button _btnToggleArrow;          // AR 화면 화살표 토글
    private Button _btnToggleDocent;         // AR 화면 도슨트 패널 토글
    private Button _btnSkipReview;           // 리뷰 화면 "건너뛰기" (앱 종료)
    private Button _btnSubmitReview;         // 리뷰 화면 "리뷰 남기기" (앱 종료)

    // 나이대 설문 관련
    private static readonly string[] AgeOptionNames = { "age-10", "age-20", "age-30", "age-40", "age-50", "age-60" };
    private static readonly string[] AgeLabels      = { "10대", "20대", "30대", "40대", "50대", "60대 이상" };
    private VisualElement[] _surveyOptions;      // 6개 옵션 컨테이너
    private VisualElement[] _radioIndicators;    // 6개 라디오 인디케이터 원형
    private Label[]         _surveyLabels;       // 6개 옵션 텍스트
    private int _selectedAgeIndex = -1;          // 선택된 나이대 인덱스 (-1 = 미선택)

    // ════════════════════════════════════════════════════════════════
    //  Unity 생명주기
    // ════════════════════════════════════════════════════════════════

    void Awake()
    {
        // Inspector 에서 연결되지 않은 경우 같은 GameObject 또는 씬에서 자동 탐색
        if (docentManager == null)
            docentManager = GetComponent<DocentManager>();
        if (docentManager == null)
            docentManager = FindObjectOfType<DocentManager>();

        if (floorMapController == null)
            floorMapController = GetComponent<FloorMapController>();
        if (floorMapController == null)
            floorMapController = FindObjectOfType<FloorMapController>();
    }

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
        _reviewScreen          = _root.Q<VisualElement>("UserReviewScreenInstance");

        // 2. 각 화면의 버튼에 이벤트 연결
        if (_startScreen != null)
        {
            SetupButton(_startScreen, "btn-start", ref _btnStart, OnStartClicked);
        }

        if (_mainScreen != null)
        {
            SetupButton(_mainScreen, "btn-exit-app",     ref _btnExitApp,     OnExitAppClicked);
            SetupButton(_mainScreen, "btn-view-map",     ref _btnViewMap,     OnViewMapClicked);
            SetupButton(_mainScreen, "btn-select-route", ref _btnSelectRoute, OnSelectRouteClicked);
        }

        if (_mapScreen != null)
        {
            SetupButton(_mapScreen, "btn-back-map", ref _btnBackMap, OnBackToMainClicked);
            floorMapController?.Initialize(_mapScreen);
        }

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
            SetupButton(_arMapScreen, "btn-back-ar",        ref _btnBackAR,         OnBackFromARClicked);
            SetupButton(_arMapScreen, "btn-toggle-path",    ref _btnTogglePath,     OnTogglePathClicked);
            SetupButton(_arMapScreen, "btn-toggle-arrow",   ref _btnToggleArrow,    OnToggleArrowClicked);
            SetupButton(_arMapScreen, "btn-toggle-docent",  ref _btnToggleDocent,   OnToggleDocentClicked);
        }

        if (_reviewScreen != null)
        {
            SetupButton(_reviewScreen, "btn-skip-review",   ref _btnSkipReview,   OnSkipReviewClicked);
            SetupButton(_reviewScreen, "btn-submit-review", ref _btnSubmitReview, OnSubmitReviewClicked);
            SetupSurvey();
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
        if (_btnExitApp            != null) _btnExitApp.clicked            -= OnExitAppClicked;
        if (_btnBackAR             != null) _btnBackAR.clicked             -= OnBackFromARClicked;
        if (_btnTogglePath         != null) _btnTogglePath.clicked         -= OnTogglePathClicked;
        if (_btnToggleArrow        != null) _btnToggleArrow.clicked        -= OnToggleArrowClicked;
        if (_btnToggleDocent       != null) _btnToggleDocent.clicked       -= OnToggleDocentClicked;
        if (_btnSkipReview         != null) _btnSkipReview.clicked         -= OnSkipReviewClicked;
        if (_btnSubmitReview       != null) _btnSubmitReview.clicked       -= OnSubmitReviewClicked;
    }

    // ════════════════════════════════════════════════════════════════
    //  버튼 이벤트 핸들러
    // ════════════════════════════════════════════════════════════════

    // 시작 화면 "Start" → 메인 화면으로 바로 전환
    private void OnStartClicked()
    {
        ShowScreen(_mainScreen);
    }

    // ══════════════════════════════════════════════
    //  나이대 설문 초기화 및 선택 처리
    // ══════════════════════════════════════════════

    // 리뷰 화면의 설문 옵션 참조를 수집하고 클릭 이벤트를 등록합니다.
    private void SetupSurvey()
    {
        _surveyOptions    = new VisualElement[AgeOptionNames.Length];
        _radioIndicators  = new VisualElement[AgeOptionNames.Length];
        _surveyLabels     = new Label[AgeOptionNames.Length];

        for (int i = 0; i < AgeOptionNames.Length; i++)
        {
            var option = _reviewScreen?.Q<VisualElement>(AgeOptionNames[i]);
            if (option == null) continue;

            _surveyOptions[i]   = option;
            _radioIndicators[i] = option.Q<VisualElement>(className: "radio-indicator");
            _surveyLabels[i]    = option.Q<Label>(className: "survey-option-label");

            int idx = i; // 람다 캡처용 복사
            option.RegisterCallback<ClickEvent>(_ => OnAgeSelected(idx));
        }
    }

    // 특정 나이대 항목 선택 처리 (라디오 버튼 단일 선택)
    private void OnAgeSelected(int index)
    {
        // 이전 선택 해제
        if (_selectedAgeIndex >= 0 && _selectedAgeIndex < _surveyOptions.Length)
        {
            _surveyOptions[_selectedAgeIndex]?.RemoveFromClassList("survey-option--selected");
            _radioIndicators[_selectedAgeIndex]?.RemoveFromClassList("radio-indicator--selected");
            _surveyLabels[_selectedAgeIndex]?.RemoveFromClassList("survey-option-label--selected");
        }

        // 같은 항목 재클릭 시 선택 해제
        if (_selectedAgeIndex == index)
        {
            _selectedAgeIndex = -1;
            return;
        }

        // 새 항목 선택 적용
        _selectedAgeIndex = index;
        _surveyOptions[index]?.AddToClassList("survey-option--selected");
        _radioIndicators[index]?.AddToClassList("radio-indicator--selected");
        _surveyLabels[index]?.AddToClassList("survey-option-label--selected");
    }

    // 리뷰 화면 진입 시 연령대 선택 상태를 초기화합니다.
    private void ResetSurvey()
    {
        if (_selectedAgeIndex >= 0 && _selectedAgeIndex < (_surveyOptions?.Length ?? 0))
        {
            _surveyOptions[_selectedAgeIndex]?.RemoveFromClassList("survey-option--selected");
            _radioIndicators[_selectedAgeIndex]?.RemoveFromClassList("radio-indicator--selected");
            _surveyLabels[_selectedAgeIndex]?.RemoveFromClassList("survey-option-label--selected");
        }
        _selectedAgeIndex = -1;
    }

    // 메인 화면 "View Full Map" → 전체 지도 화면
    private void OnViewMapClicked()
    {
        floorMapController?.OnScreenShown();
        ShowScreen(_mapScreen);
    }

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
            // mapIndex 를 함께 전달해야 AR Space 2 좌표를 올바른 XRSpace 로 변환할 수 있음
            var positions  = System.Array.ConvertAll(selectedRoute.waypoints, w => w.localPosition);
            var names      = System.Array.ConvertAll(selectedRoute.waypoints,
                w => !string.IsNullOrEmpty(w.displayName) ? w.displayName : w.instruction);
            var mapIndices = System.Array.ConvertAll(selectedRoute.waypoints, w => w.mapIndex);
            arNavigationController?.StartNavigationToAll(positions, names, mapIndices);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UIManager: StartNavigationToAll 예외: {e.Message}");
        }

        // 도슨트 패널 초기화 (AR 화면 진입 시 전시품 위치에 패널 생성, 초기 숨김)
        docentManager?.Initialize(DataSyncManager.LoadedExhibits);
        SyncToggleDocentButton();

        SyncTogglePathButton();
        SyncToggleArrowButton();
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
            // mapIndex 를 함께 전달해야 AR Space 2 좌표를 올바른 XRSpace 로 변환할 수 있음
            var positions  = System.Array.ConvertAll(userRoute.waypoints, w => w.localPosition);
            var names      = System.Array.ConvertAll(userRoute.waypoints,
                w => !string.IsNullOrEmpty(w.displayName) ? w.displayName : w.instruction);
            var mapIndices = System.Array.ConvertAll(userRoute.waypoints, w => w.mapIndex);
            arNavigationController?.StartNavigationToAll(positions, names, mapIndices);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UIManager: StartNavigationToAll 예외: {e.Message}");
        }

        // 도슨트 패널 초기화
        docentManager?.Initialize(DataSyncManager.LoadedExhibits);
        SyncToggleDocentButton();

        SyncTogglePathButton();
        SyncToggleArrowButton();
    }

    // 메인 화면 "종료" → 리뷰 화면으로 이동 (연령대 선택 초기화 포함)
    private void OnExitAppClicked()
    {
        ResetSurvey();
        userReviewController?.OnScreenShown(_reviewScreen);
        ShowScreen(_reviewScreen);
    }

    // 리뷰 화면 "건너뛰기" → 앱 종료
    private void OnSkipReviewClicked()
    {
        Debug.Log("UIManager: 리뷰 건너뛰기 → 앱 종료");
        Application.Quit();
    }

    // 리뷰 화면 "리뷰 남기기" → 연령대 포함 방문자 등록 + 리뷰 제출 후 앱 종료
    private void OnSubmitReviewClicked()
    {
        // 연령대가 선택된 경우 방문자 등록 (기기당 1회)
        if (_selectedAgeIndex >= 0 && DataSyncManager.Instance != null)
        {
            string ageGroup = AgeLabels[_selectedAgeIndex];
            Debug.Log($"UIManager: 리뷰 제출 시 연령대 등록 = {ageGroup}");
            StartCoroutine(DataSyncManager.Instance.SubmitVisitorAsync(ageGroup));
        }

        // API 응답을 받은 뒤 앱 종료 (별점 미선택 시 onComplete 가 호출되지 않으므로 화면 유지)
        userReviewController?.OnSubmitReview(onComplete: () =>
        {
            Debug.Log("UIManager: 리뷰 제출 완료 → 앱 종료");
            Application.Quit();
        });
    }

    // AR 화면 "← 나가기" → 메인 화면 (내비게이션 종료)
    private void OnBackFromARClicked()
    {
        // 진행 중인 내비게이션 종료 및 화살표·유도선 오브젝트 삭제
        arNavigationController?.StopNavigation();
        // 도슨트 패널 숨김 (AR 화면 이탈 시 정리)
        docentManager?.HideAll();
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

    // AR 화면 "화살표 보기/숨기기" 토글 버튼
    private void OnToggleArrowClicked()
    {
        if (arNavigationController == null) return;
        arNavigationController.ToggleArrow();
        SyncToggleArrowButton();
    }

    // 토글 버튼 텍스트·스타일을 현재 IsArrowVisible 값에 맞게 동기화
    private void SyncToggleArrowButton()
    {
        if (_btnToggleArrow == null || arNavigationController == null) return;

        bool visible = arNavigationController.IsArrowVisible;
        _btnToggleArrow.text = visible ? "화살표 숨기기" : "화살표 보기";
        if (visible)
            _btnToggleArrow.RemoveFromClassList("ar-toggle-arrow-button--hidden");
        else
            _btnToggleArrow.AddToClassList("ar-toggle-arrow-button--hidden");
    }

    // AR 화면 "도슨트 ON/OFF" 토글 버튼
    private void OnToggleDocentClicked()
    {
        if (docentManager == null) return;
        docentManager.ToggleDocents();
        SyncToggleDocentButton();
    }

    // 도슨트 버튼 텍스트를 현재 IsDocentVisible 값에 맞게 동기화
    private void SyncToggleDocentButton()
    {
        if (_btnToggleDocent == null || docentManager == null) return;
        // 도슨트가 표시 중이면 "도슨트 OFF", 숨김이면 "도슨트 ON"
        _btnToggleDocent.text = docentManager.IsDocentVisible ? "도슨트 OFF" : "도슨트 ON";
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

        // 전체 지도 화면에서 다른 화면으로 이동할 때 3D 뷰어 비활성화
        bool wasMapScreen = _mapScreen != null &&
                            !_mapScreen.ClassListContains("hidden");
        if (wasMapScreen && screenToShow != _mapScreen)
            floorMapController?.OnScreenHidden();

        // 모든 화면 숨김
        _startScreen?          .AddToClassList("hidden");
        _mainScreen?           .AddToClassList("hidden");
        _mapScreen?            .AddToClassList("hidden");
        _routeSelectScreen?    .AddToClassList("hidden");
        _routeSelectUserScreen?.AddToClassList("hidden");
        _arMapScreen?          .AddToClassList("hidden");
        _reviewScreen?         .AddToClassList("hidden");

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
