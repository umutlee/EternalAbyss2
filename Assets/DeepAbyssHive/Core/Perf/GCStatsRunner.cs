using System;
using System.Text;
using UnityEngine;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Core.Perf
{
    /// <summary>
    /// 低頻輸出 GC 與幀時間的心跳（預設 10s）。用於建立 v1 基線與驗證 v2 優化是否有效。
    /// </summary>
    public class GCStatsRunner : MonoBehaviour
    {
        private static bool _enabled = true;
        private static float _interval = 10f;
        private static bool _shortStack = true;
        private float _acc;
        private readonly StringBuilder _sb = new StringBuilder(256);
        private long _lastAlloc;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindObjectOfType<GCStatsRunner>() != null) return;
            var go = new GameObject("Perf-GC"); go.AddComponent<GCStatsRunner>();
            var managers = GameObject.Find("Managers"); if (managers != null) go.transform.SetParent(managers.transform);
            GameObject.DontDestroyOnLoad(go);
            TryLoadFromGameConfig();
            DAHLog.Info(LogCategory.CONFIG, $"Perf: tracking={_enabled}, interval={_interval}s, shortStack={_shortStack}");
            DAHLog.Info(LogCategory.BOOT, "GCStatsRunner created");
        }

        private void Update()
        {
            if (!_enabled) return;
            _acc += UnityEngine.Time.unscaledDeltaTime;
            if (_acc < _interval) return;
            _acc = 0f;
            Emit();
        }

        private void Emit()
        {
            // fps 與 frame time 近似
            float fps = 1f / Mathf.Max(1e-6f, UnityEngine.Time.unscaledDeltaTime);
            long total = System.GC.GetTotalMemory(false);
            long allocThread = 0;
            try { allocThread = System.GC.GetAllocatedBytesForCurrentThread(); } catch {}
            long diff = _lastAlloc == 0 ? 0 : Math.Max(0, allocThread - _lastAlloc);
            _lastAlloc = allocThread;

            _sb.Length = 0;
            _sb.Append("fps=").Append(fps.ToString("0.0"));
            _sb.Append(", memMB=").Append((int)(total / (1024 * 1024)));
            _sb.Append(", allocThreadKB=").Append((int)(allocThread / 1024));
            _sb.Append(", allocDeltaKB=").Append((int)(diff / 1024));

            DAHLog.Info(LogCategory.PERF, _sb.ToString());
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
                    _enabled = GetBool(t, cfg, "perfTrackingEnabled", true);
                    _interval = Mathf.Clamp(GetFloat(t, cfg, "perfLogInterval", 10f), 1f, 60f);
                    _shortStack = GetBool(t, cfg, "perfShortStack", true);
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