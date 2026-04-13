/*
    파일명: Assets/Navigation/ARNavigationController.cs
    역할: AR 공간에 3D 화살표를 배치해 사용자를 경로 안내하는 핵심 컨트롤러
    주요 기능:
      1. [플로팅 화살표] 카메라 앞 고정 거리에 항상 화살표를 표시 — 항상 화면에 보임
         - 매 프레임 카메라 위치를 따라 이동 (카메라에서 floatForwardDistance 앞)
         - Y축만 회전해 다음 웨이포인트 방향을 가리킴
      2. [웨이포인트 추적] 카메라가 웨이포인트 도달 거리 내로 들어오면 다음 지점으로 진행
      3. [Immersal 연동] useImmersalPositioning = true 시 XRSpace 기준 좌표 사용
         (false = 시작 시 카메라 위치/방향 기준 좌표 — 기기 테스트용)
    연동:
      - UIManager 에서 StartNavigation(route), StopNavigation() 호출
*/

using System.Collections.Generic;
using UnityEngine;

public class ARNavigationController : MonoBehaviour
{
    // ── Inspector 노출 필드 ──────────────────────────────────────────
    [Header("참조")]
    [Tooltip("씬의 AR Camera (XR Origin > Main Camera)")]
    [SerializeField] private Camera arCamera;

    [Header("내비게이션 설정")]
    [Tooltip("이 거리(m) 이내로 접근하면 다음 웨이포인트로 이동")]
    [SerializeField] private float waypointReachDistance = 2.0f;

    [Header("플로팅 화살표 설정 (항상 화면에 표시)")]
    [Tooltip("카메라로부터 화살표까지의 전방 거리 (m)")]
    [SerializeField] private float floatForwardDistance = 1.5f;

    [Tooltip("카메라 높이 기준 화살표 세로 오프셋 (m, 음수=아래)")]
    [SerializeField] private float floatHeightOffset = -0.25f;

    [Tooltip("방향 전환 시 화살표 회전 부드러움 (높을수록 빠름)")]
    [SerializeField] private float rotationSpeed = 6f;

    [Tooltip("화살표 전체 스케일")]
    [SerializeField] private float arrowScale = 1.0f;

    [Header("Immersal 설정 (선택)")]
    [Tooltip("true: Immersal XRSpace 기준 좌표 사용 / false: 시작 시 카메라 기준 좌표 사용")]
    [SerializeField] private bool      useImmersalPositioning = false;

    [Tooltip("Immersal XRSpace GameObject (useImmersalPositioning=true 일 때 필요)")]
    [SerializeField] private Transform immersalXRSpace;

    // ── 내부 상태 ────────────────────────────────────────────────────
    private NavRoute _currentRoute;
    private int      _currentWaypointIndex;
    private bool     _isNavigating = false;

    // 내비게이션 시작 시점 원점 (로컬→월드 변환용)
    private Matrix4x4 _localToWorld;

    // 화면에 항상 표시되는 플로팅 방향 화살표
    private GameObject _floatingArrow;

    // 화살표 색상 (딥 블루 계열)
    private static readonly Color ColorArrow = new Color(0.10f, 0.45f, 1.00f);

    // 도착 판정 완료 여부 (중복 호출 방지)
    private bool _arrivedHandled = false;

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

        Debug.Log($"ARNavigationController: 내비게이션 시작 → {route.routeName}");
    }

    /// <summary>
    /// AR 화면 나가기 시 UIManager 가 호출합니다.
    /// 화살표를 삭제하고 내비게이션 상태를 초기화합니다.
    /// </summary>
    public void StopNavigation()
    {
        _isNavigating = false;
        DestroyFloatingArrow();
        _currentRoute = null;
        Debug.Log("ARNavigationController: 내비게이션 종료");
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

        // 웨이포인트 도달 여부 검사
        CheckWaypointReached();
    }

    void OnDestroy()
    {
        DestroyFloatingArrow();
    }

    // ════════════════════════════════════════════════════════════════
    //  내비게이션 원점 계산
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 내비게이션 시작 시점의 카메라(또는 ImmersalXRSpace) 위치/방향을 기준으로
    /// 로컬→월드 변환 행렬을 계산합니다.
    /// </summary>
    private void ComputeNavigationOrigin()
    {
        Vector3    origin;
        Quaternion rotation;

        if (useImmersalPositioning && immersalXRSpace != null)
        {
            origin   = immersalXRSpace.position;
            rotation = immersalXRSpace.rotation;
            Debug.Log("ARNavigationController: Immersal XRSpace 기준 좌표 사용");
        }
        else
        {
            if (arCamera == null)
            {
                Debug.LogWarning("ARNavigationController: arCamera 가 연결되지 않았습니다. " +
                                 "Inspector 에서 XR Origin > Main Camera 를 연결해주세요. " +
                                 "세계 원점(0,0,0) 기준으로 대체합니다.");
                _localToWorld = Matrix4x4.identity;
                return;
            }

            // 카메라 정면을 수평면에 투영해 진행 방향 결정
            Vector3 camPos     = arCamera.transform.position;
            Vector3 camForward = arCamera.transform.forward;
            Vector3 forwardFlat = Vector3.ProjectOnPlane(camForward, Vector3.up).normalized;

            // Y를 0으로 고정해 지면 기준 원점
            origin   = new Vector3(camPos.x, 0f, camPos.z);
            rotation = Quaternion.LookRotation(forwardFlat, Vector3.up);
            Debug.Log("ARNavigationController: 카메라 기준 좌표 사용 (시뮬레이션 모드)");
        }

        _localToWorld = Matrix4x4.TRS(origin, rotation, Vector3.one);
    }

    // ════════════════════════════════════════════════════════════════
    //  플로팅 화살표 생성 / 업데이트 / 삭제
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 카메라 앞 초기 위치에 플로팅 화살표를 생성합니다.
    /// </summary>
    private void CreateFloatingArrow()
    {
        DestroyFloatingArrow();

        // 초기 위치: 카메라 앞 floatForwardDistance, 높이 floatHeightOffset 적용
        Vector3 initPos = CalculateFloatPosition();

        _floatingArrow = CreateArrowObject("NavFloatingArrow", initPos, Vector3.forward, arrowScale, ColorArrow);
        Debug.Log("ARNavigationController: 플로팅 화살표 생성 완료");
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

            Vector3 dirToWp = wpWorld - floatPos;
            dirToWp.y = 0f; // 수평 방향만

            if (dirToWp.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dirToWp.normalized, Vector3.up);
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

        // 카메라 수평 위치 (Y 무시)
        Vector3 userPos  = arCamera.transform.position;
        Vector2 userFlat = new Vector2(userPos.x, userPos.z);

        // 현재 목표 웨이포인트 월드 위치
        Vector3 wpWorld = LocalToWorldPoint(
            _currentRoute.waypoints[_currentWaypointIndex].localPosition);
        Vector2 wpFlat = new Vector2(wpWorld.x, wpWorld.z);

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
        Debug.Log($"ARNavigationController: {_currentRoute.destination} 도착 완료!");
    }

    // ════════════════════════════════════════════════════════════════
    //  보조 메서드
    // ════════════════════════════════════════════════════════════════

    // 로컬 좌표를 월드 좌표로 변환
    private Vector3 LocalToWorldPoint(Vector3 localPos)
    {
        return _localToWorld.MultiplyPoint3x4(localPos);
    }
}
