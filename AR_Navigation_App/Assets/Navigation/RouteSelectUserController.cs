/*
    파일명: Assets/Navigation/RouteSelectUserController.cs
    역할: 전시품 직접 선택 화면(RouteSelectScreenUser)의 UI 로직
      - 전시품 카드를 ScrollView에 동적 생성
      - 다중 선택 관리 — 선택 여부만 기록 (순서는 자동 최적화)
      - 선택된 전시품으로 NavRoute 를 생성해 UIManager 에 제공
    흐름:
      1. UIManager 가 화면 전환 전 OnScreenShown() 호출
      2. 사용자가 카드 탭 → 선택/해제 토글
      3. UIManager 가 GetUserRoute() 호출 → 최적 경로 계산 후 ARNavigationController 에 전달
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RouteSelectUserController : MonoBehaviour
{
    // ── Inspector 노출 필드 ──────────────────────────────────────────
    [Header("UI 연결")]
    [Tooltip("씬의 UIDocument 컴포넌트를 연결하세요")]
    [SerializeField] private UIDocument uiDocument;

    // ── 내부 상태 ────────────────────────────────────────────────────
    private Exhibit[]       _exhibits;         // 전체 전시품 목록
    private HashSet<int>    _selectedIndices;  // 선택된 전시품 인덱스 집합 (순서 무관)
    private VisualElement[] _cards;            // 동적 생성 카드 배열

    private ScrollView           _scrollView;
    private Button               _btnStart;
    private Button               _btnReset;
    private Button               _btnSelectAll;
    private Label                _labelCount;

    // ════════════════════════════════════════════════════════════════
    //  공개 메서드 (UIManager 에서 호출)
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// UIManager 가 전시품 선택 화면으로 전환할 때 호출합니다.
    /// </summary>
    public void OnScreenShown()
    {
        if (!TryGetUIReferences()) return;

        // 전시물 데이터 로드 (DataSyncManager 서버 데이터 사용)
        _exhibits = DataSyncManager.LoadedExhibits ?? new Exhibit[0];

        _selectedIndices = new HashSet<int>();
        BuildExhibitCards();
        RefreshStatusBar();
        ApplyStartButtonState(false);
    }

    /// <summary>
    /// 선택된 전시품으로 최적 경로 NavRoute 를 생성해 반환합니다.
    /// 방문 순서는 Nearest Neighbor 알고리즘으로 자동 계산합니다.
    /// 미선택 시 null 반환.
    /// UIManager.OnStartUserNavigationClicked() 에서 호출합니다.
    /// </summary>
    public NavRoute GetUserRoute()
    {
        if (_selectedIndices == null || _selectedIndices.Count == 0) return null;

        // 선택된 전시품 배열 구성 (순서 무관 — CreateUserRoute 내부에서 최적화)
        var selected = new Exhibit[_selectedIndices.Count];
        int idx = 0;
        foreach (int i in _selectedIndices)
            selected[idx++] = _exhibits[i];

        return MockExhibits.CreateUserRoute(selected);
    }

    // ════════════════════════════════════════════════════════════════
    //  UI 참조 획득
    // ════════════════════════════════════════════════════════════════

    private bool TryGetUIReferences()
    {
        if (uiDocument == null)
        {
            Debug.LogError("RouteSelectUserController: UIDocument 가 연결되지 않았습니다.");
            return false;
        }

        var root   = uiDocument.rootVisualElement;
        var screen = root.Q<VisualElement>("RouteSelectUserScreenInstance");

        if (screen == null)
        {
            Debug.LogError("RouteSelectUserController: 'RouteSelectUserScreenInstance' 를 찾을 수 없습니다.");
            return false;
        }

        _scrollView   = screen.Q<ScrollView>("exhibit-scroll");
        _btnStart     = screen.Q<Button>("btn-start-user-navigation");
        _btnReset     = screen.Q<Button>("btn-reset-exhibit");
        _btnSelectAll = screen.Q<Button>("btn-select-all");
        _labelCount   = screen.Q<Label>("label-selected-count");

        if (_btnReset     != null) _btnReset.clicked     += OnResetClicked;
        if (_btnSelectAll != null) _btnSelectAll.clicked += OnSelectAllClicked;

        return _scrollView != null;
    }

    // ════════════════════════════════════════════════════════════════
    //  카드 동적 생성
    // ════════════════════════════════════════════════════════════════

    private void BuildExhibitCards()
    {
        _scrollView.Clear();

        if (_exhibits == null || _exhibits.Length == 0)
        {
            var empty = new Label("등록된 전시품이 없습니다.");
            empty.AddToClassList("placeholder-text");
            _scrollView.Add(empty);
            _cards = new VisualElement[0];
            return;
        }

        _cards = new VisualElement[_exhibits.Length];

        for (int i = 0; i < _exhibits.Length; i++)
        {
            int capturedIndex = i;
            var card = CreateExhibitCard(_exhibits[i]);
            card.AddManipulator(new Clickable(() => OnCardClicked(capturedIndex)));
            _scrollView.Add(card);
            _cards[i] = card;
        }
    }

    /// <summary>
    /// 전시품 1개에 대한 카드 VisualElement 를 생성합니다.
    /// 순번 뱃지 / 전시품 이름·작가·위치 / 체크 마크로 구성됩니다.
    /// </summary>
    private VisualElement CreateExhibitCard(Exhibit exhibit)
    {
        // 카드 루트
        var card = new VisualElement();
        card.AddToClassList("exhibit-card");

        // ── 순번 뱃지 (선택 시 숫자 표시, 미선택 시 빈 원) ──────────
        var badge = new VisualElement();
        badge.AddToClassList("exhibit-card-badge");
        var badgeLabel = new Label("");
        badgeLabel.AddToClassList("exhibit-badge-label");
        badge.Add(badgeLabel);

        // ── 전시품 정보 영역 ──────────────────────────────────────────
        var info = new VisualElement();
        info.AddToClassList("exhibit-card-info");

        var name   = new Label(exhibit.name);
        name.AddToClassList("exhibit-card-name");

        var artist = new Label(exhibit.artist);
        artist.AddToClassList("exhibit-card-artist");

        var hallTag = new VisualElement();
        hallTag.AddToClassList("exhibit-hall-tag");
        var hallLabel = new Label(exhibit.hall);
        hallLabel.AddToClassList("exhibit-hall-label");
        hallTag.Add(hallLabel);

        info.Add(name);
        info.Add(artist);
        info.Add(hallTag);

        // ── 체크 표시 (우측, 선택 시 표시) ──────────────────────────
        var check = new Label("✓");
        check.name = "check-mark";
        check.AddToClassList("exhibit-check-mark");
        check.AddToClassList("exhibit-check-mark--hidden");

        // 조립
        card.Add(badge);
        card.Add(info);
        card.Add(check);

        return card;
    }

    // ════════════════════════════════════════════════════════════════
    //  카드 선택/해제 처리
    // ════════════════════════════════════════════════════════════════

    private void OnCardClicked(int index)
    {
        if (_selectedIndices.Contains(index))
        {
            // 이미 선택된 카드: 선택 해제
            _selectedIndices.Remove(index);
            _cards[index].RemoveFromClassList("exhibit-card--selected");
            UpdateBadge(index, false);
        }
        else
        {
            // 미선택 카드: 선택 추가
            _selectedIndices.Add(index);
            _cards[index].AddToClassList("exhibit-card--selected");
            UpdateBadge(index, true);
        }

        RefreshStatusBar();
        ApplyStartButtonState(_selectedIndices.Count > 0);

        Debug.Log($"RouteSelectUserController: 전시품 선택 변경 [{_exhibits[index].name}] " +
                  $"총 {_selectedIndices.Count}개 선택");
    }

    private void OnResetClicked()
    {
        _selectedIndices.Clear();
        if (_cards == null) return;

        for (int i = 0; i < _cards.Length; i++)
        {
            _cards[i]?.RemoveFromClassList("exhibit-card--selected");
            UpdateBadge(i, false);
        }

        RefreshStatusBar();
        ApplyStartButtonState(false);
        Debug.Log("RouteSelectUserController: 전시품 선택 초기화");
    }

    private void OnSelectAllClicked()
    {
        if (_exhibits == null || _cards == null) return;

        for (int i = 0; i < _exhibits.Length; i++)
        {
            if (!_selectedIndices.Contains(i))
            {
                _selectedIndices.Add(i);
                _cards[i]?.AddToClassList("exhibit-card--selected");
                UpdateBadge(i, true);
            }
        }

        RefreshStatusBar();
        ApplyStartButtonState(_selectedIndices.Count > 0);
        Debug.Log($"RouteSelectUserController: 전체 선택 → {_selectedIndices.Count}개");
    }

    // ════════════════════════════════════════════════════════════════
    //  뱃지·상태바 갱신
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 카드 뱃지 활성화 여부를 설정합니다. 선택 시 활성(체크), 미선택 시 비활성.
    /// </summary>
    private void UpdateBadge(int cardIndex, bool selected)
    {
        if (_cards == null || cardIndex >= _cards.Length || _cards[cardIndex] == null) return;

        var badge     = _cards[cardIndex].Q<VisualElement>(className: "exhibit-card-badge");
        var checkMark = _cards[cardIndex].Q<Label>("check-mark");

        if (selected)
        {
            badge?.AddToClassList("exhibit-card-badge--active");
            checkMark?.RemoveFromClassList("exhibit-check-mark--hidden");
        }
        else
        {
            badge?.RemoveFromClassList("exhibit-card-badge--active");
            checkMark?.AddToClassList("exhibit-check-mark--hidden");
        }
    }

    private void RefreshStatusBar()
    {
        if (_labelCount == null) return;

        int count = _selectedIndices?.Count ?? 0;
        if (count == 0)
            _labelCount.text = "방문할 전시품을 선택하세요 (자동 최적 경로 안내)";
        else
            _labelCount.text = $"{count}개 선택됨  —  최적 경로로 안내합니다";
    }

    private void ApplyStartButtonState(bool enabled)
    {
        if (_btnStart == null) return;

        _btnStart.SetEnabled(true); // 항상 활성 유지, 시각만 전환
        if (enabled)
            _btnStart.RemoveFromClassList("start-nav-button--disabled");
        else
            _btnStart.AddToClassList("start-nav-button--disabled");
    }
}
