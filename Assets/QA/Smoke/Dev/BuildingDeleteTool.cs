using UnityEngine;
using DeepAbyssHive.Core.Config;

/// <summary>
/// Dev 小工具：按下 Delete 或 X，射線選取 Building 層物件並刪除。
/// 不依賴 Placer；可直接掛在任意場景物件上（例如 Managers）。
/// </summary>
public class BuildingDeleteTool : MonoBehaviour
{
    // Inspector 後備（GameConfig 設為 None 時使用）
    public KeyCode deleteKey1 = KeyCode.Delete;
    public KeyCode deleteKey2 = KeyCode.X;

    void Update()
    {
        // 優先用 GameConfig；個別 None→各自落回 Inspector 後備；兩者皆 None→停用
        var cfg = GameConfigProvider.Current;
        var k1 = (cfg != null && cfg.buildingDeleteKey1 != KeyCode.None) ? cfg.buildingDeleteKey1 : deleteKey1;
        var k2 = (cfg != null && cfg.buildingDeleteKey2 != KeyCode.None) ? cfg.buildingDeleteKey2 : deleteKey2;
        bool pressed = (k1 != KeyCode.None && Input.GetKeyDown(k1)) || (k2 != KeyCode.None && Input.GetKeyDown(k2));
        if (!pressed) return;

        var cam = Camera.main; if (!cam) return;
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        // 用全遮罩收集命中，再向上找 Building 層祖先；避免子物件 collider 不在 Building 層時打不到
        var hits = Physics.RaycastAll(ray, 5000f, ~0, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return;
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        int buildingLayer = LayerMask.NameToLayer("Building");
        int ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
        foreach (var h in hits)
        {
            var target = FindBuildingRoot(h.collider.gameObject, buildingLayer);
            if (target == null) continue;
            if (ignoreLayer >= 0 && target.layer == ignoreLayer) continue;
            if (target.name.StartsWith("[Preview]")) continue; // 不刪預覽體
            Debug.Log($"[DEV] Delete {target.name}");
            Destroy(target);
            return;
        }
    }

    private static GameObject FindBuildingRoot(GameObject from, int buildingLayer)
    {
        if (buildingLayer < 0) return null;
        var t = from.transform;
        while (t != null)
        {
            if (t.gameObject.layer == buildingLayer) return t.gameObject;
            t = t.parent;
        }
        return null;
    }
}