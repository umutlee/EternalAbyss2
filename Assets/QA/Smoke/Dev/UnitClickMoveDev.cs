using UnityEngine;
using DeepAbyssHive.Units.Agents;
using DeepAbyssHive.Core.Config;

/// <summary>
/// DEV ONLY：用 GameConfig.devUnitsTestKey（預設 F10）把所有 UnitAgent 的目的地設為滑鼠命中點。
/// 掛在 Managers 或任何物件上即可。
/// </summary>
public class UnitClickMoveDev : MonoBehaviour
{
    public KeyCode testKeyFallback = KeyCode.F10; // 後備：GameConfig 為 None 時使用

    void Update()
    {
        var cfg = GameConfigProvider.Current;
        var key = (cfg && cfg.devUnitsTestKey != KeyCode.None) ? cfg.devUnitsTestKey : testKeyFallback;
        if (key == KeyCode.None || !Input.GetKeyDown(key)) return;

        if (!TryRayToTerrain(out var hit)) return;

        var agents = FindObjectsOfType<UnitAgent>();
        foreach (var a in agents) a.SetDestination(hit.point);
        Debug.Log($"[DEV] Units: targets set = {agents.Length} at {hit.point}");
    }

    private bool TryRayToTerrain(out RaycastHit hit)
    {
        var cam = Camera.main;   
        if (!cam) { hit = default; return false; }
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        int terrain = LayerMask.NameToLayer("Terrain");
        int mask = (terrain >= 0) ? (1 << terrain) : ~0; // 無 Terrain 層時退回全遮罩
        return Physics.Raycast(ray, out hit, 5000f, mask, QueryTriggerInteraction.Ignore);
    }
}