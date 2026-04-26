/*
    파일명: Assets/Navigation/ARNavigationController.cs
    역할: AR 공간에 3D 화살표 및 경로 유도선을 배치해 사용자를 경로 안내하는 핵심 컨트롤러
    주요 기능:
      1. [플로팅 화살표] 카메라 앞 고정 거리에 항상 화살표를 표시 — 항상 화면에 보임
         - 매 프레임 카메라 위치를 따라 이동 (카메라에서 floatForwardDistance 앞)
         - Y축만 회전해 다음 웨이포인트 방향을 가리킴
      2. [경로 유도선] 현재 위치에서 남은 웨이포인트까지 LineRenderer로 바닥 선 표시
         - 매 프레임 첫 번째 포인트를 카메라 위치로 갱신 (지나온 구간 자동 제거)
         - TogglePathLine() 으로 사용자가 표시/숨김 전환 가능
      3. [웨이포인트 추적] 카메라가 웨이포인트 도달 거리 내로 들어오면 다음 지점으로 진행
      4. [Immersal 연동] useImmersalPositioning = true 시 XRSpace 기준 좌표 사용
         - 웨이포인트 localPosition 은 Immersal 맵 로컬 좌표 (XRSpace 기준)
         - 매 프레임 immersalXRSpace.TransformPoint() 로 동적 변환 (측위 갱신 반영)
         - false = 시작 시 카메라 위치/방향 기준 좌표 — 에디터 시뮬레이션용
    연동:
      - UIManager 에서 StartNavigation(route), StopNavigation(), TogglePathLine() 호출
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ARNavigationController : MonoBehaviour
{
    // ── Inspector 노출 필드 ──────────────────────────────────────────
    [Header("참조")]
    [Tooltip("씬의 AR Camera (XR Origin > Main Camera)")]
    [SerializeField] private Camera arCamera;

    [Tooltip("블렌더로 제작한 화살표 Prefab (Assets/Models/ArrowModel.prefab). " +
             "연결 시 Prefab을 사용하고, 미연결 시 런타임 프리미티브로 대체합니다.")]
    [SerializeField] private GameObject arrowPrefab;

    [Header("내비게이션 설정")]
    [Tooltip("이 거리(m) 이내로 접근하면 다음 웨이포인트로 이동")]
    [SerializeField] private float waypointReachDistance = 2.0f;

    [Header("플로팅 화살표 설정 (항상 화면에 표시)")]
    [Tooltip("카메라로부터 화살표까지의 전방 거리 (m)")]
    [SerializeField] private float floatForwardDistance = 1.5f;

    [Tooltip("카메라 높이 기준 화살표 세로 오프셋 (m, 음수=아래)")]
    [SerializeField] private float floatHeightOffset = -0.75f;

    [Tooltip("방향 전환 시 화살표 회전 부드러움 (높을수록 빠름)")]
    [SerializeField] private float rotationSpeed = 6f;

    [Tooltip("화살표 전체 스케일")]
    [SerializeField] private float arrowScale = 1.0f;

    [Tooltip("블렌더 모델의 팁 방향이 Unity +Z(전방)와 다를 때 보정 회전을 입력합니다.\n" +
             "에디터에서 Play 후 화살표 방향을 확인하며 조정하세요.\n" +
             "일반적인 보정값 예시:\n" +
             "  팁이 +Y(위)를 향함  → X: -90\n" +
             "  팁이 -Y(아래)를 향함 → X: +90\n" +
             "  팁이 +X(오른쪽)를 향함 → Y: -90\n" +
             "  팁이 -X(왼쪽)를 향함  → Y: +90\n" +
             "  팁이 -Z(뒤)를 향함   → Y: 180")]
    [SerializeField] private Vector3 arrowRotationOffset = new Vector3(0f, 0f, 0f);

    [Header("경로 유도선 설정")]
    [Tooltip("경로 유도선 굵기 (m)")]
    [SerializeField] private float pathLineWidth = 0.06f;

    [Tooltip("경로 유도선 바닥 높이 오프셋 (m) — 지면보다 약간 위에 표시")]
    [SerializeField] private float pathLineHeightOffset = 0.05f;

    [Tooltip("내비게이션 시작 시 유도선 표시 여부")]
    [SerializeField] private bool pathLineVisibleOnStart = true;

    [Header("Immersal 설정")]
    [Tooltip("true: Immersal XRSpace 기준 맵 좌표 사용 (실기기) / false: 시작 시 카메라 기준 좌표 사용 (에디터 시뮬레이션)")]
    [SerializeField] private bool      useImmersalPositioning = true;

    [Tooltip("씬의 AR Space > Map > (XRSpace) Transform. useImmersalPositioning=true 일 때 필수.")]
    [SerializeField] private Transform immersalXRSpace;

    // ── 내부 상태 ────────────────────────────────────────────────────
    private NavRoute _currentRoute;
    private int      _currentWaypointIndex;
    private bool     _isNavigating = false;

    // 카메라 기준 모드 전용 변환 행렬 (Immersal 비활성 시에만 사용)
    private Matrix4x4 _localToWorld;

    // 화면에 항상 표시되는 플로팅 방향 화살표
    private GameObject _floatingArrow;

    // 경로 유도선 오브젝트
    private GameObject  _pathLineObject;
    private LineRenderer _pathLine;
    private bool         _pathLineVisible;

    // 화살표 색상 (딥 블루 계열)
    private static readonly Color ColorArrow    = new Color(0.10f, 0.45f, 1.00f);
    // 유도선 색상: 딥 블루 → 밝은 파랑 그라디언트
    private static readonly Color ColorLineStart = new Color(0.16f, 0.27f, 0.62f, 0.85f);
    private static readonly Color ColorLineEnd   = new Color(0.39f, 0.55f, 0.90f, 0.85f);

    // 도착 판정 완료 여부 (중복 호출 방지)
    private bool _arrivedHandled = false;

    // 다중 목적지 순서 안내 큐
    private Vector3[] _queuedDestinations;
    private string[]  _queuedDestinationNames;
    private int       _queuedDestIndex;

    // ── 공개 프로퍼티 ────────────────────────────────────────────────
    /// <summary>현재 유도선 표시 상태를 반환합니다.</summary>
    public bool IsPathLineVisible => _pathLineVisible;

    // ════════════════════════════════════════════════════════════════
    //  공개 메서드 (UIManager 에서 호출)
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 경로 선택 후 AR 화면 진입 시 UIManager 가 호출합니다.
    /// 웨이포인트 인덱스를 초기화하고 플로팅 화살표를 생성합니다.
    /// </summary>
    public void StartNavigation(NavRoute route)
    {
        if (route == null || route.waypoints == null || route.waypoints.Length == 0)
        {
            Debug.LogWarning("ARNavigationController: 유효하지 않은 경로입니다.");
            return;
        }

        _currentRoute         = route;
        _currentWaypointIndex = 0;
        _isNavigating         = true;
        _arrivedHandled       = false;

        // 내비게이션 원점 계산 (로컬→월드 변환 행렬 확정)
        ComputeNavigationOrigin();

        // 플로팅 화살표 생성 (카메라 앞에 항상 표시)
        CreateFloatingArrow();

        // 경로 유도선 생성
        CreatePathLine();

        Debug.Log($"ARNavigationController: 내비게이션 시작 → {route.routeName}");
    }

    /// <summary>
    /// 단일 목적지로 NavMesh 자동 경로 계산 후 내비게이션을 시작합니다.
    /// destMapLocal: Immersal 맵 로컬 좌표 (XRSpace 기준)
    /// </summary>
    public void StartNavigationTo(Vector3 destMapLocal, string destName = "목적지")
    {
        StartNavigationTo(new[] { destMapLocal }, new[] { destName });
    }

    /// <summary>
    /// 다중 목적지를 순서대로 안내합니다.
    /// 각 목적지 도달 후 자동으로 다음 목적지의 NavMesh 경로를 계산합니다.
    /// </summary>
    public void StartNavigationTo(Vector3[] destinations, string[] names)
    {
        if (destinations == null || destinations.Length == 0) return;

        _queuedDestinations     = destinations;
        _queuedDestinationNames = names ?? new string[destinations.Length];
        _queuedDestIndex        = 0;

        NavigateToQueued();
    }

    // 큐의 현재 인덱스 목적지로 NavMesh 경로를 계산하고 내비게이션을 시작
    private void NavigateToQueued()
    {
        if (_queuedDestinations == null || _queuedDestIndex >= _queuedDestinations.Length)
        {
            Debug.Log("ARNavigationController: 모든 목적지 안내 완료");
            return;
        }

        string  name = (_queuedDestIndex < _queuedDestinationNames.Length)
                       ? _queuedDestinationNames[_queuedDestIndex] : "목적지";
        NavRoute route = ComputeNavMeshRoute(_queuedDestinations[_queuedDestIndex], name);

        if (route != null)
            StartNavigation(route);
    }

    /// <summary>
    /// AR 화면 나가기 시 UIManager 가 호출합니다.
    /// 화살표와 유도선을 삭제하고 내비게이션 상태를 초기화합니다.
    /// </summary>
    public void StopNavigation()
    {
        _isNavigating       = false;
        _queuedDestinations = null; // 다중 목적지 큐 초기화
        DestroyFloatingArrow();
        DestroyPathLine();
        _currentRoute = null;
        Debug.Log("ARNavigationController: 내비게이션 종료");
    }

    /// <summary>
    /// 경로 유도선 표시/숨김을 전환합니다. UIManager 의 토글 버튼에서 호출합니다.
    /// </summary>
    public void TogglePathLine()
    {
        _pathLineVisible = !_pathLineVisible;
        if (_pathLine != null)
            _pathLine.enabled = _pathLineVisible;
        Debug.Log($"ARNavigationController: 유도선 {(_pathLineVisible ? "표시" : "숨김")}");
    }

    // ════════════════════════════════════════════════════════════════
    //  Unity 생명주기
    // ════════════════════════════════════════════════════════════════

    void Update()
    {
        if (!_isNavigating) return;
        if (arCamera == null) return;

        // 플로팅 화살표를 카메라 앞 위치로 이동 + 웨이포인트 방향으로 회전
        UpdateFloatingArrow();

        // 경로 유도선 갱신 (현재 위치 → 남은 웨이포인트)
        UpdatePathLine();

        // 웨이포인트 도달 여부 검사
        CheckWaypointReached();
    }

    void OnDestroy()
    {
        DestroyFloatingArrow();
        DestroyPathLine();
    }

    // ════════════════════════════════════════════════════════════════
    //  내비게이션 원점 계산
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Immersal 비활성 시에만 호출 — 시작 시점 카메라 기준 로컬→월드 행렬을 계산합니다.
    /// Immersal 활성 시에는 매 프레임 immersalXRSpace.TransformPoint() 를 직접 사용합니다.
    /// </summary>
    private void ComputeNavigationOrigin()
    {
        // Immersal 모드는 XRSpace.TransformPoint() 로 동적 변환하므로 여기서 아무것도 하지 않음
        if (useImmersalPositioning)
        {
            if (immersalXRSpace == null)
                Debug.LogError("ARNavigationController: useImmersalPositioning=true 이지만 " +
                               "immersalXRSpace 가 연결되지 않았습니다. Inspector 를 확인하세요.");
            else
                Debug.Log("ARNavigationController: Immersal XRSpace 기준 좌표 사용 (동적 변환)");
            return;
        }

        // ── 에디터 시뮬레이션 모드 ──
        if (arCamera == null)
        {
            Debug.LogWarning("ARNavigationController: arCamera 가 연결되지 않았습니다. " +
                             "세계 원점(0,0,0) 기준으로 대체합니다.");
            _localToWorld = Matrix4x4.identity;
            return;
        }

        Vector3 camPos      = arCamera.transform.position;
        Vector3 camForward  = arCamera.transform.forward;
        Vector3 forwardFlat = Vector3.ProjectOnPlane(camForward, Vector3.up).normalized;

        Vector3    origin   = new Vector3(camPos.x, 0f, camPos.z);
        Quaternion rotation = Quaternion.LookRotation(forwardFlat, Vector3.up);
        _localToWorld = Matrix4x4.TRS(origin, rotation, Vector3.one);
        Debug.Log("ARNavigationController: 카메라 기준 좌표 사용 (에디터 시뮬레이션)");
    }

    // ════════════════════════════════════════════════════════════════
    //  플로팅 화살표 생성 / 업데이트 / 삭제
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 카메라 앞 초기 위치에 플로팅 화살표를 생성합니다.
    /// arrowPrefab 이 연결된 경우 Prefab을 Instantiate 하고,
    /// 미연결 시 런타임 프리미티브(Cube 조합)로 대체합니다.
    /// </summary>
    private void CreateFloatingArrow()
    {
        DestroyFloatingArrow();

        // 초기 위치: 카메라 앞 floatForwardDistance, 높이 floatHeightOffset 적용
        Vector3 initPos = CalculateFloatPosition();

        if (arrowPrefab != null)
        {
            // Prefab Instantiate — 머티리얼/셰이더가 에셋에 포함되어 빌드 시 제외되지 않음
            // 초기 회전에도 모델 방향 보정 오프셋 적용
            _floatingArrow = Instantiate(arrowPrefab, initPos, Quaternion.Euler(arrowRotationOffset));
            _floatingArrow.name = "NavFloatingArrow";
            _floatingArrow.transform.localScale = Vector3.one * arrowScale;

            // AR 평면 메시의 깊이 버퍼에 가려지지 않도록 ZTest를 Always로 설정
            // (ARFoundation이 감지한 바닥·벽 평면이 depth를 쓰면 화살표가 사라지는 현상 방지)
            SetZTestAlways(_floatingArrow);

            Debug.Log("ARNavigationController: Prefab 화살표 생성 완료");
        }
        else
        {
            // Fallback: 런타임 프리미티브 조합 (에디터 테스트용)
            _floatingArrow = CreateArrowObject("NavFloatingArrow", initPos, Vector3.forward, arrowScale, ColorArrow);
            Debug.LogWarning("ARNavigationController: arrowPrefab 미연결 — 런타임 프리미티브로 대체합니다. " +
                             "Inspector 에서 Assets/Models/ArrowModel.prefab 을 연결해 주세요.");
        }
    }

    /// <summary>
    /// 매 프레임 화살표를 카메라 앞 위치로 이동하고
    /// 다음 웨이포인트 방향으로 Y축 회전합니다.
    /// </summary>
    private void UpdateFloatingArrow()
    {
        if (_floatingArrow == null) return;

        // ── 위치 업데이트 ──
        // 카메라 수평 전방 벡터 (Y 성분 제거)
        Vector3 camPos     = arCamera.transform.position;
        Vector3 camForward = arCamera.transform.forward;
        Vector3 flatForward = new Vector3(camForward.x, 0f, camForward.z);

        // 카메라가 정면을 거의 수직으로 바라볼 때 (완전히 위/아래) 폴백
        if (flatForward.sqrMagnitude < 0.001f)
            flatForward = new Vector3(camForward.x, 0f, camForward.z) + Vector3.forward * 0.001f;
        flatForward.Normalize();

        Vector3 floatPos = camPos + flatForward * floatForwardDistance;
        floatPos.y = camPos.y + floatHeightOffset;

        _floatingArrow.transform.position = floatPos;

        // ── 방향 업데이트: 다음 웨이포인트를 향한 Y축 회전 ──
        if (_currentWaypointIndex < _currentRoute.waypoints.Length)
        {
            Vector3 wpWorld = LocalToWorldPoint(
                _currentRoute.waypoints[_currentWaypointIndex].localPosition);

            // floatPos(카메라 전방 1.5m)가 아닌 카메라 위치 기준으로 방향 계산
            // → 경로 유도선(camPos 기준)과 동일한 기준점 사용해 방향 일치
            Vector3 dirToWp = wpWorld - camPos;
            dirToWp.y = 0f; // 수평 방향만

            if (dirToWp.sqrMagnitude > 0.01f)
            {
                // LookRotation: 오브젝트의 로컬 +Z축이 웨이포인트 방향을 향하도록 회전
                // arrowRotationOffset: 블렌더 모델의 팁이 +Z가 아닌 경우 보정 (Inspector 조정)
                Quaternion targetRot = Quaternion.LookRotation(dirToWp.normalized, Vector3.up)
                                       * Quaternion.Euler(arrowRotationOffset);
                // 부드러운 회전 보간
                _floatingArrow.transform.rotation = Quaternion.Slerp(
                    _floatingArrow.transform.rotation,
                    targetRot,
                    Time.deltaTime * rotationSpeed);
            }
        }
    }

    private void DestroyFloatingArrow()
    {
        if (_floatingArrow != null)
        {
            Destroy(_floatingArrow);
            _floatingArrow = null;
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  경로 유도선 생성 / 업데이트 / 삭제
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 내비게이션 시작 시 호출됩니다.
    /// LineRenderer 컴포넌트를 가진 오브젝트를 생성하고 초기 포인트를 설정합니다.
    /// </summary>
    private void CreatePathLine()
    {
        DestroyPathLine();

        _pathLineObject = new GameObject("NavPathLine");
        _pathLine       = _pathLineObject.AddComponent<LineRenderer>();

        // 선 굵기: 시작과 끝 동일
        _pathLine.startWidth = pathLineWidth;
        _pathLine.endWidth   = pathLineWidth;

        // 로컬 좌표 사용 안 함 (월드 좌표로 직접 지정)
        _pathLine.useWorldSpace = true;

        // 재질 생성 (URP Unlit, 빌드 시 셰이더 누락 방지 폴백 포함)
        _pathLine.material = CreatePathLineMaterial();

        // 시작→끝 색상 그라디언트
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(ColorLineStart, 0f),
                    new GradientColorKey(ColorLineEnd,   1f) },
            new[] { new GradientAlphaKey(ColorLineStart.a, 0f),
                    new GradientAlphaKey(ColorLineEnd.a,   1f) }
        );
        _pathLine.colorGradient = gradient;

        // 선 끝을 둥글게
        _pathLine.numCornerVertices = 4;
        _pathLine.numCapVertices    = 4;

        // AR 평면 메시에 가려지지 않도록 ZTest = Always 적용
        SetZTestAlways(_pathLineObject);

        // 초기 포인트 세팅
        _pathLineVisible = pathLineVisibleOnStart;
        _pathLine.enabled = _pathLineVisible;
        UpdatePathLine();
    }

    /// <summary>
    /// 매 프레임 호출됩니다.
    /// 유도선 포인트를 [현재 위치] + [남은 웨이포인트]로 갱신합니다.
    /// 지나온 웨이포인트는 자동으로 선에서 제외됩니다.
    /// </summary>
    private void UpdatePathLine()
    {
        if (_pathLine == null || _currentRoute == null) return;
        // 숨겨진 상태여도 포인트는 갱신 — 다시 켰을 때 즉시 올바른 위치 표시

        int remaining = _currentRoute.waypoints.Length - _currentWaypointIndex;
        if (remaining <= 0)
        {
            _pathLine.positionCount = 0;
            return;
        }

        // 포인트 배열: [현재 카메라 위치(바닥)] + [남은 웨이포인트들]
        int count = 1 + remaining;
        _pathLine.positionCount = count;

        // 포인트 0: 카메라 바닥 위치
        Vector3 camPos = arCamera.transform.position;
        _pathLine.SetPosition(0, new Vector3(camPos.x, pathLineHeightOffset, camPos.z));

        // 포인트 1~N: 남은 웨이포인트 월드 좌표
        for (int i = 0; i < remaining; i++)
        {
            Vector3 wp = LocalToWorldPoint(
                _currentRoute.waypoints[_currentWaypointIndex + i].localPosition);
            _pathLine.SetPosition(i + 1, new Vector3(wp.x, pathLineHeightOffset, wp.z));
        }
    }

    private void DestroyPathLine()
    {
        if (_pathLineObject != null)
        {
            Destroy(_pathLineObject);
            _pathLineObject = null;
            _pathLine       = null;
        }
    }

    /// <summary>
    /// URP Unlit 셰이더 기반 유도선 머티리얼을 생성합니다.
    /// </summary>
    private Material CreatePathLineMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");

        var mat = new Material(shader);
        // 색상은 colorGradient 로 제어하므로 흰색 기본값 유지
        mat.color = Color.white;
        return mat;
    }

    /// <summary>
    /// 카메라 앞 floatForwardDistance 위치를 계산합니다.
    /// </summary>
    private Vector3 CalculateFloatPosition()
    {
        if (arCamera == null) return Vector3.forward * floatForwardDistance;

        Vector3 camPos     = arCamera.transform.position;
        Vector3 camForward = arCamera.transform.forward;
        Vector3 flatForward = new Vector3(camForward.x, 0f, camForward.z).normalized;
        if (flatForward.sqrMagnitude < 0.001f) flatForward = Vector3.forward;

        Vector3 pos = camPos + flatForward * floatForwardDistance;
        pos.y = camPos.y + floatHeightOffset;
        return pos;
    }

    // ════════════════════════════════════════════════════════════════
    //  3D 화살표 오브젝트 생성 (Unity 프리미티브 조합)
    // ════════════════════════════════════════════════════════════════

    /*
     * 화살표 구조 (위에서 내려다본 모습):
     *
     *      ↑ Z(진행방향)
     *   \  |  /
     *    \ | /   ← 좌우 날개 Cube (45° 회전)
     *     \|/
     *      |
     *      |     ← 줄기 Cube
     *
     * 줄기: 얇고 긴 Cube, 중심에서 약간 뒤로 배치
     * 좌날개/우날개: 줄기 앞쪽에 45° 각도로 배치
     */
    private GameObject CreateArrowObject(
        string name, Vector3 position, Vector3 direction, float scale, Color color)
    {
        // 부모 오브젝트 (위치·방향 제어)
        var parent = new GameObject(name);
        parent.transform.position = position;
        if (direction != Vector3.zero)
            parent.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        Material mat = CreateArrowMaterial(color);

        // ── 줄기 (Cube) ──────────────────────────────────────────────
        var shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shaft.name = "Shaft";
        shaft.transform.SetParent(parent.transform, false);
        shaft.transform.localPosition = new Vector3(0f, 0f, -0.15f);
        shaft.transform.localScale    = new Vector3(0.13f, 0.10f, 0.48f) * scale;
        shaft.GetComponent<Renderer>().sharedMaterial = mat;
        Destroy(shaft.GetComponent<Collider>());

        // ── 왼쪽 날개 (Cube, +40° Y 회전) ──────────────────────────
        var headL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headL.name = "HeadLeft";
        headL.transform.SetParent(parent.transform, false);
        headL.transform.localPosition = new Vector3(-0.18f, 0f, 0.12f);
        headL.transform.localRotation = Quaternion.Euler(0f, 40f, 0f);
        headL.transform.localScale    = new Vector3(0.13f, 0.10f, 0.40f) * scale;
        headL.GetComponent<Renderer>().sharedMaterial = mat;
        Destroy(headL.GetComponent<Collider>());

        // ── 오른쪽 날개 (Cube, -40° Y 회전) ────────────────────────
        var headR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headR.name = "HeadRight";
        headR.transform.SetParent(parent.transform, false);
        headR.transform.localPosition = new Vector3(0.18f, 0f, 0.12f);
        headR.transform.localRotation = Quaternion.Euler(0f, -40f, 0f);
        headR.transform.localScale    = new Vector3(0.13f, 0.10f, 0.40f) * scale;
        headR.GetComponent<Renderer>().sharedMaterial = mat;
        Destroy(headR.GetComponent<Collider>());

        return parent;
    }

    /// <summary>
    /// URP Unlit 셰이더 기반 화살표 재질을 생성합니다.
    /// AR 환경에서 조명 영향 없이 항상 동일한 색으로 표시됩니다.
    /// </summary>
    private Material CreateArrowMaterial(Color color)
    {
        // URP Unlit 셰이더 우선 사용, 없으면 레거시 Unlit/Color 사용
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Standard");

        var mat = new Material(shader);
        mat.color = color;
        return mat;
    }

    // ════════════════════════════════════════════════════════════════
    //  웨이포인트 추적
    // ════════════════════════════════════════════════════════════════

    private void CheckWaypointReached()
    {
        if (_currentWaypointIndex >= _currentRoute.waypoints.Length) return;

        // 웨이포인트 맵 로컬 좌표
        Vector3 wpLocal = _currentRoute.waypoints[_currentWaypointIndex].localPosition;

        // 카메라를 맵 로컬 좌표로 역변환해 동일 공간에서 비교 (Y 무시, 수평 거리만)
        Vector3 camLocal  = WorldToLocalPoint(arCamera.transform.position);
        Vector2 userFlat  = new Vector2(camLocal.x,  camLocal.z);
        Vector2 wpFlat    = new Vector2(wpLocal.x,   wpLocal.z);

        float dist = Vector2.Distance(userFlat, wpFlat);

        if (dist <= waypointReachDistance)
        {
            _currentWaypointIndex++;

            if (_currentWaypointIndex >= _currentRoute.waypoints.Length)
            {
                OnNavigationComplete();
            }
            else
            {
                Debug.Log($"ARNavigationController: 웨이포인트 도달 " +
                          $"[{_currentWaypointIndex}/{_currentRoute.waypoints.Length}]");
                // 다음 웨이포인트 방향으로 회전은 UpdateFloatingArrow()가 자동 처리
            }
        }
    }

    private void OnNavigationComplete()
    {
        if (_arrivedHandled) return;
        _arrivedHandled = true;

        _isNavigating = false;
        DestroyFloatingArrow();
        DestroyPathLine();
        Debug.Log($"ARNavigationController: {_currentRoute.destination} 도착 완료!");

        // 다중 목적지 큐가 남아있으면 다음 목적지로 자동 전환
        if (_queuedDestinations != null && _queuedDestIndex + 1 < _queuedDestinations.Length)
        {
            _queuedDestIndex++;
            Debug.Log($"ARNavigationController: 다음 목적지로 이동 " +
                      $"[{_queuedDestIndex + 1}/{_queuedDestinations.Length}]");
            NavigateToQueued();
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  보조 메서드
    // ════════════════════════════════════════════════════════════════

    // ════════════════════════════════════════════════════════════════
    //  NavMesh 경로 계산
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 현재 카메라 위치 → destMapLocal 까지의 NavRoute를 자동 생성합니다.
    /// </summary>
    private NavRoute ComputeNavMeshRoute(Vector3 destMapLocal, string destName)
    {
        if (arCamera == null)
        {
            Debug.LogError("ARNavigationController: arCamera 가 연결되지 않았습니다.");
            return null;
        }
        Vector3 camLocal = WorldToLocalPoint(arCamera.transform.position);
        return ComputeNavMeshRoute(new Vector3(camLocal.x, 0f, camLocal.z), destMapLocal, destName);
    }

    /// <summary>
    /// 명시적 시작점 startMapLocal → destMapLocal 까지의 NavRoute를 자동 생성합니다.
    /// 전체 경로 계산(StartNavigationToAll)에서 구간별로 호출됩니다.
    /// 좌표는 모두 맵 로컬 공간(= NavMesh 베이킹 월드 공간)에서 처리합니다.
    /// </summary>
    private NavRoute ComputeNavMeshRoute(Vector3 startMapLocal, Vector3 destMapLocal, string destName)
    {
        Vector3 endMapLocal = new Vector3(destMapLocal.x, 0f, destMapLocal.z);

        // 시작/끝 점을 NavMesh 위로 스냅 (범위 2m 내 탐색)
        if (!NavMesh.SamplePosition(startMapLocal, out NavMeshHit startHit, 2f, NavMesh.AllAreas))
        {
            Debug.LogError($"ARNavigationController: 시작 위치({startMapLocal})를 NavMesh에서 찾을 수 없습니다. " +
                           "NavMesh가 올바르게 베이킹되었는지 확인하세요.");
            return null;
        }
        if (!NavMesh.SamplePosition(endMapLocal, out NavMeshHit endHit, 2f, NavMesh.AllAreas))
        {
            Debug.LogError($"ARNavigationController: 목적지({endMapLocal})를 NavMesh에서 찾을 수 없습니다. " +
                           "목적지가 NavMesh 범위 안에 있는지 확인하세요.");
            return null;
        }

        var navPath = new NavMeshPath();
        bool found = NavMesh.CalculatePath(startHit.position, endHit.position, NavMesh.AllAreas, navPath);

        if (!found || navPath.status == NavMeshPathStatus.PathInvalid)
        {
            Debug.LogError("ARNavigationController: NavMesh 경로 계산 실패. " +
                           "시작점과 목적지가 같은 NavMesh 영역에 있는지 확인하세요.");
            return null;
        }

        // corners[0]=시작, corners[last]=목적지 (맵 로컬 좌표 = NavMesh 베이킹 공간)
        Vector3[] corners = navPath.corners;
        var waypoints = new NavWaypoint[corners.Length];
        for (int i = 0; i < corners.Length; i++)
        {
            waypoints[i] = new NavWaypoint
            {
                localPosition = corners[i],
                displayName   = destName,
                instruction   = (i == corners.Length - 1) ? $"{destName} 도착!" : ""
            };
        }

        int distM = Mathf.RoundToInt(CalcPathLength(corners));
        return new NavRoute
        {
            routeId           = $"auto_{System.DateTime.Now.Ticks}",
            routeName         = $"{destName} 자동 경로",
            destination       = destName,
            description       = "NavMesh 자동 계산 경로",
            estimatedDistance = $"약 {distM}m",
            estimatedTime     = $"약 {Mathf.Max(1, Mathf.RoundToInt(distM / 80f))}분",
            waypoints         = waypoints
        };
    }

    /// <summary>
    /// 선택한 모든 목적지의 NavMesh 경로를 한번에 계산해 단일 경로로 내비게이션을 시작합니다.
    /// 경로 유도선이 현재 위치→목적지1→목적지2→... 전 구간을 처음부터 표시합니다.
    /// UIManager.OnStartUserNavigationClicked() 에서 호출됩니다.
    /// </summary>
    public void StartNavigationToAll(Vector3[] destinations, string[] destNames)
    {
        if (destinations == null || destinations.Length == 0) return;
        if (arCamera == null)
        {
            Debug.LogError("ARNavigationController: arCamera 가 연결되지 않았습니다.");
            return;
        }

        // WorldToLocalPoint 사용 전 좌표계 초기화 (비-Immersal 모드의 _localToWorld 계산)
        ComputeNavigationOrigin();

        var allWaypoints = new List<NavWaypoint>();
        Vector3 camLocal = WorldToLocalPoint(arCamera.transform.position);
        Vector3 segStart = new Vector3(camLocal.x, 0f, camLocal.z);

        for (int i = 0; i < destinations.Length; i++)
        {
            string name = (destNames != null && i < destNames.Length)
                          ? destNames[i] : $"전시품 {i + 1}";
            NavRoute segment = ComputeNavMeshRoute(segStart, destinations[i], name);

            if (segment?.waypoints == null || segment.waypoints.Length == 0)
            {
                Debug.LogWarning($"ARNavigationController: [{name}] 구간 경로 계산 실패, 건너뜁니다.");
                segStart = new Vector3(destinations[i].x, 0f, destinations[i].z);
                continue;
            }

            // 두 번째 구간부터 시작점이 이전 구간 끝점과 동일 → 중복 제거
            int skip = (allWaypoints.Count > 0) ? 1 : 0;
            for (int j = skip; j < segment.waypoints.Length; j++)
                allWaypoints.Add(segment.waypoints[j]);

            segStart = new Vector3(destinations[i].x, 0f, destinations[i].z);
        }

        if (allWaypoints.Count == 0)
        {
            Debug.LogError("ARNavigationController: 모든 구간 경로 계산 실패. NavMesh 범위를 확인하세요.");
            return;
        }

        string finalDest = (destNames != null && destNames.Length > 0)
                           ? destNames[destNames.Length - 1] : "최종 목적지";
        float totalDist  = CalcPathLength(
            System.Array.ConvertAll(allWaypoints.ToArray(), w => w.localPosition));

        var fullRoute = new NavRoute
        {
            routeId           = "route_all_selected",
            routeName         = $"전시품 {destinations.Length}개 전체 경로",
            destination       = finalDest,
            description       = $"선택한 전시품 {destinations.Length}개 경유",
            estimatedDistance = $"약 {Mathf.RoundToInt(totalDist)}m",
            estimatedTime     = $"약 {Mathf.Max(1, Mathf.RoundToInt(totalDist / 80f))}분",
            waypoints         = allWaypoints.ToArray()
        };

        _queuedDestinations = null; // 단계별 큐 사용 안 함 (전체 경로를 단일 NavRoute로 처리)
        StartNavigation(fullRoute);

        Debug.Log($"ARNavigationController: 전체 경로 내비게이션 시작 → " +
                  $"{destinations.Length}개 목적지, 총 {allWaypoints.Count}개 웨이포인트, " +
                  $"약 {Mathf.RoundToInt(totalDist)}m");
    }

    // 경로 총 길이(m) 계산
    private float CalcPathLength(Vector3[] corners)
    {
        float len = 0f;
        for (int i = 1; i < corners.Length; i++)
            len += Vector3.Distance(corners[i - 1], corners[i]);
        return len;
    }

    /// <summary>
    /// 웨이포인트 맵 로컬 좌표를 월드 좌표로 변환합니다.
    /// Immersal 모드: 매 프레임 immersalXRSpace.TransformPoint() 사용 (측위 갱신 반영)
    /// 에디터 모드: 시작 시점에 고정된 _localToWorld 행렬 사용
    /// </summary>
    private Vector3 LocalToWorldPoint(Vector3 mapLocalPos)
    {
        if (useImmersalPositioning && immersalXRSpace != null)
            return immersalXRSpace.TransformPoint(mapLocalPos);

        return _localToWorld.MultiplyPoint3x4(mapLocalPos);
    }

    /// <summary>
    /// 월드 좌표를 맵 로컬 좌표로 역변환합니다 (웨이포인트 도달 거리 계산에 사용).
    /// </summary>
    private Vector3 WorldToLocalPoint(Vector3 worldPos)
    {
        if (useImmersalPositioning && immersalXRSpace != null)
            return immersalXRSpace.InverseTransformPoint(worldPos);

        return _localToWorld.inverse.MultiplyPoint3x4(worldPos);
    }

    /// <summary>
    /// 오브젝트(및 모든 하위 자식)의 머티리얼 ZTest를 Always로 설정합니다.
    /// AR 평면 메시가 깊이 버퍼에 기록되어 화살표를 가리는 현상을 방지합니다.
    /// UnityEngine.Rendering.CompareFunction.Always = 8
    /// </summary>
    private void SetZTestAlways(GameObject root)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            // 원본 에셋 머티리얼을 건드리지 않기 위해 인스턴스 머티리얼 사용
            foreach (var mat in renderer.materials)
            {
                // ZTest Always(8): 깊이 비교 없이 항상 렌더링
                mat.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            }
        }
    }
}
