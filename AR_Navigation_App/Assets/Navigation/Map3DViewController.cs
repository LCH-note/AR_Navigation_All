/*
    파일명: Assets/Navigation/Map3DViewController.cs
    역할: MapScreen 의 3D 전체도 뷰어 컨트롤러
          Screen Space - Camera UGUI Canvas + RawImage 로 RenderTexture 를 표시하고,
          UI Toolkit PointerEvent 기반 드래그 회전 및 핀치/스크롤 줌을 처리합니다.

    Background.FromRenderTexture(URP+AR 조합에서 표시 불가) → UGUI RawImage 하이브리드로 전환.
    UIToolkit 레이아웃(헤더·탭·map-3d-view 크기 소스·노모델 안내)은 그대로 유지.
    map-3d-area 배경색은 USS 에서 transparent 로 변경해야 UGUI 내용이 보임.

    Inspector 연결 필요:
      · mapCamera      : MapViewCamera (전용 카메라, cullingMask = MapView3D 레이어만)
      · modelRoot      : 3D 모델이 배치될 빈 Transform (MapModelRoot, layer = MapView3D)
      · uiRenderCamera : UGUI Canvas 렌더 카메라 (AR Main Camera 연결, 미연결 시 Camera.main 사용)

    렌더링 순서 (URP + AR Foundation):
      1. AR Main Camera 렌더 (AR 배경 + Screen Space-Camera UGUI ← RawImage 가 여기에)
      2. UIToolkit 렌더 (헤더·탭·noModelLabel, map-3d-area 는 transparent)
      3. SS-Overlay UGUI (없음)
    → SS-Camera UGUI 가 UIToolkit 보다 먼저 렌더링되므로
      map-3d-area 를 투명으로 두면 RawImage 가 그대로 비침.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UI;
using GLTFast;

public class Map3DViewController : MonoBehaviour
{
    // ── Inspector 필드 ──────────────────────────────────────────────
    [Header("RenderTexture 카메라")]
    [Tooltip("3D 지도 전용 카메라 — MapView3D 레이어만 렌더링")]
    [SerializeField] private Camera mapCamera;

    [Tooltip("RenderTexture 해상도 (정사각형)")]
    [SerializeField] private int renderTextureSize = 1024;

    [Header("모델 배치")]
    [Tooltip("다운로드된 3D 모델이 배치될 빈 Transform (MapModelRoot)")]
    [SerializeField] private Transform modelRoot;

    [Header("UGUI Canvas 렌더 카메라")]
    [Tooltip("Screen Space-Camera UGUI Canvas 에 사용할 카메라 (AR Main Camera 연결 권장). " +
             "미연결 시 Camera.main 사용.")]
    [SerializeField] private Camera uiRenderCamera;

    [Header("카메라 조작 설정")]
    [Tooltip("드래그 회전 감도 (픽셀당 도)")]
    [SerializeField] private float rotationSpeed = 0.3f;

    [Tooltip("핀치/스크롤 줌 감도")]
    [SerializeField] private float zoomSpeed = 0.02f;

    [Tooltip("카메라 최소 거리 (m)")]
    [SerializeField] private float minDistance = 2f;

    [Tooltip("카메라 최대 거리 (m)")]
    [SerializeField] private float maxDistance = 40f;

    [Tooltip("카메라 초기 거리 (m)")]
    [SerializeField] private float initialDistance = 15f;

    // ── 내부 상태 ────────────────────────────────────────────────────
    private RenderTexture  _rt;
    private VisualElement  _viewElement;   // map-3d-view — 크기 소스 + 포인터 이벤트 타겟
    private Label          _noModelLabel;
    private GameObject     _loadedModel;

    // UGUI RawImage 하이브리드
    private GameObject     _rawImageGO;
    private Canvas         _rawImageCanvas;
    private RectTransform  _rawImageRect;
    private RawImage       _rawImage;

    // 미등록 안내 텍스트 — RawImage 캔버스 내부 자식 (별도 전체화면 캔버스 제거)
    // 캔버스 자체가 map-3d-area 크기로 동기화되므로 텍스트가 자동으로 해당 영역에만 표시됨
    private GameObject     _noModelOverlayGO;
    private RectTransform  _noModelTextRect;

    // 포인터 ID → 현재 위치 (단일 드래그 + 핀치 줌 공용)
    private readonly Dictionary<int, Vector2> _activePointers = new Dictionary<int, Vector2>();
    private float   _prevPinchDist = -1f;
    private Vector2 _lastDragPos;

    // 카메라 회전 · 거리
    private float _cameraDistance;
    private float _rotX = 30f;
    private float _rotY = 0f;

    public bool IsActive { get; private set; }

    // ════════════════════════════════════════════════════════════════
    //  Unity 생명주기
    // ════════════════════════════════════════════════════════════════

    void Awake()
    {
        _cameraDistance = initialDistance;
        if (mapCamera != null)
            mapCamera.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!IsActive || _rawImageGO == null || !_rawImageGO.activeSelf) return;
        // 캔버스가 활성 상태일 때만 bounds 동기화
        SyncRawImageBounds();
    }

    void OnDestroy()
    {
        if (_viewElement != null)
        {
            _viewElement.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            _viewElement.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            _viewElement.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            _viewElement.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
            _viewElement.UnregisterCallback<WheelEvent>(OnWheel);
        }
        if (_rt != null)               { _rt.Release(); Destroy(_rt); }
        if (_loadedModel != null)      Destroy(_loadedModel);
        if (_rawImageGO != null)       Destroy(_rawImageGO);
        if (_noModelOverlayGO != null) Destroy(_noModelOverlayGO);
    }

    // ════════════════════════════════════════════════════════════════
    //  초기화 (FloorMapController 에서 호출)
    // ════════════════════════════════════════════════════════════════

    public void Initialize(VisualElement viewElement, Label noModelLabel)
    {
        _viewElement  = viewElement;
        _noModelLabel = noModelLabel;

        Debug.Log($"Map3DViewController: Initialize() 호출됨. viewElement={viewElement?.name}, mapCamera={mapCamera?.name}");

        // RenderTexture 생성
        // antiAliasing 미설정(기본값 1) — MSAA RT는 리졸브 없이 UIToolkit/RawImage 에서 샘플링 불가
        // renderTextureSize 가 0 이하이면 1024 로 강제 설정
        int rtSize = renderTextureSize > 0 ? renderTextureSize : 1024;
        _rt = new RenderTexture(rtSize, rtSize, 24, RenderTextureFormat.ARGB32);
        if (!_rt.Create())
        {
            Debug.LogError("Map3DViewController: RenderTexture 생성 실패 — 초기화를 중단합니다.");
            return;
        }
        Debug.Log($"Map3DViewController: RT 생성 완료 ({rtSize}x{rtSize}), IsCreated={_rt.IsCreated()}");

        // MapViewCamera → RT 연결
        if (mapCamera != null)
        {
            mapCamera.targetTexture   = _rt;
            mapCamera.clearFlags      = CameraClearFlags.SolidColor;
            mapCamera.backgroundColor = new Color(0.063f, 0.086f, 0.133f, 1f);
            Debug.Log($"Map3DViewController: mapCamera.targetTexture 할당 완료");
        }
        else
        {
            Debug.LogError("Map3DViewController: mapCamera 가 null — Inspector 연결 확인 필요");
        }

        // UGUI Canvas + RawImage 생성 (map-3d-view 의 worldBound 에 동기화)
        CreateRawImageCanvas();

        // UI Toolkit 포인터 이벤트 등록 — map-3d-view 영역 안에서만 발생
        _viewElement.RegisterCallback<PointerDownEvent>(OnPointerDown);
        _viewElement.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        _viewElement.RegisterCallback<PointerUpEvent>(OnPointerUp);
        _viewElement.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
        _viewElement.RegisterCallback<WheelEvent>(OnWheel);
    }

    // ════════════════════════════════════════════════════════════════
    //  UGUI RawImage 캔버스 생성
    // ════════════════════════════════════════════════════════════════

    private void CreateRawImageCanvas()
    {
        // ── SS-Overlay 캔버스 (전체화면, sortingOrder=1) ──
        // UIToolkit 은 SS-Overlay 보다 먼저 렌더링 → SS-Overlay 가 최상단에 표시됨
        _rawImageGO = new GameObject("Map3D_RawImageCanvas");

        _rawImageCanvas = _rawImageGO.AddComponent<Canvas>();
        _rawImageCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _rawImageCanvas.sortingOrder = 1;

        var scaler = _rawImageGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;

        // ── 내부 컨테이너: map-3d-area bounds 에 동기화 (SyncRawImageBounds 가 이 RectTransform 을 갱신) ──
        // SS-Overlay 캔버스 루트는 Unity 가 전체화면으로 강제하므로,
        // 내부 자식의 anchorMin/Max 로 표시 영역을 제한한다.
        // typeof(RectTransform) 명시: 일반 Transform 은 GetComponent<RectTransform>() 에서 null 반환
        var containerGO = new GameObject("Map3D_Container", typeof(RectTransform));
        containerGO.transform.SetParent(_rawImageGO.transform, false);

        _rawImageRect = containerGO.GetComponent<RectTransform>();
        _rawImageRect.pivot    = new Vector2(0.5f, 0.5f);
        _rawImageRect.anchorMin = Vector2.zero;
        _rawImageRect.anchorMax = Vector2.zero;  // 초기 0-0 → SyncRawImageBounds() 에서 갱신
        _rawImageRect.offsetMin = Vector2.zero;
        _rawImageRect.offsetMax = Vector2.zero;

        // ── RawImage (컨테이너를 꽉 채움) ──
        var rawGO = new GameObject("RawImage");
        rawGO.transform.SetParent(containerGO.transform, false);

        _rawImage = rawGO.AddComponent<RawImage>();
        _rawImage.texture = _rt;

        var rawRect = rawGO.GetComponent<RectTransform>();
        rawRect.anchorMin = Vector2.zero;
        rawRect.anchorMax = Vector2.one;
        rawRect.offsetMin = Vector2.zero;
        rawRect.offsetMax = Vector2.zero;

        // ── 미등록 안내 텍스트 (컨테이너 내부 자식 → map-3d-area 영역에만 표시됨) ──
        _noModelOverlayGO = new GameObject("NoModelText");
        _noModelOverlayGO.transform.SetParent(containerGO.transform, false);

        var text = _noModelOverlayGO.AddComponent<Text>();
        text.text      = "이 층의 3D 전체도가 아직 등록되지 않았습니다.\n웹 대시보드 > 공간관리 > 맵 관리에서\n3D 전체도(.glb)를 업로드해 주세요.";
        text.fontSize  = 28;
        text.color     = new Color(1f, 1f, 1f, 0.65f);
        text.alignment = TextAnchor.MiddleCenter;
        text.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        _noModelTextRect = _noModelOverlayGO.GetComponent<RectTransform>();
        _noModelTextRect.anchorMin = Vector2.zero;
        _noModelTextRect.anchorMax = Vector2.one;
        _noModelTextRect.offsetMin = Vector2.zero;
        _noModelTextRect.offsetMax = Vector2.zero;

        _noModelOverlayGO.SetActive(false);

        // RawImage 캔버스 초기 비활성 — SetActive(true) 시 활성화
        _rawImageGO.SetActive(false);
    }

    // ════════════════════════════════════════════════════════════════
    //  좌표 동기화: map-3d-view worldBound → RawImage RectTransform
    //  PanelSettings.ScaleWithScreenSize 대응을 위해 Screen.width/height 대신
    //  UIToolkit 패널의 논리 해상도(panel.visualTree.resolvedStyle)를 분모로 사용.
    //  - ConstantPixelSize(scale=1)이면 패널 논리 크기 = 화면 픽셀 크기 → 동일 결과.
    //  - ScaleWithScreenSize(예: 1080×1920 기준)에서 실기기 너비가 다를 때도 정확.
    //  - worldBound 는 UIToolkit y축(위→아래), SS-Overlay 앵커는 y=0 이 하단.
    // ════════════════════════════════════════════════════════════════

    private bool _boundsSyncLogged = false;  // 첫 성공 로그를 1회만 출력하기 위한 플래그

    private void SyncRawImageBounds()
    {
        if (_rawImageRect == null || _viewElement == null) return;

        // UIToolkit 패널 논리 크기 취득 (ScaleWithScreenSize 모드에서도 올바른 분모)
        var panel = _viewElement.panel;
        if (panel == null) return;

        var panelRoot = panel.visualTree;
        float pw = panelRoot.resolvedStyle.width;
        float ph = panelRoot.resolvedStyle.height;

        if (float.IsNaN(pw) || float.IsNaN(ph) || pw <= 0f || ph <= 0f) return;

        Rect wb = _viewElement.worldBound;  // UIToolkit 패널 논리 좌표: top-left 원점, y 아래 증가

        // NaN 명시 검사 필수 — 레이아웃 미확정(display:none 등) 시 NaN 반환
        if (float.IsNaN(wb.x)     || float.IsNaN(wb.y) ||
            float.IsNaN(wb.width) || float.IsNaN(wb.height) ||
            wb.width <= 0f        || wb.height <= 0f)
            return;

        // UIToolkit 패널 논리 좌표 → SS-Overlay 앵커 비율(0~1)
        // UGUI y=0 이 화면 하단 → UIToolkit y 축 반전 필요
        _rawImageRect.anchorMin = new Vector2(wb.xMin / pw,  1f - wb.yMax / ph);  // 좌하단
        _rawImageRect.anchorMax = new Vector2(wb.xMax / pw,  1f - wb.yMin / ph);  // 우상단
        _rawImageRect.offsetMin = Vector2.zero;
        _rawImageRect.offsetMax = Vector2.zero;

        // 최초 성공 1회만 로그 출력
        if (!_boundsSyncLogged)
        {
            _boundsSyncLogged = true;
            Debug.Log($"Map3DViewController: Bounds 첫 동기화 성공 — " +
                      $"anchorMin={_rawImageRect.anchorMin}, anchorMax={_rawImageRect.anchorMax}, " +
                      $"panelSize={pw}×{ph}, wb={wb}");
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  활성화 / 비활성화
    // ════════════════════════════════════════════════════════════════

    public void SetActive(bool active)
    {
        IsActive = active;

        if (mapCamera != null)
            mapCamera.gameObject.SetActive(active);

        if (!active)
        {
            if (_rawImageGO != null)       _rawImageGO.SetActive(false);
            if (_noModelOverlayGO != null) _noModelOverlayGO.SetActive(false);
            _activePointers.Clear();
            _prevPinchDist = -1f;
        }
        else
        {
            // 캔버스를 항상 켬 — DataSyncManager URL 여부와 무관하게
            if (_rawImageGO != null)
            {
                _rawImageGO.SetActive(true);
                Debug.Log($"Map3DViewController: SetActive(true) — _rawImageGO 활성화. rt={_rt?.IsCreated()}, tex={_rawImage?.texture?.name}");
            }
            else
            {
                Debug.LogError("Map3DViewController: SetActive(true) — _rawImageGO 가 null! Initialize() 가 호출되지 않았을 수 있습니다.");
            }
            UpdateCameraPosition();
            SyncRawImageBounds();
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  3D 모델 표시 / 미등록 안내
    // ════════════════════════════════════════════════════════════════

    public void ShowModel(string fileUrl)
    {
        // 안내 오버레이 숨기기
        if (_noModelOverlayGO != null) _noModelOverlayGO.SetActive(false);
        _noModelLabel?.AddToClassList("hidden");

        if (string.IsNullOrEmpty(fileUrl))
        {
            // URL 없어도 씬에 기존 모델이 있을 수 있으므로 캔버스는 켜둠
            return;
        }
        StartCoroutine(LoadModelCoroutine(fileUrl));
    }

    // 미등록 안내 표시 — RawImage 캔버스는 유지(MapViewCamera 배경색 표시),
    // sortingOrder=2 UGUI 텍스트로 안내 문구를 RawImage 위에 표시
    public void ShowNoModelMessage()
    {
        _noModelLabel?.RemoveFromClassList("hidden");
        if (_noModelOverlayGO != null) _noModelOverlayGO.SetActive(true);
    }

    // ════════════════════════════════════════════════════════════════
    //  GLB 파일 로드 (glTFast)
    // ════════════════════════════════════════════════════════════════

    private IEnumerator LoadModelCoroutine(string fileUrl)
    {
        if (_loadedModel != null)
        {
            Destroy(_loadedModel);
            _loadedModel = null;
        }

        var gltf     = new GltfImport();
        var loadTask = gltf.Load(fileUrl);
        yield return new WaitUntil(() => loadTask.IsCompleted);

        if (!loadTask.Result)
        {
            Debug.LogError($"Map3DViewController: GLB 로드 실패 — {fileUrl}");
            ShowNoModelMessage();
            yield break;
        }

        _loadedModel = new GameObject("3DMapModel");
        if (modelRoot != null)
            _loadedModel.transform.SetParent(modelRoot, false);

        var instTask = gltf.InstantiateMainSceneAsync(_loadedModel.transform);
        yield return new WaitUntil(() => instTask.IsCompleted);

        // glTFast 자식 오브젝트는 부모 레이어를 자동 상속하지 않으므로 재귀 설정
        if (modelRoot != null)
            SetLayerRecursively(_loadedModel, modelRoot.gameObject.layer);

        Debug.Log($"Map3DViewController: 3D 전체도 로드 완료 ({fileUrl})");
        UpdateCameraPosition();
    }

    // ════════════════════════════════════════════════════════════════
    //  UI Toolkit 포인터 이벤트 핸들러
    //  — map-3d-view VisualElement 내 터치/마우스만 처리
    // ════════════════════════════════════════════════════════════════

    private void OnPointerDown(PointerDownEvent e)
    {
        if (!IsActive) return;
        _viewElement.CapturePointer(e.pointerId);
        _activePointers[e.pointerId] = e.localPosition;
        if (_activePointers.Count == 1)
            _lastDragPos = e.localPosition;
        _prevPinchDist = -1f;
        e.StopPropagation();
    }

    private void OnPointerMove(PointerMoveEvent e)
    {
        if (!IsActive || !_activePointers.ContainsKey(e.pointerId)) return;
        _activePointers[e.pointerId] = e.localPosition;

        if (_activePointers.Count == 1)
        {
            // 단일 터치: 드래그 → Y축 좌우 회전, X축 상하 회전
            Vector2 delta = (Vector2)e.localPosition - _lastDragPos;
            _rotY += delta.x * rotationSpeed;
            _rotX -= delta.y * rotationSpeed;
            _rotX  = Mathf.Clamp(_rotX, -85f, 85f);
            _lastDragPos = e.localPosition;
            UpdateCameraPosition();
        }
        else if (_activePointers.Count >= 2)
        {
            // 멀티 터치: 핀치 → 줌
            var   keys = new List<int>(_activePointers.Keys);
            float dist = Vector2.Distance(_activePointers[keys[0]], _activePointers[keys[1]]);
            if (_prevPinchDist > 0f)
            {
                _cameraDistance += (_prevPinchDist - dist) * zoomSpeed;
                _cameraDistance  = Mathf.Clamp(_cameraDistance, minDistance, maxDistance);
                UpdateCameraPosition();
            }
            _prevPinchDist = dist;
        }
        e.StopPropagation();
    }

    private void OnPointerUp(PointerUpEvent e)
    {
        if (!IsActive) return;
        if (_viewElement.HasPointerCapture(e.pointerId))
            _viewElement.ReleasePointer(e.pointerId);
        _activePointers.Remove(e.pointerId);
        if (_activePointers.Count < 2) _prevPinchDist = -1f;
        e.StopPropagation();
    }

    private void OnPointerCancel(PointerCancelEvent e)
    {
        if (!IsActive) return;
        if (_viewElement.HasPointerCapture(e.pointerId))
            _viewElement.ReleasePointer(e.pointerId);
        _activePointers.Remove(e.pointerId);
        if (_activePointers.Count < 2) _prevPinchDist = -1f;
        e.StopPropagation();
    }

    // 에디터 · 데스크톱: 마우스 휠 줌
    private void OnWheel(WheelEvent e)
    {
        if (!IsActive) return;
        _cameraDistance += e.delta.y * zoomSpeed * 5f;
        _cameraDistance  = Mathf.Clamp(_cameraDistance, minDistance, maxDistance);
        UpdateCameraPosition();
        e.StopPropagation();
    }

    // ════════════════════════════════════════════════════════════════
    //  헬퍼
    // ════════════════════════════════════════════════════════════════

    // GameObject 및 모든 자식에 레이어를 재귀 설정합니다.
    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    // 회전·거리에 따라 카메라 위치·방향을 갱신합니다.
    private void UpdateCameraPosition()
    {
        if (mapCamera == null) return;
        Vector3    pivot = modelRoot != null ? modelRoot.position : Vector3.zero;
        Quaternion rot   = Quaternion.Euler(_rotX, _rotY, 0f);
        mapCamera.transform.position = pivot + rot * new Vector3(0f, 0f, -_cameraDistance);
        mapCamera.transform.LookAt(pivot);
    }
}
