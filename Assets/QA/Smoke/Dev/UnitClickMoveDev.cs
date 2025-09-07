using UnityEngine;
using System.Linq;
using DeepAbyssHive.Units.Agents;

/// <summary>
/// DEV ONLY：在 Play 中，滑鼠右鍵指派所有 UnitAgent 前往命中點。
/// 將本腳本掛在場景任意物件（推薦 "Managers"）即可。
/// </summary>
public class UnitClickMoveDev : MonoBehaviour
{
    void Update()
    {
        if (!Input.GetMouseButtonDown(1)) return; // 右鍵
        var cam = Camera.main; if (!cam) return;
        var ray = cam.ScreenPointToRay(Input.mousePosition);

        int terrain = LayerMask.NameToLayer("Terrain");
        int mask = (terrain >= 0) ? (1 << terrain) : ~0; // 無 Terrain 層時退回全遮罩
        if (Physics.Raycast(ray, out var hit, 5000f, mask, QueryTriggerInteraction.Ignore))
        {
            var agents = FindObjectsOfType<UnitAgent>();
            foreach (var a in agents)
            {
                a.SetDestination(hit.point);
            }
            Debug.Log($"[DEV] Units: targets set = {agents.Length} at {hit.point}");
        }
    }
}