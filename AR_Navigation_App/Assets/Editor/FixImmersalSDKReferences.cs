// 일회성 유틸리티: ImmersalSDK의 SceneUpdater/TrackingAnalyzer 참조를 자동 연결
// 실행: Unity 메뉴 → AR Navigation → Fix ImmersalSDK References
// 실행 후 이 파일은 삭제해도 됩니다.
using UnityEditor;
using UnityEngine;
using Immersal;
using Immersal.XR;

public static class FixImmersalSDKReferences
{
    [MenuItem("AR Navigation/Fix ImmersalSDK References")]
    public static void Fix()
    {
        // ImmersalSDK 오브젝트 찾기
        ImmersalSDK sdk = Object.FindObjectOfType<ImmersalSDK>();
        if (sdk == null)
        {
            Debug.LogError("[Fix] ImmersalSDK 컴포넌트를 씬에서 찾을 수 없습니다.");
            return;
        }

        // SceneUpdater, TrackingAnalyzer 찾기 (씬 전체에서)
        SceneUpdater sceneUpdater = Object.FindObjectOfType<SceneUpdater>();
        TrackingAnalyzer trackingAnalyzer = Object.FindObjectOfType<TrackingAnalyzer>();

        if (sceneUpdater == null)
        {
            Debug.LogError("[Fix] SceneUpdater 컴포넌트를 씬에서 찾을 수 없습니다.");
            return;
        }
        if (trackingAnalyzer == null)
        {
            Debug.LogError("[Fix] TrackingAnalyzer 컴포넌트를 씬에서 찾을 수 없습니다.");
            return;
        }

        // SerializedObject로 private 필드에 접근해서 참조 설정
        SerializedObject serializedSDK = new SerializedObject(sdk);
        SerializedProperty sceneUpdaterProp = serializedSDK.FindProperty("m_SceneUpdater");
        SerializedProperty trackingAnalyzerProp = serializedSDK.FindProperty("m_TrackingAnalyzer");

        sceneUpdaterProp.objectReferenceValue = sceneUpdater;
        trackingAnalyzerProp.objectReferenceValue = trackingAnalyzer;
        serializedSDK.ApplyModifiedProperties();

        // 씬 더티 마크
        EditorUtility.SetDirty(sdk);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sdk.gameObject.scene);

        Debug.Log($"[Fix] 완료!\n" +
                  $"  SceneUpdater  → {sceneUpdater.gameObject.name}\n" +
                  $"  TrackingAnalyzer → {trackingAnalyzer.gameObject.name}\n" +
                  $"씬을 저장해주세요 (Ctrl+S).");
    }
}
