using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Units.Agents;

/// <summary>
/// DEV ONLY：F9 生成單位、F10 指派所有單位前往滑鼠命中點。
/// 掛到場景任意物件（建議掛在 "Managers"）即可。
/// </summary>
public class UnitDevSpawner : MonoBehaviour
{
    [Header("Prefab / Fallback")]
    [Tooltip("測試用單位 Prefab（需含 UnitAgent）。未指定時會即時建立 Capsule+UnitAgent 作為後備。")]
    public GameObject unitPrefab;

    [Tooltip("當 GameConfig.devSpawnCount <= 0 時的後備生成數量。")]
    public int fallbackSpawnCount = 50;

    [Tooltip("生成時的隨機散佈半徑。")]
    public float spawnScatterRadius = 2f;

    // [EA-M4-T04|2025-09-10] 主/備用鍵與後備落點；不動 GameConfig 也能用
    public KeyCode spawnKeyFallback = KeyCode.F9;   // 主鍵（GameConfig 為 None 才用）
    public KeyCode spawnKeyAltFallback = KeyCode.F6; // 備用鍵：避免某些系統攔截 F9
    public KeyCode testKeyFallback = KeyCode.F10;
    [Tooltip("滑鼠 Raycast 未命中時，改以螢幕中心對 y=0 平面落點生成。")]
    public bool fallbackToPlaneY0 = true;

    void OnEnable()
    {
        // 啟用時列印實際採用的鍵，便於驗收（在 Console 找這行）
        var cfg = GameConfigProvider.Current;
        var kSpawn = (cfg && cfg.devUnitsSpawnKey != KeyCode.None) ? cfg.devUnitsSpawnKey : spawnKeyFallback;
        var kAlt   = spawnKeyAltFallback;
        var kTest  = (cfg && cfg.devUnitsTestKey  != KeyCode.None) ? cfg.devUnitsTestKey  : testKeyFallback;
        Debug.Log($"[DEV] UnitDevSpawner: spawnKey={kSpawn} alt={kAlt} testKey={kTest} scatter={spawnScatterRadius}");
    }

    void Update()
    {
        var cfg = GameConfigProvider.Current;

        // 讀取集中熱鍵；None 退回 Inspector 後備
        var spawnKey = (cfg && cfg.devUnitsSpawnKey != KeyCode.None) ? cfg.devUnitsSpawnKey : spawnKeyFallback;
        var testKey  = (cfg != null && cfg.devUnitsTestKey  != KeyCode.None) ? cfg.devUnitsTestKey  : testKeyFallback;
        var altKey   = spawnKeyAltFallback; // 不依賴 GameConfig，確保總有一顆可用

        // 同時接受主鍵或備用鍵
        if ((spawnKey != KeyCode.None && Input.GetKeyDown(spawnKey)) ||
            (altKey   != KeyCode.None && Input.GetKeyDown(altKey)))
        {
            if (TryRayToTerrain(out var hit))
            {
                int count = (cfg != null && cfg.devSpawnCount > 0) ? cfg.devSpawnCount : Mathf.Max(1, fallbackSpawnCount);
                SpawnUnitsAt(hit.point, count);
                Debug.Log($"[DEV] Units: spawn key → {count} @ Terrain hit {hit.point}");
            }
            else if (fallbackToPlaneY0 && TryRayToPlaneY0(out var p))
            {
                int count = (cfg != null && cfg.devSpawnCount > 0) ? cfg.devSpawnCount : Mathf.Max(1, fallbackSpawnCount);
                SpawnUnitsAt(p, count);
                Debug.LogWarning($"[DEV] Units: Terrain ray MISS; fallback y=0 → {count} @ {p}");
            }
            else
            {
                Debug.LogWarning("[DEV] Units: spawn key pressed but Raycast missed (no fallback). 檢查地表 Collider/Layers。");
            }
        }

        if (testKey != KeyCode.None && Input.GetKeyDown(testKey))
        {
            if (TryRayToTerrain(out var hit))
            {
                var all = FindObjectsOfType<UnitAgent>();
                StartCoroutine(BatchSetDestination(all, hit.point));
            }
        }
    }

    // 優先 Terrain 層；若沒有該層，退回全遮罩，容錯更高
    private bool TryRayToTerrain(out RaycastHit hit)
    {
        var cam = Camera.main; 
        if (!cam) { hit = default; return false; }
        var ray = cam.ScreenPointToRay(Input.mousePosition);

        int terrain = LayerMask.NameToLayer("Terrain");
        int mask = (terrain >= 0) ? (1 << terrain) : ~0; // 無 Terrain 層時退回全遮罩
        return Physics.Raycast(ray, out hit, 5000f, mask, QueryTriggerInteraction.Ignore);
    }

    // 滑鼠沒有命中任何 Collider 時，取螢幕中心對 y=0 平面作為後備落點（常見於純平地測試場）
    private bool TryRayToPlaneY0(out Vector3 point)
    {
        var cam = Camera.main;
        if (!cam) { point = default; return false; }
        var ray = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0));
        var plane = new Plane(Vector3.up, Vector3.zero); // y=0
        if (plane.Raycast(ray, out float enter)) { point = ray.GetPoint(enter); return true; }
        point = default; return false;
    }

    private void SpawnUnitsAt(Vector3 center, int count)
    {
        int spawned = 0;
        for (int i = 0; i < count; i++)
        {
            Vector2 rnd = Random.insideUnitCircle * spawnScatterRadius;
            Vector3 pos = new Vector3(center.x + rnd.x, center.y + 0.5f, center.z + rnd.y);
            var go = InstantiateSafe(pos, Quaternion.identity);
            if (go != null) spawned++;
        }
        Debug.Log($"[DEV] Units: spawned = {spawned} at {center} (scatter={spawnScatterRadius})");
    }

    private GameObject InstantiateSafe(Vector3 pos, Quaternion rot)
    {
        GameObject prefab = unitPrefab;
        if (prefab == null)
        {
            // 後備：即時建立 Capsule+UnitAgent
            prefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            prefab.name = "UnitDev(Runtime)";
            if (!prefab.GetComponent<UnitAgent>()) prefab.AddComponent<UnitAgent>();
            var created = Instantiate(prefab, pos, rot);
            Destroy(prefab); // 刪除臨時模板
            return created;
        }
        return Instantiate(prefab, pos, rot);
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