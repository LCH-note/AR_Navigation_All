/*
    파일명: Assets/Navigation/DocentManager.cs
    역할: AR 공간의 각 전시품 위치에 도슨트 패널(World Space Canvas UI)을 생성하고,
          on/off 토글로 전체 가시 여부를 제어합니다.

    사용 흐름:
      1. UIManager가 AR 내비게이션 시작 시 Initialize(LoadedExhibits) 호출
      2. btn-toggle-docent 버튼 클릭 → UIManager → ToggleDocents() 호출
      3. AR 화면 종료 시 UIManager → HideAll() 호출

    패널 구조 (World Space Canvas):
      └ Background (반투명 어두운 배경)
           ├ 전시품 이름 (TMP, 굵게)
           ├ 작가 (TMP, 파란색 — 값이 있을 때만)
           ├ 위치/관 (TMP, 작은 글씨 — 값이 있을 때만)
           ├ 구분선
           └ 도슨트 설명 (TMP — 값이 있을 때만)

    좌표:
      Exhibit.localPosition (Immersal 맵 로컬 좌표) 를
      ARNavigationController.MapLocalToWorld() 로 매 프레임 갱신합니다.
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DocentManager : MonoBehaviour
{
    // ── Inspector 필드 ──────────────────────────────────────────────
    [Header("참조")]
    [Tooltip("AR 카메라 (빌보드 효과에 사용)")]
    [SerializeField] private Camera arCamera;

    [Tooltip("맵 로컬 좌표 → 월드 좌표 변환에 사용")]
    [SerializeField] private ARNavigationController navigationController;

    [Header("도슨트 패널 설정")]
    [Tooltip("패널 너비 (미터)")]
    [SerializeField] private float panelWidth = 0.55f;

    [Tooltip("패널 높이 (미터)")]
    [SerializeField] private float panelHeight = 0.70f;

    [Tooltip("바닥에서 패널 중심까지의 높이 (미터)")]
    [SerializeField] private float panelHeightOffset = 0.0f;

    [Tooltip("한국어 TTF 파일을 직접 연결하세요. (MalgunGothic.ttf → 런타임에 TMP_FontAsset으로 자동 변환)")]
    [SerializeField] private Font docentFontTTF;

    [Tooltip("TMP_FontAsset이 있을 경우 직접 연결 (docentFontTTF보다 우선 적용)")]
    [SerializeField] private TMP_FontAsset docentFont;

    [Header("씬 앵커 (선택)")]
    [Tooltip("전시품 이름과 씬 오브젝트를 연결합니다. 앵커가 있는 전시품은 해당 오브젝트 위치에 패널이 고정됩니다.")]
    [SerializeField] private DocentAnchorEntry[] sceneAnchors;

    // ── 내부 상태 ────────────────────────────────────────────────────
    // panel: 생성된 패널 루트, localPos: DB 좌표(앵커 없을 때 폴백), mapIndex: 맵 구분, anchor: 씬 앵커(null 가능)
    private readonly List<(GameObject panel, Vector3 localPos, int mapIndex, Transform anchor)> _panels = new();
    private bool _docentVisible = false;

    // ── 공개 프로퍼티 ────────────────────────────────────────────────
    public bool IsDocentVisible => _docentVisible;

    // ════════════════════════════════════════════════════════════════
    //  Unity 생명주기
    // ════════════════════════════════════════════════════════════════

    void Awake()
    {
        // Inspector 에서 연결되지 않은 경우 씬에서 자동으로 찾아 연결
        if (navigationController == null)
            navigationController = FindObjectOfType<ARNavigationController>();

        if (arCamera == null)
        {
            arCamera = Camera.main;
            if (arCamera == null)
                arCamera = FindObjectOfType<Camera>();
        }

        if (navigationController == null)
            Debug.LogWarning("DocentManager: ARNavigationController 를 찾을 수 없습니다.");
        if (arCamera == null)
            Debug.LogWarning("DocentManager: AR Camera 를 찾을 수 없습니다.");
    }

    /// <summary>
    /// 폰트 로드: TMP_FontAsset 직접 연결 → TTF 연결 후 동적 생성 → AssetDatabase(에디터) → Resources 순으로 시도.
    /// Initialize() 에서 패널 생성 전에 호출됩니다.
    /// </summary>
    private void LoadFont()
    {
        // TMP_FontAsset 이 이미 연결된 경우 (최우선)
        if (docentFont != null) return;

        // TTF 필드에서 TMP_FontAsset 동적 생성 (Inspector에서 MalgunGothic.ttf 연결 시)
        if (docentFontTTF != null)
        {
            docentFont = TMP_FontAsset.CreateFontAsset(docentFontTTF);
            Debug.Log("DocentManager: TTF에서 TMP 폰트 에셋 생성 완료");
            return;
        }

#if UNITY_EDITOR
        // 에디터 폴백 1: 이미 생성된 SDF 에셋 직접 로드 (가장 빠르고 안정적)
        var sdfAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/UI Toolkit/Fonts/MalgunGothic SDF.asset");
        if (sdfAsset != null)
        {
            docentFont = sdfAsset;
            Debug.Log("DocentManager: MalgunGothic SDF 에셋 직접 로드 완료");
            return;
        }

        // 에디터 폴백 2: TTF를 로드해 TMP_FontAsset 동적 생성
        var ttf = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(
            "Assets/UI Toolkit/Fonts/MalgunGothic.ttf");
        if (ttf != null)
        {
            docentFont = TMP_FontAsset.CreateFontAsset(ttf);
            Debug.Log("DocentManager: AssetDatabase TTF에서 TMP 폰트 에셋 생성 완료");
            return;
        }
#endif

        // 빌드 폴백 1: Resources/Fonts/MalgunGothic SDF (TMP_FontAsset)
        var sdfFromResources = Resources.Load<TMP_FontAsset>("Fonts/MalgunGothic SDF");
        if (sdfFromResources != null)
        {
            docentFont = sdfFromResources;
            Debug.Log("DocentManager: Resources에서 SDF 폰트 에셋 로드 완료");
            return;
        }

        // 빌드 폴백 2: Resources/Fonts/MalgunGothic.ttf (TTF)
        var fontFromResources = Resources.Load<Font>("Fonts/MalgunGothic");
        if (fontFromResources != null)
        {
            docentFont = TMP_FontAsset.CreateFontAsset(fontFromResources);
            Debug.Log("DocentManager: Resources TTF에서 TMP 폰트 에셋 생성 완료");
            return;
        }

        Debug.LogWarning("DocentManager: 한국어 폰트를 로드할 수 없습니다.\n" +
                         "Inspector의 'Docent Font TTF' 필드에 MalgunGothic.ttf를 연결해주세요.");
    }

    void Update()
    {
        if (!_docentVisible || navigationController == null) return;

        foreach (var entry in _panels)
        {
            if (entry.panel == null || !entry.panel.activeSelf) continue;

            // 앵커가 있으면 위치는 Transform 부모 관계가 자동 처리 → 스킵
            if (entry.anchor != null) continue;

            // 앵커 없음: Immersal XRSpace 변환으로 매 프레임 위치 동기화
            Vector3 worldPos = navigationController.MapLocalToWorld(entry.localPos, entry.mapIndex);
            worldPos.y = panelHeightOffset;
            entry.panel.transform.position = worldPos;
        }
    }

    void LateUpdate()
    {
        if (arCamera == null || !_docentVisible) return;

        // 빌보드: 모든 패널이 카메라를 정면으로 향하도록 Y축 회전 (앵커 유무 무관)
        foreach (var entry in _panels)
        {
            if (entry.panel == null || !entry.panel.activeSelf) continue;
            Vector3 dir = arCamera.transform.position - entry.panel.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                entry.panel.transform.rotation = Quaternion.LookRotation(-dir.normalized, Vector3.up);
        }
    }

    void OnDestroy()
    {
        ClearPanels();
    }

    // ════════════════════════════════════════════════════════════════
    //  공개 메서드 (UIManager 에서 호출)
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 전시품 배열을 받아 각 위치에 도슨트 패널을 생성합니다.
    /// AR 내비게이션 시작 시 UIManager 에서 호출합니다.
    /// 초기 상태는 숨김입니다.
    /// </summary>
    public void Initialize(Exhibit[] exhibits)
    {
        ClearPanels();
        _docentVisible = false;

        // 폰트 로드 (Inspector 미연결 시 메모리/AssetDatabase에서 탐색)
        LoadFont();

        if (exhibits == null || exhibits.Length == 0)
        {
            Debug.Log("DocentManager: 전시품 데이터가 없습니다.");
            return;
        }

        if (navigationController == null)
        {
            Debug.LogError("DocentManager: ARNavigationController 가 연결되지 않았습니다.");
            return;
        }

        foreach (var exhibit in exhibits)
        {
            // 이름이 없으면 패널 생성 의미 없음
            if (string.IsNullOrEmpty(exhibit.name)) continue;

            Transform anchor = FindAnchor(exhibit.name);

            Vector3 worldPos;
            if (anchor != null)
            {
                // 앵커 모드: 씬 오브젝트 위치를 그대로 사용
                worldPos = anchor.position;
                worldPos.y += panelHeightOffset;
            }
            else
            {
                // 폴백: DB 좌표 + XRSpace 변환
                worldPos = navigationController.MapLocalToWorld(exhibit.localPosition, exhibit.mapIndex);
                worldPos.y = panelHeightOffset;
            }

            RawImage rawImage;
            var panel = CreateDocentPanel(exhibit, worldPos, out rawImage);

            if (anchor != null)
            {
                // 앵커 자식으로 부착 → 앵커 이동 시 패널도 따라감
                panel.transform.SetParent(anchor, true);
            }

            panel.SetActive(false); // 초기 숨김 — ToggleDocents() 호출 전까지 비표시
            _panels.Add((panel, exhibit.localPosition, exhibit.mapIndex, anchor));

            // 이미지 URL이 있으면 비동기 다운로드 후 RawImage에 적용
            if (!string.IsNullOrEmpty(exhibit.imageUrl) && rawImage != null && ApiClient.Instance != null)
                StartCoroutine(LoadImageAsync(exhibit.imageUrl, rawImage));
        }

        Debug.Log($"DocentManager: 도슨트 패널 {_panels.Count}개 생성 완료");
    }

    /// <summary>
    /// 도슨트 전체 표시/숨김을 토글합니다.
    /// UIManager 의 btn-toggle-docent 버튼 클릭 시 호출합니다.
    /// </summary>
    public void ToggleDocents()
    {
        _docentVisible = !_docentVisible;
        foreach (var entry in _panels)
            entry.panel?.SetActive(_docentVisible);

        // 활성화 후 레이아웃 강제 재계산 — TMP 다이나믹 폰트가 글리프를 atlas에 등록한 뒤 적용
        if (_docentVisible)
            StartCoroutine(RebuildAllPanelLayouts());

        Debug.Log($"DocentManager: 도슨트 {(_docentVisible ? "표시" : "숨김")}");
    }

    // 2프레임 대기 후 모든 패널의 레이아웃 강제 재계산
    private IEnumerator RebuildAllPanelLayouts()
    {
        yield return null;
        yield return null;
        foreach (var entry in _panels)
        {
            if (entry.panel == null || !entry.panel.activeSelf) continue;
            var canvasRect = entry.panel.GetComponentInChildren<Canvas>()
                                 ?.GetComponent<RectTransform>();
            if (canvasRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRect);
        }
    }

    /// <summary>
    /// 도슨트를 강제로 숨깁니다 (AR 화면 종료 시 호출).
    /// </summary>
    public void HideAll()
    {
        _docentVisible = false;
        foreach (var entry in _panels)
            entry.panel?.SetActive(false);
    }

    // ════════════════════════════════════════════════════════════════
    //  패널 생성 (World Space Canvas)
    // ════════════════════════════════════════════════════════════════

    private GameObject CreateDocentPanel(Exhibit exhibit, Vector3 worldPos, out RawImage rawImageOut)
    {
        rawImageOut = null;

        // ── 루트 오브젝트 ──────────────────────────────────────────
        var root = new GameObject($"Docent_{exhibit.exhibitId}");
        root.transform.position = worldPos;

        // ── World Space Canvas ─────────────────────────────────────
        var canvasGO = new GameObject("Canvas");
        canvasGO.transform.SetParent(root.transform, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.WorldSpace;
        canvas.worldCamera = arCamera;

        // dynamicPixelsPerUnit: 텍스트 선명도
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // 1 local unit = 0.001m (1mm) 기준
        float unitPerMeter = 1000f;
        var canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(panelWidth * unitPerMeter, panelHeight * unitPerMeter);
        canvasGO.transform.localScale = Vector3.one * (1f / unitPerMeter);

        // ── 테두리 (맨 뒤) ────────────────────────────────────────
        CreateImage(canvasGO.transform, "Border",
            new Color(0.28f, 0.58f, 1f, 0.85f),
            new Vector2(-6f, -6f), new Vector2(6f, 6f),
            siblingIndex: 0);

        // ── 배경 ──────────────────────────────────────────────────
        CreateImage(canvasGO.transform, "Background",
            new Color(0.04f, 0.07f, 0.16f, 0.90f),
            Vector2.zero, Vector2.zero,
            siblingIndex: 1);

        // ── 콘텐츠 영역 (패딩 적용, 세로 배치) ───────────────────
        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(canvasGO.transform, false);
        var contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        float pad = 36f;
        contentRect.offsetMin = new Vector2(pad, pad);
        contentRect.offsetMax = new Vector2(-pad, -pad);

        var vLayout = contentGO.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing              = 14f;
        vLayout.childForceExpandWidth  = true;
        vLayout.childForceExpandHeight = false;
        vLayout.childControlWidth      = true;
        vLayout.childControlHeight     = true;
        vLayout.childAlignment         = TextAnchor.UpperLeft;

        // ── 이미지 섹션 (상단) ─────────────────────────────────
        var imgSectionGO = new GameObject("ImageSection");
        imgSectionGO.transform.SetParent(contentGO.transform, false);
        imgSectionGO.AddComponent<RectTransform>();

        // 이미지 영역 높이를 패널 높이의 약 40%로 고정
        float imgHeight = panelHeight * unitPerMeter * 0.40f;
        var imgLayout = imgSectionGO.AddComponent<LayoutElement>();
        imgLayout.preferredHeight = imgHeight;
        imgLayout.flexibleWidth   = 1f;

        // 이미지가 없을 때 보여줄 어두운 플레이스홀더 배경
        var imgBgGO = new GameObject("ImageBackground");
        imgBgGO.transform.SetParent(imgSectionGO.transform, false);
        var imgBgImg = imgBgGO.AddComponent<Image>();
        imgBgImg.color = new Color(0.08f, 0.12f, 0.22f, 0.80f);
        var imgBgRect = imgBgGO.GetComponent<RectTransform>();
        imgBgRect.anchorMin = Vector2.zero;
        imgBgRect.anchorMax = Vector2.one;
        imgBgRect.offsetMin = Vector2.zero;
        imgBgRect.offsetMax = Vector2.zero;

        // RawImage: 다운로드 완료 시 텍스처를 적용
        var rawImgGO = new GameObject("ExhibitImage");
        rawImgGO.transform.SetParent(imgSectionGO.transform, false);
        var rawImg = rawImgGO.AddComponent<RawImage>();
        rawImg.color = Color.white;
        var rawImgRect = rawImgGO.GetComponent<RectTransform>();
        rawImgRect.anchorMin = Vector2.zero;
        rawImgRect.anchorMax = Vector2.one;
        rawImgRect.offsetMin = Vector2.zero;
        rawImgRect.offsetMax = Vector2.zero;

        // AspectRatioFitter: 이미지 비율을 유지하면서 영역에 맞게 표시
        var arf = rawImgGO.AddComponent<AspectRatioFitter>();
        arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        arf.aspectRatio = 4f / 3f; // 다운로드 전 기본 비율, 로드 후 실제 비율로 갱신

        rawImageOut = rawImg;

        // ── 구분선 (이미지 ↔ 텍스트) ─────────────────────────────
        AddDivider(contentGO.transform);

        // ── 전시품 이름 (굵게, 밝은 흰색) ───────────────────────
        AddText(contentGO.transform, exhibit.name,
            fontSize: 72f, bold: true,
            color: new Color(0.93f, 0.96f, 1f));

        // ── 특징 (있을 때만, 황금색 계열) ───────────────────────
        if (!string.IsNullOrEmpty(exhibit.feature))
            AddText(contentGO.transform, exhibit.feature,
                fontSize: 50f, bold: false,
                color: new Color(1.00f, 0.85f, 0.45f));

        // ── 작가 (파란 계열, 있을 때만) ─────────────────────────
        if (!string.IsNullOrEmpty(exhibit.artist))
            AddText(contentGO.transform, exhibit.artist,
                fontSize: 50f, bold: false,
                color: new Color(0.55f, 0.80f, 1f));

        // ── 위치/관 (회색 계열, 있을 때만) ──────────────────────
        if (!string.IsNullOrEmpty(exhibit.hall))
            AddText(contentGO.transform, exhibit.hall,
                fontSize: 44f, bold: false,
                color: new Color(0.55f, 0.65f, 0.80f));

        // ── 도슨트 설명 (있을 때만) ──────────────────────────────
        if (!string.IsNullOrEmpty(exhibit.docentText))
        {
            AddDivider(contentGO.transform);

            // 200자 이내로 제한
            string desc = exhibit.docentText.Length > 200
                ? exhibit.docentText.Substring(0, 197) + "..."
                : exhibit.docentText;
            AddText(contentGO.transform, desc,
                fontSize: 44f, bold: false,
                color: new Color(0.82f, 0.87f, 0.95f),
                maxLines: 4);
        }

        // AR 평면 메시에 가려지지 않도록 ZTest Always 설정
        SetZTestAlways(root);

        return root;
    }

    // ── 구분선 헬퍼 ─────────────────────────────────────────────────
    private void AddDivider(Transform parent)
    {
        var divGO = new GameObject("Divider");
        divGO.transform.SetParent(parent, false);
        var divImg = divGO.AddComponent<Image>();
        divImg.color = new Color(0.28f, 0.55f, 1f, 0.45f);
        var divLayout = divGO.AddComponent<LayoutElement>();
        divLayout.preferredHeight = 3f;
        divLayout.flexibleWidth   = 1f;
    }

    // ── 이미지 비동기 다운로드 코루틴 ───────────────────────────────
    private IEnumerator LoadImageAsync(string imageUrl, RawImage rawImage)
    {
        if (string.IsNullOrEmpty(imageUrl) || rawImage == null) yield break;

        yield return StartCoroutine(
            ApiClient.Instance.DownloadTextureAsync(imageUrl, (texture, error) =>
            {
                if (error != null)
                {
                    Debug.LogWarning($"DocentManager: 이미지 다운로드 실패 ({error})");
                    return;
                }
                if (texture == null || rawImage == null) return;

                rawImage.texture = texture;

                // 실제 이미지 비율로 AspectRatioFitter 갱신
                var arf = rawImage.GetComponent<AspectRatioFitter>();
                if (arf != null && texture.height > 0)
                    arf.aspectRatio = (float)texture.width / texture.height;
            }));
    }

    // ── 이미지 요소 생성 헬퍼 ────────────────────────────────────────
    private Image CreateImage(Transform parent, string name, Color color,
        Vector2 offsetMin, Vector2 offsetMax, int siblingIndex = -1)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img  = go.AddComponent<Image>();
        img.color = color;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        if (siblingIndex >= 0) go.transform.SetSiblingIndex(siblingIndex);
        return img;
    }

    // ── TMP 텍스트 요소 생성 헬퍼 ────────────────────────────────────
    private TextMeshProUGUI AddText(Transform parent, string text,
        float fontSize, bool bold, Color color, int maxLines = 0)
    {
        var go  = new GameObject("Text");
        go.transform.SetParent(parent, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize           = fontSize;
        tmp.color              = color;
        tmp.fontStyle          = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.enableWordWrapping = true;
        tmp.overflowMode       = TextOverflowModes.Truncate;
        if (maxLines > 0) tmp.maxVisibleLines = maxLines;

        // 폰트 먼저 지정 후 텍스트 설정 (폰트 변경 시 레이아웃 재계산 방지)
        if (docentFont != null) tmp.font = docentFont;
        tmp.text = text;

        // 다이나믹 폰트의 한국어 글리프를 즉시 atlas에 로드
        // 비활성 상태에서도 강제 실행 — ContentSizeFitter가 preferredHeight 0을 반환하는 문제 방지
        tmp.ForceMeshUpdate(ignoreActiveState: true);

        // LayoutGroup 이 높이를 자동 계산하도록 ContentSizeFitter 추가
        var csf = go.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        return tmp;
    }

    // ── 패널 전체 삭제 ──────────────────────────────────────────────
    private void ClearPanels()
    {
        foreach (var entry in _panels)
        {
            if (entry.panel != null)
                Destroy(entry.panel);
        }
        _panels.Clear();
        _docentVisible = false;
    }

    // ── 앵커 탐색: 전시품 이름으로 sceneAnchors 배열에서 일치하는 Transform 반환 ──
    private Transform FindAnchor(string exhibitName)
    {
        if (sceneAnchors == null) return null;
        foreach (var entry in sceneAnchors)
        {
            if (entry.anchor != null &&
                string.Equals(entry.exhibitName, exhibitName, System.StringComparison.Ordinal))
                return entry.anchor;
        }
        return null;
    }

    // ── ZTest Always 설정 (AR 바닥 메시에 가려지지 않도록) ──────────
    private void SetZTestAlways(GameObject root)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            foreach (var mat in renderer.materials)
            {
                mat.SetInt("unity_GUIZTestMode",
                    (int)UnityEngine.Rendering.CompareFunction.Always);
                mat.SetInt("_ZTest",
                    (int)UnityEngine.Rendering.CompareFunction.Always);
            }
        }
    }
}

// Inspector에서 전시품 이름 ↔ 씬 앵커 오브젝트를 연결하는 데이터 클래스
[System.Serializable]
public class DocentAnchorEntry
{
    [Tooltip("DB에 등록된 전시품 이름 (artworks.title) 과 정확히 일치해야 합니다.")]
    public string exhibitName;

    [Tooltip("씬에 배치한 빈 GameObject. 이 오브젝트의 위치에 도슨트 패널이 고정됩니다.")]
    public Transform anchor;
}
