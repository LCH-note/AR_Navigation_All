/*
    파일명: ARMapScreenController.cs
    역할: AR 내비게이션 화면(ARMapScreen)의 로직을 담당하는 MonoBehaviour
    주요 기능:
      1. [기기 방향 감지] 가속도계(Input.acceleration)를 이용해
         기기가 세워졌는지(세로) / 눕혀졌는지(수평) 판단
         → 수평이면 2D 실내 지도 패널 표시, 세로면 힌트 문구 표시
      2. [나침반] Input.compass.trueHeading으로 방위(N/NE/E…)를 상단 HUD에 표시
      3. [실내 지도 그리기] UI Toolkit의 Painter2D API를 사용해
         map-canvas VisualElement에 방, 복도, 사용자 위치, 방향 화살표를 직접 그림
      4. [사용자 위치·방향] 현재는 시뮬레이션(고정값),
         실제 구현 시 BLE 비콘 / WiFi RTT / PDR 로 교체 예정
    연동:
      - UIManager 에서 SetActive(true/false) 를 호출해 활성화/비활성화
      - UIDocument 의 ARMapScreenInstance 내부 요소를 직접 쿼리
*/

using UnityEngine;
using UnityEngine.UIElements;

public class ARMapScreenController : MonoBehaviour
{
    // ── Inspector 노출 필드 ──────────────────────────────────────────
    [Header("UI 연결")]
    [Tooltip("AppStructure.uxml 이 연결된 씬의 UIDocument 컴포넌트")]
    [SerializeField] private UIDocument uiDocument;

    // ── 기기 방향 감지 설정 ──────────────────────────────────────────
    // Input.acceleration.z 값:
    //   기기를 세워서 정면을 향할 때 ≈  0
    //   기기를 눕혀서 하늘을 향할 때 ≈ -1
    // TILT_THRESHOLD 보다 작으면 "눕혀진 것"으로 판단해 지도를 표시
    private const float TILT_THRESHOLD = -0.45f;

    // ── 내부 UI 요소 참조 ────────────────────────────────────────────
    private VisualElement _mapOverlay;       // 지도 패널 (표시/숨김 대상)
    private VisualElement _orientationHint;  // 기기 방향 안내 힌트
    private VisualElement _mapCanvas;        // Painter2D 가 그릴 캔버스
    private Label         _labelCompass;     // 나침반 방위 레이블
    private Label         _labelFloor;       // 층 정보 레이블
    private Label         _labelNavInstruction; // 내비게이션 안내
    private Label         _labelNavDistance;    // 남은 거리

    // ── 상태 변수 ────────────────────────────────────────────────────
    private bool _isActive    = false; // 이 화면이 현재 표시 중인지
    private bool _isMapVisible = false; // 지도 패널이 현재 보이는지
    private bool _isBound     = false; // UI 요소 바인딩 완료 여부

    // ── 사용자 위치/방향 (지도 위에 표시할 데이터) ───────────────────
    // 정규화 좌표 (0~1): (0,0) = 지도 좌상단, (1,1) = 우하단
    private Vector2 _userPosition  = new Vector2(0.5f, 0.5f);
    // 나침반 방위각 (0° = 북, 90° = 동, 시계방향 증가)
    private float   _userDirection = 0f;

    // ── 실내 지도 데이터 (정규화 좌표 Rect) ─────────────────────────
    // 실제 서비스에서는 서버/파일에서 지도 데이터를 로드
    // 여기서는 간단한 1층 평면도를 하드코딩

    // 방 4개 (각각 좌상단 x,y 와 너비,높이를 0~1로 표현)
    private readonly Rect[] _rooms = new[]
    {
        new Rect(0.05f, 0.05f, 0.38f, 0.32f), // 방 A: 좌상단
        new Rect(0.57f, 0.05f, 0.38f, 0.32f), // 방 B: 우상단
        new Rect(0.05f, 0.63f, 0.38f, 0.32f), // 방 C: 좌하단
        new Rect(0.57f, 0.63f, 0.38f, 0.32f), // 방 D: 우하단
    };

    // 세로 복도 (중앙)
    private readonly Rect _corridorV = new Rect(0.43f, 0.05f, 0.14f, 0.90f);
    // 가로 복도 (중앙)
    private readonly Rect _corridorH = new Rect(0.05f, 0.37f, 0.90f, 0.26f);

    // ── 색상 정의 ────────────────────────────────────────────────────
    private static readonly Color ColorFloor       = new Color(0.90f, 0.90f, 0.90f); // 바닥 (밝은 회색)
    private static readonly Color ColorCorridor    = new Color(0.97f, 0.97f, 0.97f); // 복도 (더 밝은 회색)
    private static readonly Color ColorRoom        = new Color(0.72f, 0.84f, 0.96f); // 방 (연한 파랑)
    private static readonly Color ColorWall        = new Color(0.18f, 0.18f, 0.18f); // 벽 (진한 회색)
    private static readonly Color ColorUserFill    = new Color(0.13f, 0.59f, 0.95f); // 사용자 위치 원 (파랑)
    private static readonly Color ColorUserStroke  = Color.white;                     // 사용자 위치 원 테두리
    private static readonly Color ColorArrow       = new Color(0.06f, 0.40f, 0.80f); // 방향 화살표 (진한 파랑)

    // ════════════════════════════════════════════════════════════════
    //  Unity 생명주기
    // ════════════════════════════════════════════════════════════════

    void Start()
    {
        // 나침반 센서 활성화 (Android/iOS 모두 필요)
        Input.compass.enabled = true;
        // 위치 서비스 시작 (실내 측위 확장 시 필요)
        // Input.location.Start();
    }

    void Update()
    {
        // 화면이 비활성 상태면 업데이트 건너뜀 (배터리 절약)
        if (!_isActive) return;

        // UI 요소 바인딩이 아직 완료되지 않았으면 재시도
        if (!_isBound) BindUIElements();
        if (!_isBound) return;

        // 1. 기기 방향 감지 → 지도 표시/숨김 결정
        DetectOrientation();

        // 2. 나침반 값으로 방위 레이블 업데이트
        UpdateCompass();

        // 3. 사용자 위치/방향 업데이트
        //    (현재: 시뮬레이션 고정값 / 실제: 측위 시스템 연동)
        UpdateUserPosition();

        // 4. 지도가 보이는 상태면 다시 그리도록 요청
        if (_isMapVisible)
            _mapCanvas?.MarkDirtyRepaint();
    }

    void OnDestroy()
    {
        // 씬 정리 시 콜백 해제 (메모리 누수 방지)
        if (_mapCanvas != null)
            _mapCanvas.generateVisualContent -= DrawIndoorMap;
    }

    // ════════════════════════════════════════════════════════════════
    //  공개 메서드 (UIManager 에서 호출)
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// ARMapScreen 이 표시/숨김될 때 UIManager 가 호출합니다.
    /// true: 화면 활성화 → 센서 폴링, 지도 그리기 시작
    /// false: 화면 비활성화 → 업데이트 중단 (배터리 절약)
    /// </summary>
    public void SetActive(bool active)
    {
        _isActive = active;

        // 화면이 비활성화될 때 지도도 강제로 숨김
        if (!active)
        {
            _isMapVisible = false;
            _mapOverlay?.AddToClassList("hidden");
            _orientationHint?.RemoveFromClassList("hidden");
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  UI 바인딩
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// UIDocument 에서 ARMapScreen 내부 요소를 이름으로 찾아 참조를 저장합니다.
    /// SetActive(true) 직후 첫 Update() 에서 호출되므로
    /// UIDocument 가 완전히 초기화된 뒤 실행됩니다.
    /// </summary>
    private void BindUIElements()
    {
        if (uiDocument == null)
        {
            Debug.LogError("ARMapScreenController: UIDocument 가 연결되지 않았습니다.");
            return;
        }

        var root = uiDocument.rootVisualElement;

        // AppStructure.uxml 에서 ARMapScreen 인스턴스 찾기
        var arScreen = root.Q<VisualElement>("ARMapScreenInstance");
        if (arScreen == null)
        {
            Debug.LogWarning("ARMapScreenController: 'ARMapScreenInstance' 를 찾을 수 없습니다. " +
                             "AppStructure.uxml 에 인스턴스가 추가되었는지 확인하세요.");
            return;
        }

        // 각 UI 요소 참조 획득
        _mapOverlay      = arScreen.Q<VisualElement>("map-overlay");
        _orientationHint = arScreen.Q<VisualElement>("orientation-hint");
        _mapCanvas       = arScreen.Q<VisualElement>("map-canvas");
        _labelCompass    = arScreen.Q<Label>("label-compass");
        _labelFloor      = arScreen.Q<Label>("label-floor");
        _labelNavInstruction = arScreen.Q<Label>("label-nav-instruction");
        _labelNavDistance    = arScreen.Q<Label>("label-nav-distance");

        // 지도 그리기 콜백 등록
        // MarkDirtyRepaint() 가 호출될 때마다 DrawIndoorMap 이 실행됨
        if (_mapCanvas != null)
            _mapCanvas.generateVisualContent += DrawIndoorMap;
        else
            Debug.LogWarning("ARMapScreenController: 'map-canvas' 요소를 찾을 수 없습니다.");

        _isBound = true;
        Debug.Log("ARMapScreenController: UI 바인딩 완료");
    }

    // ════════════════════════════════════════════════════════════════
    //  기기 방향 감지
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 가속도계의 Z축 값으로 기기가 눕혀졌는지 감지합니다.
    /// Input.acceleration 좌표계:
    ///   x = 기기 우측 방향
    ///   y = 기기 위쪽 방향 (세워서 들고 있을 때 ≈ -1)
    ///   z = 기기 화면 바깥 방향 (눕혀서 하늘을 향할 때 ≈ -1)
    /// z < TILT_THRESHOLD(-0.45) 이면 지도 표시
    /// </summary>
    private void DetectOrientation()
    {
        bool shouldShowMap = Input.acceleration.z < TILT_THRESHOLD;

        // 상태가 변경된 경우에만 UI 업데이트 (매 프레임 클래스 추가/제거 방지)
        if (shouldShowMap == _isMapVisible) return;

        _isMapVisible = shouldShowMap;

        if (shouldShowMap)
        {
            // 기기가 눕혀짐 → 지도 표시, 힌트 숨김
            _mapOverlay?.RemoveFromClassList("hidden");
            _orientationHint?.AddToClassList("hidden");
            Debug.Log("ARMapScreenController: 지도 표시 (수평 감지)");
        }
        else
        {
            // 기기가 세워짐 → 지도 숨김, 힌트 표시
            _mapOverlay?.AddToClassList("hidden");
            _orientationHint?.RemoveFromClassList("hidden");
            Debug.Log("ARMapScreenController: 지도 숨김 (수직 감지)");
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  나침반 업데이트
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Input.compass.trueHeading(0~360°)을 8방위 문자열로 변환해
    /// 상단 HUD 레이블에 표시합니다.
    /// </summary>
    private void UpdateCompass()
    {
        if (_labelCompass == null) return;

        float heading = Input.compass.trueHeading;
        _userDirection = heading; // 지도 화살표 회전에 사용

        // 360° 를 8등분(45° 단위)해 방위 문자로 변환
        // index: 0=N, 1=NE, 2=E, 3=SE, 4=S, 5=SW, 6=W, 7=NW
        string[] directions = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        int index = Mathf.RoundToInt(heading / 45f) % 8;
        _labelCompass.text = directions[index];
    }

    // ════════════════════════════════════════════════════════════════
    //  사용자 위치 업데이트 (시뮬레이션)
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 사용자의 실내 위치를 갱신합니다.
    /// 현재: 복도 중앙에 고정 (데모용)
    /// 실제 구현 시:
    ///   - BLE 비콘 기반 삼각측량
    ///   - WiFi RTT (Round-Trip Time) 측위
    ///   - PDR (Pedestrian Dead Reckoning): 걸음 수 + 나침반
    /// </summary>
    private void UpdateUserPosition()
    {
        // TODO: 실제 측위 시스템 연동
        // 현재는 지도 정중앙(복도 교차점)에 고정
        _userPosition = new Vector2(0.50f, 0.50f);
    }

    // ════════════════════════════════════════════════════════════════
    //  실내 지도 그리기 (Painter2D)
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// map-canvas 에 MarkDirtyRepaint() 가 호출될 때마다 실행되는 콜백입니다.
    /// Unity UI Toolkit 의 Painter2D API 로 실내 지도를 렌더링합니다.
    /// 그리는 순서: 바닥 → 복도 → 방 → 벽 테두리 → 외벽 → 사용자 위치
    /// </summary>
    private void DrawIndoorMap(MeshGenerationContext ctx)
    {
        var painter = ctx.painter2D;

        // 캔버스의 실제 픽셀 크기 (레이아웃 완료 후 확정됨)
        float w = _mapCanvas.contentRect.width;
        float h = _mapCanvas.contentRect.height;

        // 크기가 결정되지 않은 초기 프레임에서는 그리지 않음
        if (w <= 0 || h <= 0) return;

        // 1. 전체 바닥 배경 ─────────────────────────────────────────
        DrawFilledRect(painter, 0, 0, w, h, ColorFloor);

        // 2. 복도 (방보다 밝은 색) ──────────────────────────────────
        painter.fillColor = ColorCorridor;
        DrawNormalizedRect(painter, _corridorV, w, h, fill: true);
        DrawNormalizedRect(painter, _corridorH, w, h, fill: true);

        // 3. 방들 ───────────────────────────────────────────────────
        painter.fillColor = ColorRoom;
        foreach (var room in _rooms)
            DrawNormalizedRect(painter, room, w, h, fill: true);

        // 4. 방 테두리(벽) ─────────────────────────────────────────
        painter.strokeColor = ColorWall;
        painter.lineWidth = 1.5f;
        foreach (var room in _rooms)
            DrawNormalizedRect(painter, room, w, h, fill: false);

        // 5. 외벽 (두꺼운 테두리) ───────────────────────────────────
        painter.strokeColor = ColorWall;
        painter.lineWidth = 3f;
        DrawFilledRectOutline(painter, 2f, 2f, w - 4f, h - 4f);

        // 6. 사용자 위치 표시 ────────────────────────────────────────
        // 정규화 좌표 → 픽셀 좌표 변환
        Vector2 userPx = new Vector2(_userPosition.x * w, _userPosition.y * h);

        // 방향 화살표 (나침반 방위각 기준)
        DrawDirectionArrow(painter, userPx, _userDirection, arrowSize: 14f);

        // 현재 위치 파란 원
        painter.fillColor = ColorUserFill;
        painter.BeginPath();
        painter.Arc(userPx, 9f, 0f, 360f);
        painter.Fill();

        // 위치 원 흰색 테두리 (가독성)
        painter.strokeColor = ColorUserStroke;
        painter.lineWidth = 2.5f;
        painter.BeginPath();
        painter.Arc(userPx, 9f, 0f, 360f);
        painter.Stroke();
    }

    // ─── 보조 그리기 메서드들 ─────────────────────────────────────────

    /// <summary>픽셀 좌표로 채워진 사각형을 그립니다.</summary>
    private void DrawFilledRect(Painter2D p, float x, float y, float w, float h, Color color)
    {
        p.fillColor = color;
        p.BeginPath();
        p.MoveTo(new Vector2(x,     y    ));
        p.LineTo(new Vector2(x + w, y    ));
        p.LineTo(new Vector2(x + w, y + h));
        p.LineTo(new Vector2(x,     y + h));
        p.ClosePath();
        p.Fill();
    }

    /// <summary>픽셀 좌표로 사각형 테두리만 그립니다.</summary>
    private void DrawFilledRectOutline(Painter2D p, float x, float y, float w, float h)
    {
        p.BeginPath();
        p.MoveTo(new Vector2(x,     y    ));
        p.LineTo(new Vector2(x + w, y    ));
        p.LineTo(new Vector2(x + w, y + h));
        p.LineTo(new Vector2(x,     y + h));
        p.ClosePath();
        p.Stroke();
    }

    /// <summary>
    /// 정규화 좌표(0~1)의 Rect 를 캔버스 픽셀 좌표로 변환해 그립니다.
    /// fill=true 이면 채우기, false 이면 테두리만 그립니다.
    /// </summary>
    private void DrawNormalizedRect(Painter2D p, Rect r, float w, float h, bool fill)
    {
        float px = r.x * w;
        float py = r.y * h;
        float pw = r.width  * w;
        float ph = r.height * h;

        p.BeginPath();
        p.MoveTo(new Vector2(px,      py     ));
        p.LineTo(new Vector2(px + pw, py     ));
        p.LineTo(new Vector2(px + pw, py + ph));
        p.LineTo(new Vector2(px,      py + ph));
        p.ClosePath();

        if (fill) p.Fill();
        else      p.Stroke();
    }

    /// <summary>
    /// 사용자 방향을 나타내는 화살표(삼각형)를 그립니다.
    /// compassDeg: 나침반 방위각 (0=북, 90=동, 시계방향)
    /// UI 좌표계(y축 아래가 양수)에 맞게 각도를 보정합니다.
    /// </summary>
    private void DrawDirectionArrow(Painter2D p, Vector2 center, float compassDeg, float arrowSize)
    {
        // 나침반(시계방향, 0=북) → 수학적 각도(반시계방향, 0=우) 변환
        // UI 좌표: y축이 아래로 증가하므로 추가 보정
        // 결과적으로: compassDeg 0° (북) = UI 위쪽 방향
        float rad = (-compassDeg + 90f) * Mathf.Deg2Rad;

        // 화살표 꼭짓점 3개 계산
        Vector2 tip = center + new Vector2(
            Mathf.Cos(rad), -Mathf.Sin(rad)) * arrowSize;           // 앞쪽 꼭짓점
        Vector2 left = center + new Vector2(
            Mathf.Cos(rad + 2.5f), -Mathf.Sin(rad + 2.5f)) * (arrowSize * 0.5f); // 왼쪽
        Vector2 right = center + new Vector2(
            Mathf.Cos(rad - 2.5f), -Mathf.Sin(rad - 2.5f)) * (arrowSize * 0.5f); // 오른쪽

        p.fillColor = ColorArrow;
        p.BeginPath();
        p.MoveTo(tip);
        p.LineTo(left);
        p.LineTo(right);
        p.ClosePath();
        p.Fill();
    }
}
