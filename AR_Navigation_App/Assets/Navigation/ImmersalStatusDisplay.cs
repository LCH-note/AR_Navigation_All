/*
    파일명: Assets/Navigation/ImmersalStatusDisplay.cs
    역할: 실기기에서 Immersal SDK 작동 여부를 단계별로 진단해 화면에 표시
    표시 항목 (체크리스트):
      1. SDK 인스턴스 존재 (씬에 ImmersalSDK 오브젝트 있는지)
      2. SDK 초기화 완료 (IsReady — Platform + Localizer + Maps 모두 구성됐는지)
      3. developerToken 설정 여부 (빈 문자열이면 측위 불가)
      4. 사용자 토큰 검증 완료 (HasValidated — 실기기에서만 동작)
      5. 맵 등록 여부 (MapManager.HasRegisteredMaps)
      6. 측위 시도/성공 횟수 및 TrackingQuality(0~3)
    사용법:
      씬의 아무 GameObject에 컴포넌트 추가 → showStatusOverlay 체크 → 빌드 후 실기기 확인
      (Inspector에 별도 참조 연결 불필요 — ImmersalSDK.Instance 자동 접근)
*/

using Immersal;
using Immersal.XR;
using UnityEngine;

public class ImmersalStatusDisplay : MonoBehaviour
{
    [Header("SDK 진단 오버레이 설정")]
    [Tooltip("true: 화면 우측 상단에 SDK 진단 정보를 표시합니다. 배포 전 false로 끄세요.")]
    [SerializeField] private bool showStatusOverlay = true;

    // ── 수집된 진단 데이터 ──────────────────────────────────────────
    private bool _sdkFound;
    private bool _isReady;
    private bool _hasToken;
    private bool _hasValidated;
    private int  _licenseLevel;   // -1=미검증, 0=Free, 1+=Enterprise
    private bool _hasMaps;
    private int  _mapCount;

    // 트래킹 통계
    private int   _attempts;
    private int   _successes;
    private int   _quality;       // TrackingQuality 0~3
    private float _lastSuccessTime = -1f;
    private int   _prevSuccessCount;

    // OnGUI 배경 텍스처 (매 프레임 재생성 방지)
    private Texture2D _bgTex;

    // ════════════════════════════════════════════════════════════════
    //  매 프레임 진단 데이터 수집
    // ════════════════════════════════════════════════════════════════

    void Update()
    {
        if (!showStatusOverlay) return;

        // 1. SDK 인스턴스 존재 여부
        var sdk = ImmersalSDK.Instance;
        _sdkFound = (sdk != null);

        if (!_sdkFound) return;

        // 2. SDK 초기화 완료
        _isReady = sdk.IsReady;

        // 3. developerToken 설정 여부
        _hasToken = !string.IsNullOrEmpty(sdk.developerToken);

        // 4. 토큰 검증 완료 및 라이센스 레벨
        _hasValidated  = sdk.HasValidated;
        _licenseLevel  = sdk.LicenseLevel;

        // 5. 맵 등록 여부
        _hasMaps  = MapManager.HasRegisteredMaps;
        _mapCount = MapManager.GetRegisteredMaps()?.Count ?? 0;

        // 6. 트래킹 통계
        var status = sdk.TrackingStatus;
        if (status != null)
        {
            _attempts  = status.LocalizationAttemptCount;
            _successes = status.LocalizationSuccessCount;
            _quality   = status.TrackingQuality;

            // 성공 횟수가 늘면 마지막 성공 시각 갱신
            if (_successes > _prevSuccessCount)
            {
                _lastSuccessTime   = Time.time;
                _prevSuccessCount  = _successes;
            }
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  화면 오버레이 렌더링
    // ════════════════════════════════════════════════════════════════

    void OnGUI()
    {
        if (!showStatusOverlay) return;

        if (_bgTex == null)
            _bgTex = MakeBgTex(new Color(0f, 0f, 0f, 0.78f));

        // ── 항목별 체크 문자 및 색상 ────────────────────────────────
        string Chk(bool ok) => ok ? "<color=#44FF55>✓</color>" : "<color=#FF4444>✗</color>";

        // SDK 미발견 시 오류 메시지만 표시
        if (!_sdkFound)
        {
            DrawBox(MakeStyle(Color.red, 26),
                "■ Immersal SDK 진단\n" +
                "<color=#FF4444>✗  ImmersalSDK 인스턴스 없음</color>\n" +
                "   → 씬에 ImmersalSDK 오브젝트가 있는지 확인하세요.");
            return;
        }

        // ── TrackingQuality 바 ──────────────────────────────────────
        string qualityBar = QualityBar(_quality);

        // ── 마지막 측위 성공 경과 시간 ─────────────────────────────
        string lastSuccessStr = _lastSuccessTime >= 0f
            ? $"{Time.time - _lastSuccessTime:F1}초 전"
            : "—";

        // ── 성공률 ─────────────────────────────────────────────────
        string rateStr = _attempts > 0
            ? $"{_successes * 100 / _attempts}%"
            : "—";

        // ── 라이센스 표시 ──────────────────────────────────────────
        string licenseStr = !_hasValidated ? "미검증" :
                            _licenseLevel >= 1 ? "Enterprise" : "Free";

        // ── 최종 출력 텍스트 ───────────────────────────────────────
        string text =
            "■ Immersal SDK 진단\n" +
            "─────────────────────────────\n" +
            $"{Chk(_sdkFound      )} SDK 인스턴스 존재\n" +
            $"{Chk(_isReady       )} SDK 초기화 완료 (IsReady)\n" +
            $"{Chk(_hasToken      )} developerToken 설정됨\n" +
            $"{Chk(_hasValidated  )} 토큰 검증 완료  [{licenseStr}]\n" +
            $"{Chk(_hasMaps       )} 맵 등록됨  ({_mapCount}개)\n" +
            "─────────────────────────────\n" +
            $"측위 시도: {_attempts}회  성공: {_successes}회  ({rateStr})\n" +
            $"트래킹 품질: {qualityBar}  {_quality}/3\n" +
            $"마지막 성공: {lastSuccessStr}";

        DrawBox(MakeStyle(Color.white, 26), text);
    }

    // ── TrackingQuality 시각화 바 (0~3) ────────────────────────────
    private static string QualityBar(int quality)
    {
        string filled   = "<color=#44FF55>█</color>";
        string empty    = "<color=#555555>░</color>";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 3; i++)
            sb.Append(i < quality ? filled : empty);
        return sb.ToString();
    }

    // ── 박스 그리기 ─────────────────────────────────────────────────
    private void DrawBox(GUIStyle style, string text)
    {
        const float W = 500f;
        const float H = 290f;
        float x = Screen.width - W - 20f;   // 우측 상단
        float y = 20f;
        GUI.Box(new Rect(x, y, W, H), text, style);
    }

    // ── GUIStyle 생성 ───────────────────────────────────────────────
    private GUIStyle MakeStyle(Color textColor, int fontSize)
    {
        return new GUIStyle(GUI.skin.box)
        {
            fontSize  = fontSize,
            alignment = TextAnchor.UpperLeft,
            richText  = true,
            normal    = { textColor = textColor, background = _bgTex }
        };
    }

    // ── 단색 배경 텍스처 ────────────────────────────────────────────
    private static Texture2D MakeBgTex(Color color)
    {
        var pix = new Color[] { color, color, color, color };
        var tex = new Texture2D(2, 2);
        tex.SetPixels(pix);
        tex.Apply();
        return tex;
    }
}
