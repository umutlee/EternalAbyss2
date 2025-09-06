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

        var cam = Camera.main;
        if (cam == null) return;

        var ray = cam.ScreenPointToRay(Input.mousePosition);
        int mask = DeepAbyssHive.Common.Placement.PlacementLayerUtil.GetBuildingOnlyMask();
        if (mask == 0) { Debug.Log("[DEV HUD] BuildingDeleteTool: Building 層不存在"); return; }
        if (Physics.Raycast(ray, out var hit, 1000f, mask, QueryTriggerInteraction.Ignore))
        {
            var go = hit.collider ? hit.collider.gameObject : null;
            if (go != null)
            {
                Debug.Log($"[DEV] Delete {go.name}");
                Destroy(go);
            }
        }
    }
}