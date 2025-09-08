using UnityEngine;
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

    // Inspector 後備熱鍵（Config 設為 None 時採用）
    public KeyCode spawnKeyFallback = KeyCode.F9;
    public KeyCode testKeyFallback = KeyCode.F10;

    void Update()
    {
        var cfg = GameConfigProvider.Current;

        // 讀取集中熱鍵；None 退回 Inspector 後備
        var spawnKey = (cfg != null && cfg.devUnitsSpawnKey != KeyCode.None) ? cfg.devUnitsSpawnKey : spawnKeyFallback;
        var testKey  = (cfg != null && cfg.devUnitsTestKey  != KeyCode.None) ? cfg.devUnitsTestKey  : testKeyFallback;

        if (spawnKey != KeyCode.None && Input.GetKeyDown(spawnKey))
        {
            if (TryRayToTerrain(out var hit))
            {
                int count = (cfg != null && cfg.devSpawnCount > 0) ? cfg.devSpawnCount : Mathf.Max(1, fallbackSpawnCount);
                SpawnUnitsAt(hit.point, count);
            }
        }

        if (testKey != KeyCode.None && Input.GetKeyDown(testKey))
        {
            if (TryRayToTerrain(out var hit))
            {
                var all = FindObjectsOfType<UnitAgent>();
                foreach (var a in all) a.SetDestination(hit.point);
                Debug.Log($"[DEV] Units: targets set = {all.Length} at {hit.point}");
            }
        }
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
}