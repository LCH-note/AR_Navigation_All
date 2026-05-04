/*
    파일명: Assets/Navigation/RouteSelectController.cs
    역할: 경로 선택 화면의 UI 로직
      - 백엔드(또는 Mock) 데이터를 받아 경로 카드를 ScrollView에 동적 생성
      - 카드 선택 상태 관리 → "내비게이션 시작" 버튼 활성/비활성
      - UIManager 에서 GetSelectedRoute() 로 선택된 경로를 가져감
    흐름:
      1. UIManager 가 화면 전환 전 LoadRoutes(routes) 호출
         (현재는 Mock, 추후 백엔드 API 응답으로 교체)
      2. 사용자가 카드 선택 → "내비게이션 시작" 버튼 활성화
      3. UIManager 가 GetSelectedRoute() 호출 → ARNavigationController 로 전달
*/

using UnityEngine;
using UnityEngine.UIElements;

public class RouteSelectController : MonoBehaviour
{
    // ── Inspector 노출 필드 ──────────────────────────────────────────
    [Header("UI 연결")]
    [Tooltip("씬의 UIDocument 컴포넌트를 연결하세요")]
    [SerializeField] private UIDocument uiDocument;

    // ── 내부 상태 ────────────────────────────────────────────────────
    private int          _selectedIndex = -1;   // 현재 선택된 카드 인덱스
    private NavRoute[]   _routes;               // 로드된 경로 데이터
    private VisualElement[] _cards;             // 동적으로 생성된 카드 목록

    private ScrollView   _scrollView;
    private Button       _btnStartNavigation;

    // ════════════════════════════════════════════════════════════════
    //  공개 메서드 (UIManager 에서 호출)
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// UIManager 가 경로 선택 화면으로 전환할 때 호출합니다.
    /// 경로 데이터를 받아 카드를 동적으로 생성합니다.
    /// 현재는 Mock 데이터 사용 — 추후 백엔드 API 응답으로 교체 예정.
    /// </summary>
    public void OnScreenShown()
    {
        // ① UI 요소 참조 획득
        if (!TryGetUIReferences()) return;

        // ② 경로 데이터 로드 (DataSyncManager 로드 완료 시 서버 데이터, 미완료 시 Mock)
        _routes = DataSyncManager.LoadedRoutes ?? MockRoutes.GetAllRoutes();

        // ③ 카드 동적 생성
        BuildRouteCards();

        // ④ 선택 상태 초기화
        ResetSelection();
    }

    /// <summary>
    /// 현재 선택된 NavRoute 반환. 미선택 시 null.
    /// UIManager.OnStartNavigationClicked() 에서 호출합니다.
    /// </summary>
    public NavRoute GetSelectedRoute()
    {
        if (_routes == null || _selectedIndex < 0 || _selectedIndex >= _routes.Length)
            return null;
        return _routes[_selectedIndex];
    }

    // ════════════════════════════════════════════════════════════════
    //  UI 참조 획득
    // ════════════════════════════════════════════════════════════════

    private bool TryGetUIReferences()
    {
        if (uiDocument == null)
        {
            Debug.LogError("RouteSelectController: UIDocument 가 연결되지 않았습니다.");
            return false;
        }

        var root   = uiDocument.rootVisualElement;
        var screen = root.Q<VisualElement>("RouteSelectScreenInstance");

        if (screen == null)
        {
            Debug.LogError("RouteSelectController: 'RouteSelectScreenInstance' 를 찾을 수 없습니다.");
            return false;
        }

        _scrollView         = screen.Q<ScrollView>("route-scroll");
        _btnStartNavigation = screen.Q<Button>("btn-start-navigation");

        if (_scrollView == null)
            Debug.LogWarning("RouteSelectController: 'route-scroll' ScrollView 를 찾을 수 없습니다.");

        return _scrollView != null;
    }

    // ════════════════════════════════════════════════════════════════
    //  카드 동적 생성
    // ════════════════════════════════════════════════════════════════

    private void BuildRouteCards()
    {
        // 이전 카드 전부 제거
        _scrollView.Clear();

        if (_routes == null || _routes.Length == 0)
        {
            // 경로 없음 안내 텍스트
            var empty = new Label("등록된 경로가 없습니다.");
            empty.AddToClassList("placeholder-text");
            _scrollView.Add(empty);
            _cards = new VisualElement[0];
            return;
        }

        _cards = new VisualElement[_routes.Length];

        for (int i = 0; i < _routes.Length; i++)
        {
            int capturedIndex = i; // 클로저 캡처
            var card = CreateCard(_routes[i], i + 1);

            // Clickable manipulator 사용:
            // Button이 내부적으로 사용하는 방식으로, 모바일 터치와 마우스 클릭을
            // 모두 안정적으로 처리. ScrollView 내부에서도 정상 동작.
            card.AddManipulator(new Clickable(() => OnCardClicked(capturedIndex)));

            _scrollView.Add(card);
            _cards[i] = card;
        }
    }

    /// <summary>
    /// 경로 1개에 대한 카드 VisualElement 를 생성합니다.
    /// (UXML 하드코딩 없이 순수 C# 으로 구성)
    /// </summary>
    private VisualElement CreateCard(NavRoute route, int number)
    {
        // 카드 루트
        var card = new VisualElement();
        card.AddToClassList("route-card");

        // ── 번호 뱃지 ─────────────────────────────────────────────
        var badge = new VisualElement();
        badge.AddToClassList("route-card-badge");
        var badgeLabel = new Label(number.ToString());
        badgeLabel.AddToClassList("route-badge-label");
        badge.Add(badgeLabel);

        // ── 경로 정보 영역 ─────────────────────────────────────────
        var info = new VisualElement();
        info.AddToClassList("route-card-info");

        var title = new Label(route.routeName);
        title.AddToClassList("route-card-title");

        var desc = new Label(route.description);
        desc.AddToClassList("route-card-desc");

        // 거리·시간 태그 행
        var meta = new VisualElement();
        meta.AddToClassList("route-card-meta");

        var distTag = CreateMetaTag(route.estimatedDistance);
        var timeTag = CreateMetaTag(route.estimatedTime);

        meta.Add(distTag);
        meta.Add(timeTag);

        info.Add(title);
        info.Add(desc);
        info.Add(meta);

        // ── 우측 화살표 ───────────────────────────────────────────
        var chevron = new Label(">");
        chevron.AddToClassList("route-card-chevron");

        // 조립
        card.Add(badge);
        card.Add(info);
        card.Add(chevron);

        return card;
    }

    private VisualElement CreateMetaTag(string text)
    {
        var tag = new VisualElement();
        tag.AddToClassList("route-meta-tag");
        var label = new Label(text);
        label.AddToClassList("route-meta-text");
        tag.Add(label);
        return tag;
    }

    // ════════════════════════════════════════════════════════════════
    //  카드 선택 처리
    // ════════════════════════════════════════════════════════════════

    private void OnCardClicked(int index)
    {
        // 이전 선택 강조 해제
        if (_selectedIndex >= 0 && _cards != null && _selectedIndex < _cards.Length)
            _cards[_selectedIndex]?.RemoveFromClassList("route-card--selected");

        _selectedIndex = index;

        // 새 선택 강조
        if (_cards != null && index < _cards.Length)
            _cards[index]?.AddToClassList("route-card--selected");

        ApplyStartButtonState(true);
        Debug.Log($"RouteSelectController: 경로 선택 [{index}] {_routes[index].routeName}");
    }

    // ════════════════════════════════════════════════════════════════
    //  선택 초기화 / 버튼 상태
    // ════════════════════════════════════════════════════════════════

    private void ResetSelection()
    {
        if (_selectedIndex >= 0 && _cards != null && _selectedIndex < _cards.Length)
            _cards[_selectedIndex]?.RemoveFromClassList("route-card--selected");

        _selectedIndex = -1;
        ApplyStartButtonState(false);
    }

    private void ApplyStartButtonState(bool enabled)
    {
        if (_btnStartNavigation == null) return;

        // SetEnabled(false)로 비활성화하면 Unity UI Toolkit에서 clicked 이벤트 자체가
        // 발생하지 않아 UIManager의 OnStartNavigationClicked()가 호출되지 않음.
        // 버튼은 항상 활성 상태로 두고, 미선택 시 경고는 UIManager에서 처리.
        _btnStartNavigation.SetEnabled(true);

        // 시각적 스타일만 미선택/선택 상태에 따라 전환
        if (enabled)
            _btnStartNavigation.RemoveFromClassList("start-nav-button--disabled");
        else
            _btnStartNavigation.AddToClassList("start-nav-button--disabled");
    }
}
