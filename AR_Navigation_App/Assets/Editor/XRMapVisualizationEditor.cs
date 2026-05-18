/*
    파일명: Assets/Editor/XRMapVisualizationEditor.cs
    역할: XRMapVisualization Inspector에 "로컬 PLY 파일 불러오기" 버튼 추가
    사용법:
      1. AR Space > Map > Map Visualization GameObject 선택
      2. Inspector 하단 "로컬 PLY 파일 불러오기" 버튼 클릭
      3. developer.immersal.com 에서 다운로드한 sparse.ply 파일 선택
*/

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Immersal.XR;

[CustomEditor(typeof(XRMapVisualization))]
public class XRMapVisualizationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        XRMapVisualization vis = (XRMapVisualization)target;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("로컬 PLY 파일 불러오기", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "developer.immersal.com 에서 다운로드한 sparse.ply 파일을 선택하세요.\n" +
            "씬 뷰에 포인트 클라우드로 맵이 시각화됩니다.",
            MessageType.Info);

        if (GUILayout.Button("PLY 파일 선택 및 불러오기", GUILayout.Height(30)))
        {
            // 파일 탐색기 열기
            string path = EditorUtility.OpenFilePanel("Immersal Sparse PLY 선택", "", "ply");

            if (!string.IsNullOrEmpty(path))
            {
                vis.LoadPly(path);
                // 씬 변경사항 기록 (Undo 지원)
                EditorUtility.SetDirty(vis);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(vis.gameObject.scene);
                Debug.Log($"[XRMapVisualization] PLY 로드 완료: {path}");
            }
        }

        if (vis.IsVisualized)
        {
            EditorGUILayout.Space(4);
            // 초록색 배경으로 로드 완료 표시
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.5f, 1f, 0.6f);
            EditorGUILayout.HelpBox("포인트 클라우드가 로드되어 있습니다.", MessageType.Info);
            GUI.backgroundColor = prev;

            if (GUILayout.Button("시각화 제거"))
            {
                vis.ClearVisualization();
                EditorUtility.SetDirty(vis);
            }
        }
    }
}
#endif
