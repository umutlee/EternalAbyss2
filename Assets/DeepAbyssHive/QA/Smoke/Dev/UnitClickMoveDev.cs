using UnityEngine;
using System.Collections;
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
        StartCoroutine(BatchSetDestination(agents, hit.point));
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

    /// <summary>
    /// [EA-M4-T11] 分批派發目標，避免大量單位同時派發造成卡頓。
    /// </summary>
    private System.Collections.IEnumerator BatchSetDestination(UnitAgent[] units, Vector3 target)
    {
        var cfg = GameConfigProvider.Current;
        int batchSize = (cfg != null && cfg.batchTargetDispatchSize > 0) ? cfg.batchTargetDispatchSize : 32;
        float interval = (cfg != null && cfg.batchTargetDispatchInterval > 0f) ? cfg.batchTargetDispatchInterval : 0.02f;

        int totalUnits = units.Length;
        int processed = 0;
        
        Debug.Log($"[DEV] Units: batch target dispatch started - {totalUnits} units, batchSize={batchSize}, interval={interval:0.###}s");

        for (int i = 0; i < totalUnits; i += batchSize)
        {
            int endIdx = Mathf.Min(i + batchSize, totalUnits);
            int batchCount = 0;
            int batchNum = (i / batchSize) + 1;
            int totalBatches = Mathf.CeilToInt((float)totalUnits / batchSize);
            
            // 處理當前批次
            for (int j = i; j < endIdx; j++)
            {
                if (units[j] != null) // 防止單位被銷毀
                {
                    units[j].SetDestination(target);
                    batchCount++;
                }
            }
            
            processed += batchCount;
            Debug.Log($"[DEV] Units: batch {batchNum}/{totalBatches} processed {batchCount} units (total: {processed}/{totalUnits})");
            
            // 如果不是最後一批，等待間隔
            if (endIdx < totalUnits)
            {
                yield return new WaitForSeconds(interval);
            }
        }
        
        Debug.Log($"[DEV] Units: batch target dispatch completed - {processed}/{totalUnits} units at {target}");
    }
}