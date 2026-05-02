/*
    파일명: Assets/Navigation/UserReviewController.cs
    역할: 리뷰 작성 화면 UI 로직
      - 별점 선택 (1~5)
      - 의견 입력 필드 placeholder 처리
      - 리뷰 제출 처리 (현재는 콘솔 출력; 백엔드 연동 시 여기서 API 호출)
*/

using UnityEngine;
using UnityEngine.UIElements;

public class UserReviewController : MonoBehaviour
{
    [Header("UI Document 연결")]
    [SerializeField] private UIDocument uiDocument;

    // 별 라벨 이름 목록
    private static readonly string[] StarNames = { "star-1", "star-2", "star-3", "star-4", "star-5" };

    // 별점 라벨 참조 배열
    private Label[]       _starLabels;
    private Label         _ratingLabel;
    private TextField     _textField;

    private int  _currentRating      = 0;   // 0 = 미선택
    private bool _isPlaceholderActive = true;

    private const string PlaceholderText = "의견을 남겨주세요";

    // ════════════════════════════════════════════════════════════════
    //  외부 호출 진입점
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// UIManager 에서 리뷰 화면 표시 직전에 호출 — 상태 초기화
    /// </summary>
    public void OnScreenShown(VisualElement reviewScreenRoot)
    {
        if (reviewScreenRoot == null) return;

        // 별점 라벨 참조
        _starLabels = new Label[StarNames.Length];
        for (int i = 0; i < StarNames.Length; i++)
        {
            _starLabels[i] = reviewScreenRoot.Q<Label>(StarNames[i]);
            if (_starLabels[i] == null) continue;

            int idx = i + 1; // 클로저 캡처: 별 번호 1~5
            _starLabels[i].RegisterCallback<ClickEvent>(_ => OnStarClicked(idx));
        }

        _ratingLabel = reviewScreenRoot.Q<Label>("review-rating-label");
        _textField   = reviewScreenRoot.Q<TextField>("review-text-field");

        // 입력 필드 초기화 및 placeholder 이벤트 등록
        if (_textField != null)
        {
            _textField.RegisterCallback<FocusInEvent>(_ =>
            {
                if (_isPlaceholderActive)
                {
                    _textField.value = "";
                    _textField.RemoveFromClassList("review-text-field--placeholder");
                    _isPlaceholderActive = false;
                }
            });

            _textField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (string.IsNullOrWhiteSpace(_textField.value))
                    SetPlaceholder();
            });
        }

        // 화면 초기 상태로 리셋
        ResetReview();
    }

    // ════════════════════════════════════════════════════════════════
    //  별점 처리
    // ════════════════════════════════════════════════════════════════

    // i번째 별 클릭 시 1~i까지 채움
    private void OnStarClicked(int rating)
    {
        // 같은 별 재클릭 시 선택 해제
        _currentRating = (_currentRating == rating) ? 0 : rating;
        RefreshStars();
    }

    // 현재 별점에 맞게 ★/☆ 갱신
    private void RefreshStars()
    {
        for (int i = 0; i < _starLabels.Length; i++)
        {
            if (_starLabels[i] == null) continue;
            bool filled = (i + 1) <= _currentRating;
            _starLabels[i].text = filled ? "★" : "☆";
            if (filled)
                _starLabels[i].AddToClassList("star-label--active");
            else
                _starLabels[i].RemoveFromClassList("star-label--active");
        }

        if (_ratingLabel != null)
        {
            _ratingLabel.text = _currentRating > 0
                ? $"{_currentRating}점"
                : "";
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  리뷰 제출 및 상태 관리
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// "리뷰 남기기" 버튼 클릭 시 UIManager 에서 호출
    /// </summary>
    public void OnSubmitReview()
    {
        string reviewText = _isPlaceholderActive ? "" : (_textField?.value ?? "");

        if (_currentRating == 0)
        {
            Debug.LogWarning("UserReviewController: 별점을 선택해주세요.");
            return;
        }

        // TODO: 백엔드 API 연동 시 여기서 POST 요청 전송
        Debug.Log($"[리뷰 제출] 별점: {_currentRating}점 / 의견: \"{reviewText}\"");
    }

    /// <summary>
    /// 화면 진입 시 또는 제출 후 초기 상태로 리셋
    /// </summary>
    public void ResetReview()
    {
        _currentRating = 0;
        RefreshStars();
        SetPlaceholder();
    }

    // placeholder 텍스트 및 스타일 설정
    private void SetPlaceholder()
    {
        if (_textField == null) return;
        _textField.value = PlaceholderText;
        _textField.AddToClassList("review-text-field--placeholder");
        _isPlaceholderActive = true;
    }
}
