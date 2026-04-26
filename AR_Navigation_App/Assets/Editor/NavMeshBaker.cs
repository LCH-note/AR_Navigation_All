/*
    파일명: Assets/Editor/NavMeshBaker.cs
    역할: NavMeshOrigin의 NavMeshSurface를 메뉴에서 한 번에 베이킹하는 에디터 유틸리티
          + Immersal sparse.ply 좌표 범위 분석 → NavFloor 권장 크기 계산
*/

#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Unity.AI.Navigation;

public static class NavMeshBaker
{
    [MenuItem("AR Navigation/Bake NavMesh")]
    public static void BakeNavMesh()
    {
        var origin = GameObject.Find("NavMeshOrigin");
        if (origin == null)
        {
            Debug.LogError("[NavMeshBaker] 'NavMeshOrigin' GameObject를 씬에서 찾을 수 없습니다.");
            return;
        }

        var surface = origin.GetComponent<NavMeshSurface>();
        if (surface == null)
        {
            Debug.LogError("[NavMeshBaker] NavMeshOrigin에 NavMeshSurface 컴포넌트가 없습니다.");
            return;
        }

        surface.RemoveData();
        surface.BuildNavMesh();

        EditorUtility.SetDirty(surface);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(surface.gameObject.scene);

        Debug.Log("[NavMeshBaker] NavMesh 베이킹 완료!");
    }

    // ── Immersal sparse.ply 좌표 범위 분석 ────────────────────────────────
    [MenuItem("AR Navigation/Analyze PLY Bounds (NavFloor 크기 계산)")]
    public static void AnalyzePlyBounds()
    {
        string path = EditorUtility.OpenFilePanel("Immersal sparse.ply 선택", "Assets/Maps", "ply");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            Vector3 min, max;
            int count = ParsePlyBounds(path, out min, out max);

            if (count == 0)
            {
                Debug.LogError("[NavMeshBaker] PLY에서 버텍스를 읽을 수 없습니다.");
                return;
            }

            Vector3 size   = max - min;
            Vector3 center = (min + max) * 0.5f;

            // NavFloor는 Unity Plane — scale(1,1,1)=10m×10m이므로 목표 크기/10 = 필요 스케일
            // 여유 마진 20% 추가
            float margin    = 1.2f;
            float scaleX    = Mathf.Ceil((size.x * margin) / 10f * 10f) / 10f; // 소수점 1자리 반올림
            float scaleZ    = Mathf.Ceil((size.z * margin) / 10f * 10f) / 10f;
            float maxScale  = Mathf.Max(scaleX, scaleZ, 0.5f);

            string msg =
                $"[NavMeshBaker] PLY 분석 결과 ({count:N0}개 포인트)\n" +
                $"  X 범위: {min.x:F2} ~ {max.x:F2}  (너비 {size.x:F2}m)\n" +
                $"  Y 범위: {min.y:F2} ~ {max.y:F2}  (높이 {size.y:F2}m)\n" +
                $"  Z 범위: {min.z:F2} ~ {max.z:F2}  (깊이 {size.z:F2}m)\n" +
                $"  중심점: ({center.x:F2}, {center.y:F2}, {center.z:F2})\n" +
                $"\n권장 NavFloor Scale X={scaleX:F1}, Z={scaleZ:F1}  (정사각형 기준: {maxScale:F1})\n" +
                $"NavMeshOrigin 위치를 중심점({center.x:F2}, 0, {center.z:F2})으로 이동하거나\n" +
                $"NavFloor Scale을 위 값으로 수정 후 'AR Navigation > Bake NavMesh'를 실행하세요.";

            Debug.Log(msg);
            EditorUtility.DisplayDialog("PLY 분석 완료", msg, "확인");

            // NavFloor 자동 조정 여부 묻기
            if (EditorUtility.DisplayDialog(
                    "NavFloor 자동 조정",
                    $"NavFloor를 분석된 크기로 자동 조정할까요?\n" +
                    $"Scale X={scaleX:F1}, Z={scaleZ:F1}\n" +
                    $"NavMeshOrigin 위치 → ({center.x:F2}, 0, {center.z:F2})",
                    "예, 조정 후 재베이킹", "아니오"))
            {
                ApplyNavFloorScale(scaleX, scaleZ, center);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[NavMeshBaker] PLY 파싱 오류: {e.Message}");
        }
    }

    // 각 property의 바이트 크기를 반환합니다 (타입별 정확한 크기)
    private static int GetPropertyByteSize(string typeName)
    {
        switch (typeName)
        {
            case "char": case "uchar": case "int8": case "uint8": return 1;
            case "short": case "ushort": case "int16": case "uint16": return 2;
            case "int": case "uint": case "int32": case "uint32":
            case "float": case "float32": return 4;
            case "double": case "float64": case "int64": case "uint64": return 8;
            default: return 4;
        }
    }

    /// <summary>
    /// PLY 파일을 파싱해 모든 버텍스의 X,Y,Z 범위를 반환합니다.
    /// ASCII/Binary little-endian/big-endian 및 uchar 등 혼합 property 타입을 올바르게 처리합니다.
    /// </summary>
    private static int ParsePlyBounds(string path, out Vector3 min, out Vector3 max)
    {
        min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        using var fs     = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(fs, Encoding.ASCII);

        // ── 헤더 파싱 ─────────────────────────────────────────────────
        int  vertexCount = 0;
        bool isBinary    = false;
        bool isBigEndian = false;
        int  xIdx = -1, yIdx = -1, zIdx = -1;

        // property별 (이름, 바이트크기, float여부) 목록
        var propNames = new System.Collections.Generic.List<string>();
        var propSizes = new System.Collections.Generic.List<int>();
        var propIsFloat = new System.Collections.Generic.List<bool>();

        string line;
        bool inVertexElement = false;
        while ((line = ReadLine(reader)) != null)
        {
            line = line.Trim();
            if (line.StartsWith("element vertex"))
            {
                vertexCount    = int.Parse(line.Split(' ')[2]);
                inVertexElement = true;
            }
            else if (line.StartsWith("element") && !line.StartsWith("element vertex"))
                inVertexElement = false; // face 등 다른 element 시작
            else if (line == "format ascii 1.0")
                isBinary = false;
            else if (line == "format binary_little_endian 1.0")
                { isBinary = true; isBigEndian = false; }
            else if (line == "format binary_big_endian 1.0")
                { isBinary = true; isBigEndian = true; }
            else if (inVertexElement && line.StartsWith("property ") && !line.StartsWith("property list"))
            {
                // "property <type> <name>" 파싱
                var parts    = line.Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    string typeName = parts[1];
                    string propName = parts[2];
                    int    byteSize = GetPropertyByteSize(typeName);
                    bool   isFloat  = (typeName == "float" || typeName == "float32"
                                       || typeName == "double" || typeName == "float64");
                    int    idx      = propNames.Count;
                    if (propName == "x") xIdx = idx;
                    else if (propName == "y") yIdx = idx;
                    else if (propName == "z") zIdx = idx;
                    propNames.Add(propName);
                    propSizes.Add(byteSize);
                    propIsFloat.Add(isFloat);
                }
            }
            else if (line == "end_header") break;
        }

        if (vertexCount == 0 || xIdx < 0 || yIdx < 0 || zIdx < 0)
            return 0;

        // 버텍스 1개당 총 바이트 수 계산
        int vertexByteSize = 0;
        for (int i = 0; i < propSizes.Count; i++) vertexByteSize += propSizes[i];

        // x, y, z의 파일 내 바이트 오프셋 계산
        int xOffset = 0, yOffset = 0, zOffset = 0;
        for (int i = 0; i < propSizes.Count; i++)
        {
            if (i == xIdx) xOffset = GetOffsetBefore(propSizes, i);
            if (i == yIdx) yOffset = GetOffsetBefore(propSizes, i);
            if (i == zIdx) zOffset = GetOffsetBefore(propSizes, i);
        }

        // ── 버텍스 데이터 읽기 ────────────────────────────────────────
        if (isBinary)
        {
            byte[] vbuf = new byte[vertexByteSize];
            for (int i = 0; i < vertexCount; i++)
            {
                int read = reader.Read(vbuf, 0, vertexByteSize);
                if (read < vertexByteSize) break;

                float x = ReadFloat(vbuf, xOffset, isBigEndian);
                float y = ReadFloat(vbuf, yOffset, isBigEndian);
                float z = ReadFloat(vbuf, zOffset, isBigEndian);

                if (!float.IsNaN(x) && !float.IsInfinity(x))
                {
                    if (x < min.x) min.x = x; if (x > max.x) max.x = x;
                }
                if (!float.IsNaN(y) && !float.IsInfinity(y))
                {
                    if (y < min.y) min.y = y; if (y > max.y) max.y = y;
                }
                if (!float.IsNaN(z) && !float.IsInfinity(z))
                {
                    if (z < min.z) min.z = z; if (z > max.z) max.z = z;
                }
            }
        }
        else
        {
            for (int i = 0; i < vertexCount; i++)
            {
                string vLine = ReadLine(reader);
                if (vLine == null) break;
                var parts = vLine.Trim().Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length <= Mathf.Max(xIdx, yIdx, zIdx)) continue;
                float x = float.Parse(parts[xIdx], System.Globalization.CultureInfo.InvariantCulture);
                float y = float.Parse(parts[yIdx], System.Globalization.CultureInfo.InvariantCulture);
                float z = float.Parse(parts[zIdx], System.Globalization.CultureInfo.InvariantCulture);
                if (x < min.x) min.x = x; if (x > max.x) max.x = x;
                if (y < min.y) min.y = y; if (y > max.y) max.y = y;
                if (z < min.z) min.z = z; if (z > max.z) max.z = z;
            }
        }

        return vertexCount;
    }

    // index번째 property 이전까지의 바이트 오프셋 합산
    private static int GetOffsetBefore(System.Collections.Generic.List<int> sizes, int index)
    {
        int offset = 0;
        for (int i = 0; i < index; i++) offset += sizes[i];
        return offset;
    }

    // 버퍼에서 float32를 읽습니다 (big endian 시 바이트 역전)
    private static float ReadFloat(byte[] buf, int offset, bool bigEndian)
    {
        if (bigEndian)
        {
            byte[] tmp = new byte[4] { buf[offset+3], buf[offset+2], buf[offset+1], buf[offset] };
            return BitConverter.ToSingle(tmp, 0);
        }
        return BitConverter.ToSingle(buf, offset);
    }

    // BinaryReader에서 한 줄을 읽습니다 (헤더 파싱용)
    private static string ReadLine(BinaryReader reader)
    {
        var sb = new StringBuilder();
        try
        {
            int b;
            while ((b = reader.BaseStream.ReadByte()) != -1)
            {
                char c = (char)b;
                if (c == '\n') break;
                if (c != '\r') sb.Append(c);
            }
        }
        catch { return null; }
        return sb.Length == 0 && reader.BaseStream.Position == reader.BaseStream.Length ? null : sb.ToString();
    }

    // NavFloor 크기 적용 + NavMeshOrigin 위치 이동 + 재베이킹
    private static void ApplyNavFloorScale(float scaleX, float scaleZ, Vector3 center)
    {
        var origin = GameObject.Find("NavMeshOrigin");
        var floor  = GameObject.Find("NavFloor");
        if (origin == null || floor == null)
        {
            Debug.LogError("[NavMeshBaker] NavMeshOrigin 또는 NavFloor를 찾을 수 없습니다.");
            return;
        }

        Undo.RecordObject(origin.transform, "Adjust NavMeshOrigin");
        Undo.RecordObject(floor.transform,  "Adjust NavFloor Scale");

        // NavMeshOrigin을 맵 중심으로 이동 (Y=0 고정)
        origin.transform.position = new Vector3(center.x, 0f, center.z);

        // NavFloor Scale 조정 (Unity Plane = 10m×10m at scale 1)
        floor.transform.localScale = new Vector3(scaleX, 1f, scaleZ);

        EditorUtility.SetDirty(origin.transform);
        EditorUtility.SetDirty(floor.transform);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(origin.scene);

        // 재베이킹
        BakeNavMesh();

        Debug.Log($"[NavMeshBaker] NavFloor 자동 조정 완료 → Scale({scaleX:F1}, 1, {scaleZ:F1}), " +
                  $"Origin({center.x:F2}, 0, {center.z:F2})");
    }
}
#endif
