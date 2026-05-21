/*
    파일명: Assets/Navigation/Map3DViewController.cs
    역할: MapScreen 의 3D 전체도 뷰어 컨트롤러
          RenderTexture 전용 카메라로 GLB 모델을 렌더링하고,
          터치(드래그 회전, 핀치 줌) 및 에디터 마우스 입력을 처리합니다.

    요구사항:
      - Unity Package Manager 에서 glTFast 패키지 설치 필수:
        + → Add package by name → com.unity.cloud.gltfast
      - Inspector 연결 필요:
        · mapCamera      : MapViewCamera (전용 카메라 오브젝트)
        · modelRoot      : 3D 모델이 배치될 빈 Transform (MapModelRoot)
      - 레이어 설정:
        · MapViewCamera 의 Culling Mask 를 "MapView3D" 레이어만 포함하도록 설정
        · modelRoot 오브젝트를 "MapView3D" 레이어에 배치

    FloorMapController 에서 Initialize() 를 호출해 VisualElement 를 전달합니다.
*/

using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
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

    [Header("카메라 조작 설정")]
    [Tooltip("드래그 회전 감도")]
    [SerializeField] private float rotationSpeed = 0.3f;

    [Tooltip("줌 감도")]
    [SerializeField] private float zoomSpeed = 0.02f;

    [Tooltip("카메라 최소 거리 (m)")]
    [SerializeField] private float minDistance = 2f;

    [Tooltip("카메라 최대 거리 (m)")]
    [SerializeField] private float maxDistance = 40f;

    [Tooltip("카메라 초기 거리 (m)")]
    [SerializeField] private float initialDistance = 15f;

    // ── 내부 상태 ────────────────────────────────────────────────────
    private RenderTexture    _rt;
    private VisualElement    _viewElement;   // map-3d-view VisualElement
    private Label            _noModelLabel;  // label-no-3d-map
    private GameObject       _loadedModel;

    private bool    _isDragging;
    private Vector2 _lastTouchPos;
    private float   _cameraDistance;
    private float   _rotX = 30f;
    private float   _rotY = 0f;

    public bool IsActive { get; private set; }

    // ════════════════════════════════════════════════════════════════
    //  Unity 생명주기
    // ════════════════════════════════════════════════════════════════

    void Awake()
    {
        _cameraDistance = initialDistance;
        // 카메라는 Initialize() 전까지 비활성화
        if (mapCamera != null)
            mapCamera.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!IsActive) return;
        HandleInput();
    }

    void OnDestroy()
    {
        if (_rt != null) { _rt.Release(); Destroy(_rt); }
        if (_loadedModel != null) Destroy(_loadedModel);
    }

    // ════════════════════════════════════════════════════════════════
    //  초기화 (FloorMapController 에서 호출)
    // ════════════════════════════════════════════════════════════════

    // mapScreen 내 map-3d-area 하위의 VisualElement 를 전달받아 초기화합니다.
    public void Initialize(VisualElement viewElement, Label noModelLabel)
    {
        _viewElement  = viewElement;
        _noModelLabel = noModelLabel;

        // RenderTexture 생성
        _rt = new RenderTexture(renderTextureSize, renderTextureSize, 24, RenderTextureFormat.ARGB32);
        _rt.antiAliasing = 2;
        _rt.Create();

        // 카메라 설정
        if (mapCamera != null)
        {
            mapCamera.targetTexture  = _rt;
            mapCamera.clearFlags     = CameraClearFlags.SolidColor;
            mapCamera.backgroundColor = new Color(0.063f, 0.086f, 0.133f, 1f); // 앱 배경색
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
    }

    // ════════════════════════════════════════════════════════════════
    //  3D 모델 표시 / 미등록 안내
    // ════════════════════════════════════════════════════════════════

    // fileUrl 의 GLB 파일을 다운로드해 씬에 로드합니다.
    public void ShowModel(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl))
        {
            ShowNoModelMessage();
            return;
        }

        // 안내 레이블 숨김 & RenderTexture 연결
        _noModelLabel?.AddToClassList("hidden");
        if (_viewElement != null && _rt != null)
            _viewElement.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(_rt));

        StartCoroutine(LoadModelCoroutine(fileUrl));
    }

    // 미등록 안내 문구 표시
    public void ShowNoModelMessage()
    {
        _noModelLabel?.RemoveFromClassList("hidden");
        if (_viewElement != null)
            _viewElement.style.backgroundImage = StyleKeyword.None;
    }

    // ════════════════════════════════════════════════════════════════
    //  GLB 파일 로드 (glTFast)
    // ════════════════════════════════════════════════════════════════

    private IEnumerator LoadModelCoroutine(string fileUrl)
    {
        // 이전 모델 제거
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

        // 모델 인스턴스화
        _loadedModel = new GameObject("3DMapModel");
        if (modelRoot != null)
            _loadedModel.transform.SetParent(modelRoot, false);

        var instTask = gltf.InstantiateMainSceneAsync(_loadedModel.transform);
        yield return new WaitUntil(() => instTask.IsCompleted);

        Debug.Log($"Map3DViewController: 3D 전체도 로드 완료 ({fileUrl})");
        UpdateCameraPosition();
    }

    // ════════════════════════════════════════════════════════════════
    //  터치 / 마우스 입력 처리
    // ════════════════════════════════════════════════════════════════

    private void HandleInput()
    {
#if UNITY_EDITOR
        // 에디터: 마우스 드래그 회전 + 스크롤 줌
        if (Input.GetMouseButtonDown(0)) _isDragging = true;
        if (Input.GetMouseButtonUp(0))   _isDragging = false;
        if (_isDragging)
        {
            _rotY += Input.GetAxis("Mouse X") * rotationSpeed * 120f;
            _rotX -= Input.GetAxis("Mouse Y") * rotationSpeed * 120f;
            _rotX  = Mathf.Clamp(_rotX, -85f, 85f);
            UpdateCameraPosition();
        }
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            _cameraDistance -= scroll * zoomSpeed * 800f;
            _cameraDistance  = Mathf.Clamp(_cameraDistance, minDistance, maxDistance);
            UpdateCameraPosition();
        }
#else
        // 실기기: 단일 터치 드래그 회전
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                _isDragging  = true;
                _lastTouchPos = t.position;
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                _isDragging = false;
            }
            else if (_isDragging && t.phase == TouchPhase.Moved)
            {
                Vector2 delta = t.position - _lastTouchPos;
                _rotY += delta.x * rotationSpeed;
                _rotX -= delta.y * rotationSpeed;
                _rotX  = Mathf.Clamp(_rotX, -85f, 85f);
                _lastTouchPos = t.position;
                UpdateCameraPosition();
            }
        }
        // 실기기: 핀치 줌
        else if (Input.touchCount == 2)
        {
            Touch t0    = Input.GetTouch(0);
            Touch t1    = Input.GetTouch(1);
            Vector2 p0  = t0.position - t0.deltaPosition;
            Vector2 p1  = t1.position - t1.deltaPosition;
            float prev  = (p0 - p1).magnitude;
            float curr  = (t0.position - t1.position).magnitude;
            _cameraDistance += (prev - curr) * zoomSpeed;
            _cameraDistance  = Mathf.Clamp(_cameraDistance, minDistance, maxDistance);
            UpdateCameraPosition();
        }
#endif
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
