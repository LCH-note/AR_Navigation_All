/*
    파일명: Assets/Network/DataSyncManager.cs
    역할: 앱 시작 시 백엔드 데이터를 일괄 로드·캐싱하고,
          방문자 카운팅과 리뷰 제출을 담당하는 중앙 데이터 관리자 (싱글톤)

    씬 배치:
      ApiClient 컴포넌트와 같은 GameObject (또는 별도 GameObject)에 배치하세요.

    앱 시작 흐름:
      1. Start() → LoadAllDataAsync() 코루틴 시작
      2. GET /routes   → LoadedRoutes 갱신 (실패 시 Mock 폴백)
      3. GET /exhibits → LoadedExhibits 갱신 (실패 시 Mock 폴백)
      4. GET /assets/map       → 버전 체크 후 조건부 다운로드 → MapFilePath
      5. GET /assets/floor-plan → 버전 체크 후 조건부 다운로드 → FloorPlanTexture
      6. IsDataReady = true, OnDataReady 이벤트 발행

    방문자 카운팅:
      UIManager.OnStartClicked() 에서 SubmitVisitorAsync(ageGroup) 호출.
      기기당 1회만 전송 (PlayerPrefs 로컬 체크 + 서버 deviceId 중복 방지 병행).

    리뷰 제출:
      UserReviewController.OnSubmitReview() 에서 SubmitReviewAsync() 호출.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DataSyncManager : MonoBehaviour
{
    // ── 싱글톤 ──────────────────────────────────────────────────────
    public static DataSyncManager Instance { get; private set; }

    // ── 로드된 데이터 (전역 읽기 전용 접근) ─────────────────────────
    public static NavRoute[]  LoadedRoutes     { get; private set; }
    public static Exhibit[]   LoadedExhibits   { get; private set; }
    public static string                      MapFilePath        { get; private set; }  // 맵 A (145963)
    public static string                      MapFilePath2       { get; private set; }  // 맵 B (145962)
    public static Texture2D                   FloorPlanTexture   { get; private set; }  // 단일 평면도 (레거시 호환)
    public static Dictionary<string, Texture2D> FloorPlanTextures   { get; private set; } // 플로어별 평면도
    public static Dictionary<string, string>   ThreeDModelUrls     { get; private set; } // 플로어별 3D 전체도 URL
    public static bool                         IsDataReady         { get; private set; }

    // ── 이벤트 ───────────────────────────────────────────────────────
    public static event Action OnDataReady;   // 모든 데이터 로드 완료

    // ── 빌드 내장 맵 파일 (Assets/Maps/) ────────────────────────────
    [Header("빌드 내장 맵 파일 (Assets/Maps)")]
    [Tooltip("145963-real2.bytes — 연결 시 백엔드 다운로드를 스킵하고 이 파일을 직접 사용")]
    [SerializeField] private TextAsset mapAssetA;

    [Tooltip("145962-real.bytes — 연결 시 백엔드 다운로드를 스킵하고 이 파일을 직접 사용")]
    [SerializeField] private TextAsset mapAssetB;

    // ── PlayerPrefs 키 상수 ──────────────────────────────────────────
    private const string KeyVisitorCounted      = "visitor_counted_v1";
    private const string KeyMapVersion          = "cached_map_version";
    private const string KeyMapVersion2         = "cached_map_b_version";
    private const string KeyFloorVersion        = "cached_floor_version";
    private const string KeyFloorVersionPrefix  = "cached_fp_v_";   // + floor 이름
    private const string MapFileName            = "immersal_map_data.bytes";
    private const string MapFileName2           = "immersal_map_b_data.bytes";
    private const string FloorPlanFileName      = "floor_plan.png";
    private const string FloorPlanFilePrefix    = "floor_plan_";    // + floor 이름 + ".png"

    // ════════════════════════════════════════════════════════════════
    //  Unity 생명주기
    // ════════════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        StartCoroutine(LoadAllDataAsync());
    }

    // ════════════════════════════════════════════════════════════════
    //  전체 데이터 로드 (앱 시작 시 1회)
    // ════════════════════════════════════════════════════════════════

    private IEnumerator LoadAllDataAsync()
    {
        IsDataReady = false;

        if (ApiClient.Instance == null)
        {
            Debug.LogError("DataSyncManager: ApiClient 인스턴스가 없습니다. 씬에 ApiClient를 배치하세요.");
            LoadedRoutes   = new NavRoute[0];
            LoadedExhibits = new Exhibit[0];
            IsDataReady = true;
            OnDataReady?.Invoke();
            yield break;
        }

        // ① 경로 목록 로드
        yield return StartCoroutine(
            ApiClient.Instance.GetArrayAsync<RouteDto, RouteListWrapper>(
                "/routes",
                wrap => wrap.items,
                (dtos, err) =>
                {
                    if (err != null)
                    {
                        Debug.LogWarning($"DataSyncManager: 경로 로드 실패. ({err})");
                        LoadedRoutes = new NavRoute[0];
                    }
                    else
                    {
                        LoadedRoutes = ConvertRoutes(dtos);
                        Debug.Log($"DataSyncManager: 경로 {LoadedRoutes.Length}개 로드 완료");
                    }
                }));

        // ② 전시물 목록 로드
        yield return StartCoroutine(
            ApiClient.Instance.GetArrayAsync<ExhibitDto, ExhibitListWrapper>(
                "/exhibits",
                wrap => wrap.items,
                (dtos, err) =>
                {
                    if (err != null)
                    {
                        Debug.LogWarning($"DataSyncManager: 전시물 로드 실패. ({err})");
                        LoadedExhibits = new Exhibit[0];
                    }
                    else
                    {
                        LoadedExhibits = ConvertExhibits(dtos);
                        Debug.Log($"DataSyncManager: 전시물 {LoadedExhibits.Length}개 로드 완료");
                    }
                }));

        // ③ 맵 A 데이터 파일 (145963)
        // Inspector에 TextAsset이 연결된 경우 빌드 내장 파일을 직접 사용, 없으면 백엔드 다운로드
        if (mapAssetA != null)
            WriteBuiltInMap(mapAssetA, MapFileName, path => { MapFilePath = path; });
        else
            yield return StartCoroutine(
                FetchAssetIfNeeded(
                    endpoint:   "/assets/map",
                    fileName:   MapFileName,
                    versionKey: KeyMapVersion,
                    isTexture:  false,
                    onFile:     path => { MapFilePath = path; },
                    onTexture:  null));

        // ④ 맵 B 데이터 파일 (145962)
        if (mapAssetB != null)
            WriteBuiltInMap(mapAssetB, MapFileName2, path => { MapFilePath2 = path; });
        else
            yield return StartCoroutine(
                FetchAssetIfNeeded(
                    endpoint:   "/assets/map-b",
                    fileName:   MapFileName2,
                    versionKey: KeyMapVersion2,
                    isTexture:  false,
                    onFile:     path => { MapFilePath2 = path; },
                    onTexture:  null));

        // ⑤ 2D 평면도 단일 (레거시 호환 — ARMapScreen 오버레이용)
        yield return StartCoroutine(
            FetchAssetIfNeeded(
                endpoint:   "/assets/floor-plan",
                fileName:   FloorPlanFileName,
                versionKey: KeyFloorVersion,
                isTexture:  true,
                onFile:     null,
                onTexture:  tex => { FloorPlanTexture = tex; }));

        // ⑥ 플로어별 2D 평면도 (MapScreen 전체 지도 화면용)
        FloorPlanTextures = new Dictionary<string, Texture2D>();
        yield return StartCoroutine(LoadAllFloorPlansAsync());

        // ⑦ 플로어별 3D 전체도 URL (MapScreen 3D 뷰용)
        ThreeDModelUrls = new Dictionary<string, string>();
        yield return StartCoroutine(LoadAll3dModelsAsync());

        IsDataReady = true;
        Debug.Log("DataSyncManager: 모든 데이터 초기화 완료");
        OnDataReady?.Invoke();
    }

    // ════════════════════════════════════════════════════════════════
    //  빌드 내장 맵 파일 로드 (TextAsset → persistentDataPath 기록)
    // ════════════════════════════════════════════════════════════════

    // TextAsset 바이트를 persistentDataPath에 쓰고 경로를 콜백으로 전달합니다.
    // Immersal SDK가 파일 경로를 필요로 할 때를 대비해 persistentDataPath에 기록해 둡니다.
    private void WriteBuiltInMap(TextAsset asset, string fileName, Action<string> onFile)
    {
        string localPath = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllBytes(localPath, asset.bytes);
        onFile?.Invoke(localPath);
        Debug.Log($"DataSyncManager: {asset.name} — 내장 맵 파일 로드 완료 → {localPath}");
    }

    // ════════════════════════════════════════════════════════════════
    //  에셋 파일 버전 체크 + 조건부 다운로드
    // ════════════════════════════════════════════════════════════════

    private IEnumerator FetchAssetIfNeeded(
        string            endpoint,
        string            fileName,
        string            versionKey,
        bool              isTexture,
        Action<string>    onFile,
        Action<Texture2D> onTexture)
    {
        string localPath = Path.Combine(Application.persistentDataPath, fileName);

        // 서버에서 현재 URL과 버전 조회
        AssetUrlResponse response = null;
        yield return StartCoroutine(
            ApiClient.Instance.GetAsync<AssetUrlResponse>(
                endpoint,
                (res, err) =>
                {
                    if (err != null)
                        Debug.LogWarning($"DataSyncManager: {endpoint} 메타데이터 조회 실패. ({err})");
                    else
                        response = res;
                }));

        // 조회 실패 또는 아직 업로드된 파일 없음 → 로컬 캐시가 있으면 사용
        if (response == null || string.IsNullOrEmpty(response.fileUrl))
        {
            if (File.Exists(localPath))
            {
                Debug.Log($"DataSyncManager: {fileName} — 서버에 파일 없음, 로컬 캐시 사용");
                if (!isTexture)
                    onFile?.Invoke(localPath);
                else
                    yield return StartCoroutine(LoadTextureFromFile(localPath, onTexture));
            }
            else
            {
                Debug.Log($"DataSyncManager: {fileName} — 서버에 파일 없음, 로컬 캐시도 없음. 스킵");
            }
            yield break;
        }

        bool isCacheValid = PlayerPrefs.GetString(versionKey, "") == response.version
                            && File.Exists(localPath);

        if (isCacheValid)
        {
            Debug.Log($"DataSyncManager: {fileName} — 캐시 유효 (버전 {response.version})");
            if (!isTexture)
                onFile?.Invoke(localPath);
            else
                yield return StartCoroutine(LoadTextureFromFile(localPath, onTexture));
            yield break;
        }

        // 새 버전 다운로드
        Debug.Log($"DataSyncManager: {fileName} — 새 버전 다운로드 시작 (버전 {response.version})");

        if (!isTexture)
        {
            yield return StartCoroutine(
                ApiClient.Instance.DownloadBytesAsync(
                    response.fileUrl,
                    (bytes, err) =>
                    {
                        if (err != null) { Debug.LogWarning($"DataSyncManager: {fileName} 다운로드 실패. ({err})"); return; }
                        File.WriteAllBytes(localPath, bytes);
                        PlayerPrefs.SetString(versionKey, response.version);
                        PlayerPrefs.Save();
                        onFile?.Invoke(localPath);
                        Debug.Log($"DataSyncManager: {fileName} 저장 완료 → {localPath}");
                    }));
        }
        else
        {
            yield return StartCoroutine(
                ApiClient.Instance.DownloadTextureAsync(
                    response.fileUrl,
                    (tex, err) =>
                    {
                        if (err != null) { Debug.LogWarning($"DataSyncManager: {fileName} 이미지 다운로드 실패. ({err})"); return; }
                        File.WriteAllBytes(localPath, tex.EncodeToPNG());
                        PlayerPrefs.SetString(versionKey, response.version);
                        PlayerPrefs.Save();
                        onTexture?.Invoke(tex);
                        Debug.Log($"DataSyncManager: {fileName} 이미지 저장 완료 → {localPath}");
                    }));
        }
    }

    // 로컬 파일에서 Texture2D 로드
    private IEnumerator LoadTextureFromFile(string path, Action<Texture2D> onTexture)
    {
        var tex = new Texture2D(2, 2);
        tex.LoadImage(File.ReadAllBytes(path));
        onTexture?.Invoke(tex);
        yield return null;
    }

    // ════════════════════════════════════════════════════════════════
    //  플로어별 2D 평면도 로딩 (MapScreen 전체 지도 화면용)
    // ════════════════════════════════════════════════════════════════

    // GET /assets/floor-plans → FloorPlanDto[] → FloorPlanTextures 딕셔너리 채움
    private IEnumerator LoadAllFloorPlansAsync()
    {
        FloorPlanDto[] plans = null;
        yield return StartCoroutine(
            ApiClient.Instance.GetArrayAsync<FloorPlanDto, FloorPlanListWrapper>(
                "/assets/floor-plans",
                wrap => wrap.items,
                (dtos, err) =>
                {
                    if (err != null)
                        Debug.LogWarning($"DataSyncManager: 플로어 평면도 목록 로드 실패. ({err})");
                    else
                        plans = dtos;
                }));

        if (plans == null || plans.Length == 0)
        {
            Debug.Log("DataSyncManager: 등록된 플로어 평면도 없음");
            yield break;
        }

        foreach (var fp in plans)
        {
            if (string.IsNullOrEmpty(fp.fileUrl)) continue;
            yield return StartCoroutine(LoadFloorPlanTextureAsync(fp));
        }
        Debug.Log($"DataSyncManager: 플로어 평면도 {FloorPlanTextures.Count}개 로드 완료 " +
                  $"({string.Join(", ", FloorPlanTextures.Keys)})");
    }

    // 단일 플로어 평면도를 버전 체크 후 조건부 다운로드해 FloorPlanTextures에 저장
    private IEnumerator LoadFloorPlanTextureAsync(FloorPlanDto dto)
    {
        string versionKey = KeyFloorVersionPrefix + dto.floor;
        string fileName   = FloorPlanFilePrefix + dto.floor + ".png";
        string localPath  = Path.Combine(Application.persistentDataPath, fileName);

        // 캐시 유효 시 로컬 파일 사용
        bool isCacheValid = PlayerPrefs.GetString(versionKey, "") == dto.version
                            && File.Exists(localPath);
        if (isCacheValid)
        {
            yield return StartCoroutine(LoadTextureFromFile(localPath,
                tex => { FloorPlanTextures[dto.floor] = tex; }));
            Debug.Log($"DataSyncManager: {dto.floor} 평면도 캐시 사용 (버전 {dto.version})");
            yield break;
        }

        // 새 버전 다운로드
        Debug.Log($"DataSyncManager: {dto.floor} 평면도 다운로드 시작");
        yield return StartCoroutine(
            ApiClient.Instance.DownloadTextureAsync(
                dto.fileUrl,
                (tex, err) =>
                {
                    if (err != null)
                    {
                        Debug.LogWarning($"DataSyncManager: {dto.floor} 평면도 다운로드 실패. ({err})");
                        return;
                    }
                    File.WriteAllBytes(localPath, tex.EncodeToPNG());
                    PlayerPrefs.SetString(versionKey, dto.version);
                    PlayerPrefs.Save();
                    FloorPlanTextures[dto.floor] = tex;
                    Debug.Log($"DataSyncManager: {dto.floor} 평면도 저장 완료");
                }));
    }

    // ════════════════════════════════════════════════════════════════
    //  플로어별 3D 전체도 URL 로딩 (MapScreen 3D 뷰용)
    // ════════════════════════════════════════════════════════════════

    // GET /assets/3d-models → ThreeDModelDto[] → ThreeDModelUrls 딕셔너리 채움
    private IEnumerator LoadAll3dModelsAsync()
    {
        ThreeDModelDto[] models = null;
        yield return StartCoroutine(
            ApiClient.Instance.GetArrayAsync<ThreeDModelDto, ThreeDModelListWrapper>(
                "/assets/3d-models",
                wrap => wrap.items,
                (dtos, err) =>
                {
                    if (err != null)
                        Debug.LogWarning($"DataSyncManager: 3D 전체도 목록 로드 실패. ({err})");
                    else
                        models = dtos;
                }));

        if (models == null || models.Length == 0)
        {
            Debug.Log("DataSyncManager: 등록된 3D 전체도 없음");
            yield break;
        }

        foreach (var m in models)
        {
            if (!string.IsNullOrEmpty(m.floor) && !string.IsNullOrEmpty(m.fileUrl))
                ThreeDModelUrls[m.floor] = m.fileUrl;
        }
        Debug.Log($"DataSyncManager: 3D 전체도 {ThreeDModelUrls.Count}개 로드 완료 " +
                  $"({string.Join(", ", ThreeDModelUrls.Keys)})");
    }

    // ════════════════════════════════════════════════════════════════
    //  방문자 카운팅 (UIManager.OnStartClicked 에서 호출)
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 기기당 1회만 방문자 등록 API를 호출합니다.
    /// 연령대(ageGroup)와 방문 일시(UTC)를 함께 전송합니다.
    /// callback(alreadyCounted) — true 이면 이미 카운팅된 기기
    /// </summary>
    public IEnumerator SubmitVisitorAsync(string ageGroup, Action<bool> callback = null)
    {
        bool alreadyCounted = PlayerPrefs.GetInt(KeyVisitorCounted, 0) == 1;
        if (alreadyCounted)
        {
            Debug.Log("DataSyncManager: 이미 카운팅된 기기 — 방문자 등록 스킵");
            callback?.Invoke(true);
            yield break;
        }

        if (ApiClient.Instance == null) { callback?.Invoke(false); yield break; }

        var request = new VisitorRequest
        {
            deviceId  = SystemInfo.deviceUniqueIdentifier,
            visitedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ageGroup  = ageGroup
        };

        yield return StartCoroutine(
            ApiClient.Instance.PostAsync(
                "/visitors",
                request,
                (ok, err) =>
                {
                    if (ok)
                    {
                        PlayerPrefs.SetInt(KeyVisitorCounted, 1);
                        PlayerPrefs.Save();
                        Debug.Log($"DataSyncManager: 방문자 등록 완료 (연령대: {ageGroup})");
                    }
                    else
                    {
                        Debug.LogWarning($"DataSyncManager: 방문자 등록 실패. ({err})");
                    }
                    callback?.Invoke(false);
                }));
    }

    // ════════════════════════════════════════════════════════════════
    //  리뷰 제출 (UserReviewController 에서 호출)
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 별점과 의견을 백엔드로 전송합니다.
    /// callback(success)
    /// </summary>
    public IEnumerator SubmitReviewAsync(int rating, string comment, Action<bool> callback = null)
    {
        if (ApiClient.Instance == null) { callback?.Invoke(false); yield break; }

        // content 필드명으로 전송 — 백엔드 CreateReviewDto.content 와 일치
        var request = new ReviewRequest { rating = rating, content = comment };

        yield return StartCoroutine(
            ApiClient.Instance.PostAsync(
                "/reviews",
                request,
                (ok, err) =>
                {
                    if (ok)
                        Debug.Log($"DataSyncManager: 리뷰 제출 완료 (별점: {rating}점)");
                    else
                        Debug.LogWarning($"DataSyncManager: 리뷰 제출 실패. ({err})");

                    callback?.Invoke(ok);
                }));
    }

    // ════════════════════════════════════════════════════════════════
    //  DTO → 도메인 모델 변환
    // ════════════════════════════════════════════════════════════════

    private static NavRoute[] ConvertRoutes(RouteDto[] dtos)
    {
        if (dtos == null || dtos.Length == 0) return new NavRoute[0];

        var result = new NavRoute[dtos.Length];
        for (int i = 0; i < dtos.Length; i++)
        {
            var dto       = dtos[i];
            var wpCount   = dto.waypoints?.Length ?? 0;
            var waypoints = new NavWaypoint[wpCount];

            for (int j = 0; j < wpCount; j++)
            {
                var wp       = dto.waypoints[j];
                waypoints[j] = new NavWaypoint
                {
                    localPosition = new Vector3(wp.x, wp.y, wp.z),
                    displayName   = wp.displayName,
                    instruction   = wp.instruction,
                    mapIndex      = wp.mapIndex
                };
            }

            result[i] = new NavRoute
            {
                routeId           = dto.routeId,
                routeName         = dto.routeName,
                destination       = dto.destination,
                description       = dto.description,
                estimatedDistance = dto.estimatedDistance,
                estimatedTime     = dto.estimatedTime,
                waypoints         = waypoints
            };
        }
        return result;
    }

    private static Exhibit[] ConvertExhibits(ExhibitDto[] dtos)
    {
        if (dtos == null || dtos.Length == 0) return new Exhibit[0];

        var result = new Exhibit[dtos.Length];
        for (int i = 0; i < dtos.Length; i++)
        {
            var dto   = dtos[i];
            result[i] = new Exhibit
            {
                exhibitId     = dto.exhibitId,
                name          = dto.name,
                artist        = dto.artist,
                hall          = dto.hall,
                docentText    = dto.docentText,
                imageUrl      = dto.imageUrl,
                feature       = dto.feature,
                localPosition = new Vector3(dto.x, dto.y, dto.z),
                mapIndex      = dto.mapIndex
            };
        }
        return result;
    }
}
