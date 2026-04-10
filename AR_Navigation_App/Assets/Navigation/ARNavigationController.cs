/*
    파일명: Assets/Navigation/ARNavigationController.cs
    역할: AR 공간에 3D 화살표를 배치해 사용자를 경로 안내하는 핵심 컨트롤러
    주요 기능:
      1. [화살표 배치] 선택된 경로의 웨이포인트 위치에 3D 화살표 오브젝트를 배치
         - 현재 목표 화살표: 크고 밝게 (진한 딥 블루)
         - 이후 화살표: 작고 흐리게 (연한 블루)
      2. [웨이포인트 추적] 카메라가 웨이포인트 도달 거리 내로 들어오면 다음 지점으로 진행
      3. [Immersal 연동] useImmersalPositioning = true 시 XRSpace 기준 좌표 사용
         (false = 시작 시 카메라 위치/방향 기준 좌표 — 기기 테스트용)
      4. [HUD 업데이트] ARMapScreen 하단 HUD 의 안내 문구·거리 레이블 실시간 업데이트
    연동:
      - UIManager 에서 StartNavigation(route), StopNavigation() 호출
      - ARMapScreenController 와 동일한 UIDocument 공유
*/

using System.Collections.Generic;
using UnityEngine;

public class ARNavigationController : MonoBehaviour
{
    // ── Inspector 노출 필드 ──────────────────────────────────────────
    [Header("참조")]
    [Tooltip("씬의 AR Camera (XR Origin > Main Camera)")]
    [SerializeField] private Camera arCamera;

    // UIDocument 는 더 이상 사용하지 않음
    // AR 화면은 3D 화살표만으로 방향을 안내하며 텍스트 지시문 없음

    [Header("내비게이션 설정")]
    [Tooltip("이 거리(m) 이내로 접근하면 다음 웨이포인트로 이동")]
    [SerializeField] private float waypointReachDistance = 2.0f;

    [Tooltip("화살표 배치 높이 오프셋 (m, 지면 기준)")]
    [SerializeField] private float arrowHeightOffset = 0.05f;

    [Tooltip("화살표 전체 스케일 배수")]
    [SerializeField] private float arrowScale = 0.6f;

    [Tooltip("한 번에 표시할 최대 화살표 수 (앞쪽 N개)")]
    [SerializeField] private int   maxVisibleArrows = 3;

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

    // 생성된 화살표 오브젝트 목록 (매 웨이포인트 전진마다 재생성)
    private readonly List<GameObject> _spawnedArrows = new List<GameObject>();

    // AR 내비게이션은 3D 화살표만으로 안내 — UI 레이블 없음

    // ── 화살표 색상 (팔레트: 딥 블루 계열) ─────────────────────────
    // 현재 목표 웨이포인트 화살표 색
    private static readonly Color ColorCurrent = new Color(0.10f, 0.45f, 1.00f);
    // 이후 웨이포인트 화살표 색 (어두운 파랑)
    private static readonly Color ColorFuture  = new Color(0.08f, 0.28f, 0.65f);

    // ════════════════════════════════════════════════════════════════
    //  공개 메서드 (UIManager 에서 호출)
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 경로 선택 후 AR 화면 진입 시 UIManager 가 호출합니다.
    /// 웨이포인트 인덱스를 초기화하고 화살표를 AR 공간에 배치합니다.
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

        // 내비게이션 원점 계산 (로컬→월드 변환 행렬 확정)
        ComputeNavigationOrigin();

        // 화살표 배치
        RefreshArrows();

        Debug.Log($"ARNavigationController: 내비게이션 시작 → {route.routeName}");
    }

    /// <summary>
    /// AR 화면 나가기 시 UIManager 가 호출합니다.
    /// 화살표를 모두 삭제하고 내비게이션 상태를 초기화합니다.
    /// </summary>
    public void StopNavigation()
    {
        _isNavigating = false;
        ClearArrows();
        _currentRoute = null;
        Debug.Log("ARNavigationController: 내비게이션 종료");
    }

    // ════════════════════════════════════════════════════════════════
    //  Unity 생명주기
    // ════════════════════════════════════════════════════════════════

    void Update()
    {
        if (!_isNavigating) return;

        // 현재 웨이포인트 도달 여부 검사
        CheckWaypointReached();

        // 현재 화살표 위아래 진동 (눈에 잘 띄도록)
        AnimateCurrentArrow();
    }

    void OnDestroy()
    {
        // 씬 정리 시 화살표 오브젝트도 함께 삭제
        ClearArrows();
    }

    // ════════════════════════════════════════════════════════════════
    //  내비게이션 원점 계산
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 내비게이션 시작 시점의 카메라(또는 ImmersalXRSpace) 위치/방향을 기준으로
    /// 로컬→월드 변환 행렬을 계산합니다.
    /// 이 행렬을 사용해 NavWaypoint.localPosition 을 월드 좌표로 변환합니다.
    /// </summary>
    private void ComputeNavigationOrigin()
    {
        Vector3    origin;
        Quaternion rotation;

        if (useImmersalPositioning && immersalXRSpace != null)
        {
            // Immersal 이 제공하는 좌표 공간을 기준으로 사용
            origin   = immersalXRSpace.position;
            rotation = immersalXRSpace.rotation;
            Debug.Log("ARNavigationController: Immersal XRSpace 기준 좌표 사용");
        }
        else
        {
            // AR 카메라 기준 (수평면 투영)
            // 카메라 높이(Y)는 무시하고 지면(Y=0) 기준으로 원점 설정
            Vector3 camPos     = arCamera.transform.position;
            Vector3 camForward = arCamera.transform.forward;

            // 카메라 정면을 수평면에 투영해 진행 방향 결정
            Vector3 forwardFlat = Vector3.ProjectOnPlane(camForward, Vector3.up).normalized;

            // Y를 0으로 고정해 지면 기준 원점
            origin   = new Vector3(camPos.x, 0f, camPos.z);
            rotation = Quaternion.LookRotation(forwardFlat, Vector3.up);
            Debug.Log("ARNavigationController: 카메라 기준 좌표 사용 (시뮬레이션 모드)");
        }

        // 로컬→월드 변환 행렬 확정
        _localToWorld = Matrix4x4.TRS(origin, rotation, Vector3.one);
    }

    // ════════════════════════════════════════════════════════════════
    //  화살표 배치 / 삭제
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 현재 웨이포인트부터 maxVisibleArrows 개의 화살표를 새로 배치합니다.
    /// 웨이포인트를 하나 전진할 때마다 호출됩니다.
    /// </summary>
    private void RefreshArrows()
    {
        ClearArrows();

        if (_currentRoute == null) return;

        int placed = 0;
        for (int i = _currentWaypointIndex; i < _currentRoute.waypoints.Length; i++)
        {
            // 로컬 좌표 → 월드 좌표 변환
            Vector3 worldPos = LocalToWorldPoint(_currentRoute.waypoints[i].localPosition);
            // 화살표 높이 고정
            worldPos.y = arrowHeightOffset;

            // 이 화살표가 가리킬 방향 계산 (다음 웨이포인트 방향)
            Vector3 lookDir = CalculateLookDirection(i);

            bool    isCurrent = (i == _currentWaypointIndex);
            float   scale     = isCurrent ? arrowScale * 1.3f : arrowScale * 0.75f;
            Color   color     = isCurrent ? ColorCurrent : ColorFuture;

            GameObject arrow = CreateArrowObject($"NavArrow_{i}", worldPos, lookDir, scale, color);
            _spawnedArrows.Add(arrow);

            placed++;
            if (placed >= maxVisibleArrows) break;
        }
    }

    private void ClearArrows()
    {
        foreach (var obj in _spawnedArrows)
        {
            if (obj != null) Destroy(obj);
        }
        _spawnedArrows.Clear();
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

        // 재질 생성 (URP Unlit — AR 환경에서 조명과 무관하게 밝게 표시)
        Material mat = CreateArrowMaterial(color);

        // ── 줄기 (Cube) ──────────────────────────────────────────────
        var shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shaft.name = "Shaft";
        shaft.transform.SetParent(parent.transform, false);
        // 화살표 뒤쪽(-Z)에 배치
        shaft.transform.localPosition = new Vector3(0f, 0.05f, -0.15f);
        shaft.transform.localScale    = new Vector3(0.13f, 0.10f, 0.48f) * scale;
        shaft.GetComponent<Renderer>().sharedMaterial = mat;
        Destroy(shaft.GetComponent<Collider>());

        // ── 왼쪽 날개 (Cube, +40° Y 회전) ──────────────────────────
        var headL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headL.name = "HeadLeft";
        headL.transform.SetParent(parent.transform, false);
        headL.transform.localPosition = new Vector3(-0.18f, 0.05f, 0.12f);
        headL.transform.localRotation = Quaternion.Euler(0f, 40f, 0f);
        headL.transform.localScale    = new Vector3(0.13f, 0.10f, 0.40f) * scale;
        headL.GetComponent<Renderer>().sharedMaterial = mat;
        Destroy(headL.GetComponent<Collider>());

        // ── 오른쪽 날개 (Cube, -40° Y 회전) ────────────────────────
        var headR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headR.name = "HeadRight";
        headR.transform.SetParent(parent.transform, false);
        headR.transform.localPosition = new Vector3(0.18f, 0.05f, 0.12f);
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

        // 카메라 위치 (Y 무시, 수평 거리만 측정)
        Vector3 userPos = arCamera.transform.position;
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
                // 목적지 도착
                OnNavigationComplete();
            }
            else
            {
                Debug.Log($"ARNavigationController: 웨이포인트 도달 " +
                          $"[{_currentWaypointIndex}/{_currentRoute.waypoints.Length}]");
                RefreshArrows();
            }
        }
    }

    private void OnNavigationComplete()
    {
        _isNavigating = false;
        // 모든 화살표 제거 — 화살표가 사라지는 것으로 도착을 표시
        ClearArrows();
        Debug.Log($"ARNavigationController: {_currentRoute.destination} 도착 완료!");
    }

    // ════════════════════════════════════════════════════════════════
    //  현재 화살표 진동 애니메이션
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 현재 목표 웨이포인트 화살표를 위아래로 진동시킵니다.
    /// 사용자가 AR 화면에서 화살표를 쉽게 발견하도록 합니다.
    /// </summary>
    private void AnimateCurrentArrow()
    {
        if (_spawnedArrows.Count == 0 || _spawnedArrows[0] == null) return;

        // 사인파 기반 부드러운 진동 (주기 약 2초)
        float bounce = Mathf.Sin(Time.time * 2.5f) * 0.04f;
        var pos = _spawnedArrows[0].transform.position;
        _spawnedArrows[0].transform.position =
            new Vector3(pos.x, arrowHeightOffset + bounce, pos.z);
    }

    // ════════════════════════════════════════════════════════════════
    //  보조 메서드
    // ════════════════════════════════════════════════════════════════

    // 로컬 좌표를 월드 좌표로 변환
    private Vector3 LocalToWorldPoint(Vector3 localPos)
    {
        return _localToWorld.MultiplyPoint3x4(localPos);
    }

    /// <summary>
    /// i번째 웨이포인트가 가리켜야 할 방향(다음 웨이포인트 방향)을 계산합니다.
    /// 마지막 웨이포인트라면 이전 웨이포인트로부터의 진입 방향을 유지합니다.
    /// </summary>
    private Vector3 CalculateLookDirection(int waypointIndex)
    {
        Vector3 currentWorld = LocalToWorldPoint(_currentRoute.waypoints[waypointIndex].localPosition);

        if (waypointIndex + 1 < _currentRoute.waypoints.Length)
        {
            // 다음 웨이포인트 방향 (수평 평면 투영)
            Vector3 nextWorld = LocalToWorldPoint(_currentRoute.waypoints[waypointIndex + 1].localPosition);
            Vector3 dir = nextWorld - currentWorld;
            dir.y = 0f;
            return dir.normalized != Vector3.zero ? dir.normalized : Vector3.forward;
        }
        else if (waypointIndex > 0)
        {
            // 마지막 지점: 이전→현재 방향 유지
            Vector3 prevWorld = LocalToWorldPoint(_currentRoute.waypoints[waypointIndex - 1].localPosition);
            Vector3 dir = currentWorld - prevWorld;
            dir.y = 0f;
            return dir.normalized != Vector3.zero ? dir.normalized : Vector3.forward;
        }

        return Vector3.forward;
    }
}
