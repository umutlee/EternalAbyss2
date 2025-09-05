using UnityEngine;

/// <summary>
/// Dev 小工具：按下 Delete 或 X，射線選取 Building 層物件並刪除。
/// 不依賴 Placer；可直接掛在任意場景物件上（例如 Managers）。
/// </summary>
public class BuildingDeleteTool : MonoBehaviour
{
    public KeyCode deleteKey1 = KeyCode.Delete;
    public KeyCode deleteKey2 = KeyCode.X;

    void Update()
    {
        if (!Input.GetKeyDown(deleteKey1) && !Input.GetKeyDown(deleteKey2))
            return;

        var cam = Camera.main;
        if (cam == null) return;

        var ray = cam.ScreenPointToRay(Input.mousePosition);
        int buildingLayer = LayerMask.NameToLayer("Building");
        if (buildingLayer < 0) { Debug.Log("[DEV HUD] BuildingDeleteTool: Building 層不存在"); return; }

        int mask = 1 << buildingLayer;
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