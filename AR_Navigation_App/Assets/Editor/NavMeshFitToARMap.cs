/*
    파일명: Assets/Editor/NavMeshFitToARMap.cs
    역할: "Integrated AR Map" 오브젝트의 전체 바운드를 계산하고
          NavMeshOrigin의 NavMeshSurface size와 NavFloor(Plane) 스케일을
          해당 크기에 맞게 재조정한다.
*/
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Unity.AI.Navigation;

public static class NavMeshFitToARMap
{
    [MenuItem("AR Navigation/Fit NavMesh to Integrated AR Map")]
    public static void FitNavMeshToARMap()
    {
        // ── 대상 오브젝트 검색 ────────────────────────────────────────
        var arMap = GameObject.Find("Integrated AR Map");
        if (arMap == null)
        {
            Debug.LogError("[NavMeshFit] 'Integrated AR Map' 오브젝트를 씬에서 찾을 수 없습니다.");
            return;
        }

        var navOrigin = GameObject.Find("NavMeshOrigin");
        if (navOrigin == null)
        {
            Debug.LogError("[NavMeshFit] 'NavMeshOrigin' 오브젝트를 씬에서 찾을 수 없습니다.");
            return;
        }

        var surface = navOrigin.GetComponent<NavMeshSurface>();
        if (surface == null)
        {
            Debug.LogError("[NavMeshFit] NavMeshOrigin에 NavMeshSurface 컴포넌트가 없습니다.");
            return;
        }

        // ── Integrated AR Map 전체 바운드 계산 ────────────────────────
        // Renderer가 없으면 Collider 기준으로 폴백
        Bounds bounds = CalcRendererBounds(arMap);
        if (bounds.size == Vector3.zero)
            bounds = CalcColliderBounds(arMap);

        if (bounds.size == Vector3.zero)
        {
            // 바운드를 구하지 못한 경우 Transform 기반으로 추정
            Debug.LogWarning("[NavMeshFit] Renderer / Collider가 없어 Transform 중심·기본 크기(10m×10m)를 사용합니다.");
            bounds = new Bounds(arMap.transform.position, new Vector3(10f, 2f, 10f));
        }

        Debug.Log($"[NavMeshFit] AR Map 바운드 → 중심 {bounds.center}, 크기 {bounds.size}");

        // ── NavFloor 오브젝트 검색 (NavMeshOrigin 자식 우선, 없으면 씬 전체) ──
        Transform navFloor = null;
        foreach (Transform child in navOrigin.transform)
        {
            if (child.name == "NavFloor") { navFloor = child; break; }
        }
        if (navFloor == null)
        {
            var go = GameObject.Find("NavFloor");
            if (go != null) navFloor = go.transform;
        }

        // ── NavMeshOrigin 위치·NavMeshSurface 크기 적용 ──────────────
        Undo.RecordObject(navOrigin.transform, "Fit NavMeshOrigin to AR Map");
        Undo.RecordObject(surface, "Fit NavMeshSurface to AR Map");
        if (navFloor != null)
            Undo.RecordObject(navFloor, "Fit NavFloor to AR Map");

        // NavMeshOrigin을 AR Map의 XZ 중심(Y=0)으로 이동
        navOrigin.transform.position = new Vector3(bounds.center.x, 0f, bounds.center.z);

        // Children 모드: NavMeshOrigin 자식(NavFloor)의 Collider를 직접 사용
        // Volume 모드는 NavFloor가 박스 경계선에 정확히 걸리면 절반만 베이킹되는 문제가 있음
        surface.collectObjects = CollectObjects.Children;
        surface.center = Vector3.zero;

        // 여유 마진 10% 추가 (NavFloor 스케일 계산에만 사용, size 박스는 Children 모드에서 무시됨)
        float margin = 1.1f;
        float sizeX = bounds.size.x * margin;
        float sizeZ = bounds.size.z * margin;

        // ── NavFloor(Unity Plane) 스케일 조정 ────────────────────────
        // Unity Plane은 scale(1,1,1) 기준 10m×10m이므로 목표 크기/10 = 필요 스케일
        string navFloorLog = "NavFloor: 씬에서 찾을 수 없어 스킵";
        if (navFloor != null)
        {
            navFloor.localScale = new Vector3(sizeX / 10f, 1f, sizeZ / 10f);
            EditorUtility.SetDirty(navFloor);
            navFloorLog = $"NavFloor Scale → ({navFloor.localScale.x:F2}, 1, {navFloor.localScale.z:F2})  " +
                          $"실제 크기 {sizeX:F2} × {sizeZ:F2} m";
        }

        EditorUtility.SetDirty(navOrigin.transform);
        EditorUtility.SetDirty(surface);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(navOrigin.scene);

        Debug.Log($"[NavMeshFit] NavMeshOrigin 위치 → ({navOrigin.transform.position.x:F2}, 0, {navOrigin.transform.position.z:F2})\n" +
                  $"NavMeshSurface 크기 → {surface.size}\n" +
                  $"{navFloorLog}\n" +
                  $"재베이킹하려면 'AR Navigation > Bake NavMesh' 를 실행하세요.");

        EditorUtility.DisplayDialog(
            "NavMesh 크기 조정 완료",
            $"AR Map 바운드:\n  크기 {bounds.size.x:F2} × {bounds.size.z:F2} m\n  중심 ({bounds.center.x:F2}, {bounds.center.z:F2})\n\n" +
            $"NavMeshSurface size → {sizeX:F2} × {sizeZ:F2} m\n" +
            $"{navFloorLog}\n" +
            $"NavMeshOrigin 위치 → ({navOrigin.transform.position.x:F2}, 0, {navOrigin.transform.position.z:F2})\n\n" +
            $"'AR Navigation > Bake NavMesh' 로 재베이킹하세요.",
            "확인");
    }

    // ── Renderer 바운드 통합 계산 ─────────────────────────────────────
    private static Bounds CalcRendererBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds();

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);
        return b;
    }

    // ── Collider 바운드 통합 계산 (Renderer 없을 때 폴백) ─────────────
    private static Bounds CalcColliderBounds(GameObject root)
    {
        var colliders = root.GetComponentsInChildren<Collider>(true);
        if (colliders.Length == 0) return new Bounds();

        Bounds b = colliders[0].bounds;
        for (int i = 1; i < colliders.Length; i++)
            b.Encapsulate(colliders[i].bounds);
        return b;
    }
}
#endif
