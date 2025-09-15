using UnityEngine;
using UnityEngine.Profiling; // 取記憶體
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Units.Agents;

/// <summary>
/// [EA-M4-T08|2025-09-11] Runtime 健康監測：每段時間輸出 FPS 平均、記憶體、單位數、建築數。
/// - 掛在 Managers 或 Dev Helper 上即可；不需其他依賴。
/// - 讀 GameConfig：healthLogEnabled / healthLogInterval。
/// - 低開銷：僅取樣必要數據；不建立垃圾物件。
/// </summary>
public class RuntimeHealthLogger : MonoBehaviour
{
    private int _frame0;
    private float _t0;
    private float _timer;

    void OnEnable()
    {
        _frame0 = Time.frameCount;
        _t0 = Time.realtimeSinceStartup;
        _timer = 0f;
    }

    void Update()
    {
        var cfg = GameConfigProvider.Current;
        if (cfg == null || !cfg.healthLogEnabled) return;

        float interval = (cfg.healthLogInterval > 0f) ? cfg.healthLogInterval : 10f;
        _timer += Time.unscaledDeltaTime;
        if (_timer < interval) return;
        _timer = 0f;

        // FPS 平均（以上一個輸出點為基準）
        int f1 = Time.frameCount;
        float t1 = Time.realtimeSinceStartup;
        float dt = Mathf.Max(0.0001f, t1 - _t0);
        float fps = (f1 - _frame0) / dt;
        _frame0 = f1; _t0 = t1;

        // 記憶體（MB）：包含已配置總量，比起 GC 使用更直觀觀察增長
        long bytes = Profiler.GetTotalAllocatedMemoryLong();
        float mb = bytes / (1024f * 1024f);

        // 單位與建築統計（快速估算）
        int units = FindObjectsOfType<UnitAgent>().Length;
        int buildingLayer = LayerMask.NameToLayer("Building");
        int buildings = 0;
        if (buildingLayer >= 0)
        {
            var all = FindObjectsOfType<GameObject>();
            for (int i = 0; i < all.Length; i++)
                if (all[i].layer == buildingLayer && all[i].activeInHierarchy) buildings++;
        }

        Debug.Log($"[HEALTH] fpsAvg={fps:0.0} mem={mb:0.0}MB units={units} buildings={buildings}");
    }
}