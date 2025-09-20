using System;
using System.Text;
using UnityEngine;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Core.Telemetry
{
    /// <summary>
    /// 低頻心跳輸出（預設 10s），啟動於 Managers 下，輸出到 Smart Console 的 HEALTH 分類。
    /// 可由 GameConfig 控制開關與間隔。反射抓取 Units/Buildings/Terrain/Creep 的大數據。
    /// </summary>
    public class TelemetryRunner : MonoBehaviour
    {
        private static bool _enabled = true;
        private static float _interval = 10f;
        private float _acc;
        private readonly StringBuilder _sb = new StringBuilder(256);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindObjectOfType<TelemetryRunner>() != null) return;
            var go = new GameObject("Telemetry"); go.AddComponent<TelemetryRunner>();
            var managers = GameObject.Find("Managers"); if (managers != null) go.transform.SetParent(managers.transform);
            DontDestroyOnLoad(go);
            TryLoadFromGameConfig();
            DAHLog.Info(LogCategory.CONFIG, $"Telemetry: enabled={_enabled}, interval={_interval}s");
            DAHLog.Info(LogCategory.SERVICE, "TelemetryRunner created");
        }

        private void Update()
        {
            TelemetryService.RecordFrame(Time.unscaledDeltaTime);
            if (!_enabled) return;
            _acc += Time.unscaledDeltaTime;
            if (_acc >= _interval)
            {
                _acc = 0f;
                EmitHealthLine();
            }
        }

        private void EmitHealthLine()
        {
            TelemetryService.GetFrameStats(out var fpsAvg, out var msP50, out var msP95);
            long memBytes = System.GC.GetTotalMemory(false);
            int memMB = (int)(memBytes / (1024 * 1024));

            // 反射抓取主要數量（抓不到就省略）
            var units = TelemetryService.TryGetStaticOrInstance(new[]{
                "DeepAbyssHive.Units.Managers.UnitManager", "UnitManager"
            }, "Count");
            var buildings = TelemetryService.TryGetStaticOrInstance(new[]{
                "DeepAbyssHive.Buildings.Managers.BuildingManager", "BuildingManager"
            }, "Count");
            var chunks = TelemetryService.TryGetStaticOrInstance(new[]{
                "DeepAbyssHive.Terrain.Managers.TerrainManager", "TerrainManager"
            }, "LoadedChunkCount");

            _sb.Length = 0;
            _sb.Append("fps=").Append(fpsAvg.ToString("0.0"));
            _sb.Append(", frame_ms_p50=").Append(msP50.ToString("0.0"));
            _sb.Append(", p95=").Append(msP95.ToString("0.0"));
            _sb.Append(", memMB=").Append(memMB);
            if (units != null) _sb.Append(", units=").Append(units);
            if (buildings != null) _sb.Append(", buildings=").Append(buildings);
            if (chunks != null) _sb.Append(", chunks=").Append(chunks);

            DAHLog.Info(LogCategory.MANAGER, _sb.ToString());
        }

        private static void TryLoadFromGameConfig()
        {
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var p = asm.GetType("GameConfigProvider") ?? asm.GetType("DeepAbyssHive.Core.Config.GameConfigProvider");
                    if (p == null) continue;
                    var cfg = p.GetProperty("Current", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.GetValue(null);
                    if (cfg == null) continue;
                    var t = cfg.GetType();
                    // healthLoggingEnabled / healthLogInterval (or *Seconds)
                    var en = GetBool(t, cfg, "healthLoggingEnabled", true);
                    var sec = GetFloat(t, cfg, "healthLogInterval", 10f);
                    if (Math.Abs(sec) < 1e-4f) sec = GetFloat(t, cfg, "healthLogIntervalSeconds", 10f);
                    _enabled = en;
                    _interval = Mathf.Clamp(sec, 1f, 120f);
                    break;
                }
            } catch {}
        }

        private static bool GetBool(Type t, object cfg, string name, bool defVal)
        {
            var f = t.GetField(name, System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            if (f != null && f.FieldType == typeof(bool)) return (bool)f.GetValue(cfg);
            var p = t.GetProperty(name, System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            if (p != null && p.PropertyType == typeof(bool)) return (bool)p.GetValue(cfg);
            return defVal;
        }
        private static float GetFloat(Type t, object cfg, string name, float defVal)
        {
            var f = t.GetField(name, System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            if (f != null) return Convert.ToSingle(f.GetValue(cfg));
            var p = t.GetProperty(name, System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            if (p != null) return Convert.ToSingle(p.GetValue(cfg));
            return defVal;
        }
    }
}