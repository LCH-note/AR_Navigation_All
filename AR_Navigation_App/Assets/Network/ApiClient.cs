/*
    파일명: Assets/Network/ApiClient.cs
    역할: NestJS 백엔드와의 HTTP 통신 단일 창구 (싱글톤 MonoBehaviour)

    씬 배치:
      빈 GameObject ("NetworkManager" 등)에 이 컴포넌트를 추가하세요.
      Inspector 에서 Base Url 에 실제 서버 주소를 입력하세요.
      (예: "http://192.168.0.10:3000" 또는 "https://api.example.com")

    사용 예시:
      StartCoroutine(ApiClient.Instance.PostAsync(
          "/reviews", request, (ok, err) => { ... }));

      StartCoroutine(ApiClient.Instance.GetArrayAsync<ExhibitDto, ExhibitListWrapper>(
          "/exhibits", wrap => wrap.items, (items, err) => { ... }));
*/

using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ApiClient : MonoBehaviour
{
    // ── 싱글톤 ──────────────────────────────────────────────────────
    public static ApiClient Instance { get; private set; }

    // ── Inspector 설정 ───────────────────────────────────────────────
    [Header("서버 기본 URL (끝 슬래시 없이, /api 포함)")]
    [SerializeField] private string baseUrl = "http://localhost:3000/api";

    [Header("일반 요청 타임아웃 (초)")]
    [SerializeField] private int timeoutSeconds = 15;

    [Header("파일 다운로드 타임아웃 (초)")]
    [SerializeField] private int downloadTimeoutSeconds = 60;

    // ── 생명주기 ─────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ════════════════════════════════════════════════════════════════
    //  GET — 단일 JSON 객체
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// GET 요청 후 JSON을 T 타입으로 역직렬화합니다.
    /// callback(data, errorMessage) — 성공 시 errorMessage = null
    /// </summary>
    public IEnumerator GetAsync<T>(string endpoint, Action<T, string> callback)
    {
        using (var req = UnityWebRequest.Get(baseUrl + endpoint))
        {
            req.timeout = timeoutSeconds;
            SetJsonHeaders(req);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                callback(default, $"GET {endpoint} 실패: {req.error}");
                yield break;
            }

            try
            {
                callback(JsonUtility.FromJson<T>(req.downloadHandler.text), null);
            }
            catch (Exception e)
            {
                callback(default, $"JSON 파싱 실패 ({endpoint}): {e.Message}");
            }
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  GET — JSON 배열 (서버가 [...] 루트 배열로 반환하는 경우)
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// GET 요청 후 JSON 배열을 TItem[] 로 역직렬화합니다.
    /// JsonUtility의 루트 배열 미지원 한계를 {"items":[...]} 래핑으로 우회합니다.
    /// TWrap 은 'public TItem[] items' 필드를 가진 래퍼 클래스입니다.
    /// </summary>
    public IEnumerator GetArrayAsync<TItem, TWrap>(
        string             endpoint,
        Func<TWrap, TItem[]> itemsSelector,
        Action<TItem[], string> callback)
    {
        using (var req = UnityWebRequest.Get(baseUrl + endpoint))
        {
            req.timeout = timeoutSeconds;
            SetJsonHeaders(req);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                callback(null, $"GET {endpoint} 실패: {req.error}");
                yield break;
            }

            try
            {
                string raw     = req.downloadHandler.text;
                string wrapped = $"{{\"items\":{raw}}}";
                var    wrap    = JsonUtility.FromJson<TWrap>(wrapped);
                callback(itemsSelector(wrap), null);
            }
            catch (Exception e)
            {
                callback(null, $"JSON 배열 파싱 실패 ({endpoint}): {e.Message}");
            }
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  POST — 응답 본문 무시
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// POST 요청을 전송합니다. 응답 본문은 무시합니다.
    /// callback(success, errorMessage)
    /// </summary>
    public IEnumerator PostAsync<TReq>(
        string             endpoint,
        TReq               body,
        Action<bool, string> callback)
    {
        byte[] jsonBytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(body));

        using (var req = new UnityWebRequest(baseUrl + endpoint, "POST"))
        {
            req.uploadHandler   = new UploadHandlerRaw(jsonBytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout         = timeoutSeconds;
            SetJsonHeaders(req);

            yield return req.SendWebRequest();

            bool ok = req.result == UnityWebRequest.Result.Success;
            callback(ok, ok ? null : $"POST {endpoint} 실패: {req.error}");
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  파일 바이트 다운로드
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// URL에서 바이트 배열로 파일을 다운로드합니다. (맵 데이터 등)
    /// callback(bytes, errorMessage)
    /// </summary>
    public IEnumerator DownloadBytesAsync(string url, Action<byte[], string> callback)
    {
        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = downloadTimeoutSeconds;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                callback(null, $"파일 다운로드 실패: {req.error}");
            else
                callback(req.downloadHandler.data, null);
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  이미지 다운로드
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// URL에서 Texture2D 이미지를 다운로드합니다. (2D 평면도 등)
    /// callback(texture, errorMessage)
    /// </summary>
    public IEnumerator DownloadTextureAsync(string url, Action<Texture2D, string> callback)
    {
        using (var req = UnityWebRequestTexture.GetTexture(url))
        {
            req.timeout = downloadTimeoutSeconds;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                callback(null, $"이미지 다운로드 실패: {req.error}");
            else
                callback(DownloadHandlerTexture.GetContent(req), null);
        }
    }

    // ── 공통 JSON 헤더 설정 ──────────────────────────────────────────
    private static void SetJsonHeaders(UnityWebRequest req)
    {
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Accept",       "application/json");
    }
}
