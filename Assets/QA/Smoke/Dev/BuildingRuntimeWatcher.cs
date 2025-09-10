using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Common.Placement;

/// <summary>
/// [EA-M4-T07|2025-09-10] DEV：監看 Building 層物件增/刪，並發出 ObstaclesChanged 事件。
/// 最小成本方案：每 interval 掃描一次（僅 Dev 使用，成本可接受）。
/// 掛在 Managers 或 Dev Helper 上即可。
/// </summary>
public class BuildingRuntimeWatcher : MonoBehaviour
{
    [Tooltip("掃描週期（秒）。")]
    public float interval = 0.25f;
    [Tooltip("半徑外擴保險值（世界單位）。")]
    public float padRadius = 0.5f;

    private float _timer;
    private readonly Dictionary<int, (Vector3 center, float radius)> _known = new();

    void Update()
    {
        _timer += Time.unscaledDeltaTime;
        if (_timer < interval) return;
        _timer = 0f;

        int buildingLayer = LayerMask.NameToLayer("Building");
        if (buildingLayer < 0) return;

        // 掃描現存 Building 物件（只取頂層即可；子物件會被 bounds 包含）
        var roots = FindObjectsOfType<Transform>(false);
        var seen = new HashSet<int>();

        foreach (var t in roots)
        {
            if (!t.gameObject.activeInHierarchy) continue;
            if (t.parent != null) continue; // 只看根，避免重複
            CollectIfBuildingTree(t.gameObject, buildingLayer, seen);
        }

        // 新增：raise 新物件
        foreach (var id in seen)
        {
            if (_known.ContainsKey(id)) continue;
            var go = EditorUtilityHelper.TryFindObjectByInstanceID(id);
            var (c, r) = ComputeBounds2D(go);
            r += padRadius;
            _known[id] = (c, r);
            PlacementRuntimeEvents.RaiseObstaclesChanged(c, r);
            Debug.Log($"[DEV] BuildingWatcher: + {go.name} r~{r:0.##}");
        }

        // 刪除：raise 舊物件消失
        var toRemove = new List<int>();
        foreach (var kv in _known)
        {
            if (seen.Contains(kv.Key)) continue;
            PlacementRuntimeEvents.RaiseObstaclesChanged(kv.Value.center, kv.Value.radius);
            Debug.Log($"[DEV] BuildingWatcher: - id={kv.Key} r~{kv.Value.radius:0.##}");
            toRemove.Add(kv.Key);
        }
        foreach (var id in toRemove) _known.Remove(id);
    }

    private void CollectIfBuildingTree(GameObject root, int buildingLayer, HashSet<int> seen)
    {
        // 若整棵樹沒有任何 Building 層子孫，就跳過
        bool hasBuilding = false;
        foreach (var tr in root.GetComponentsInChildren<Transform>(true))
        {
            if (tr.gameObject.layer == buildingLayer)
            {
                hasBuilding = true; break;
            }
        }
        if (!hasBuilding) return;

        // 用根節點代表整棵建築（放置時我們會把整樹改到 Building 層）
        seen.Add(root.GetInstanceID());
    }

    private static (Vector3 c, float r) ComputeBounds2D(GameObject go)
    {
        // 優先 Renderers；沒有就看 Colliders；再沒有就用 transform
        var rends = go.GetComponentsInChildren<Renderer>(true);
        Bounds b;
        if (rends != null && rends.Length > 0)
        {
            b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        }
        else
        {
            var cols = go.GetComponentsInChildren<Collider>(true);
            if (cols != null && cols.Length > 0)
            {
                b = cols[0].bounds;
                for (int i = 1; i < cols.Length; i++) b.Encapsulate(cols[i].bounds);
            }
            else
            {
                b = new Bounds(go.transform.position, Vector3.one);
            }
        }
        float r = Mathf.Max(b.extents.x, b.extents.z);
        return (b.center, r);
    }

    // Editor/Player 兼容的 by-id 查找（避免直接持有引用在卸載時拋例外）
    private static class EditorUtilityHelper
    {
        public static GameObject TryFindObjectByInstanceID(int id)
        {
            // 在 Player 環境，InstanceID/Find 不是公開 API，這裡退回慢一點的遍歷
            foreach (var go in GameObject.FindObjectsOfType<GameObject>())
                if (go.GetInstanceID() == id) return go;
            return null;
        }
    }
}