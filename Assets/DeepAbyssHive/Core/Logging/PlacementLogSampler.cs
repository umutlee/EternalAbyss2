using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepAbyssHive.Core.Logging
{
    /// <summary>
    /// 全域 Placement 日誌節流器。
    /// - 僅節流包含關鍵詞 (PLACEMENT / Placement / Placer) 的訊息。
    /// - 以訊息前 96 字為 key 進行時間窗抑制。
    /// </summary>
    internal static class PlacementLogSampler
    {
        private static ILogHandler _original;
        private static SamplingLogHandler _handler;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            _original = Debug.unityLogger.logHandler;
            _handler = new SamplingLogHandler(_original);
            Debug.unityLogger.logHandler = _handler;

            // 配置總覽
            var (enabled, intervalMs) = GetConfig();
            DLog("CONFIG", $"PlacementLogSampler: enabled={enabled}; intervalMs={intervalMs}");
        }

        private static (bool enabled, int intervalMs) GetConfig()
        {
            bool verbose = false;
            int interval = 250; // default
            try
            {
                var cfg = Resources.Load<ScriptableObject>("Configs/GameConfig");
                if (cfg != null)
                {
                    var t = cfg.GetType();
                    var verboseF = t.GetField("verboseLogs");
                    if (verboseF != null) verbose = Convert.ToBoolean(verboseF.GetValue(cfg));
                    var intF = t.GetField("placementTraceIntervalMs") ?? t.GetField("placementLogIntervalMs");
                    if (intF != null) interval = Mathf.Max(0, Convert.ToInt32(intF.GetValue(cfg)));
                }
            }
            catch { /* ignore */ }

            bool enabled = !verbose && interval > 0;
            if (_handler != null)
            {
                _handler.Enabled = enabled;
                _handler.IntervalMs = interval;
            }
            return (enabled, interval);
        }

        /// <summary>結構化日誌橋接。若無 SmartConsole.DLog，走 Debug.Log。</summary>
        private static void DLog(string category, string message)
        {
            try
            {
                var sc = Type.GetType("SmartConsole"); // 若有全域靜態類可直接使用
                var m = sc?.GetMethod("DLog", System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);
                if (m != null) { m.Invoke(null, new object[]{category, message}); return; }
            } catch { /* fallback */ }
            Debug.Log($"[{category}] {message}");
        }

        private class SamplingLogHandler : ILogHandler
        {
            private readonly ILogHandler _inner;
            private readonly Dictionary<string, float> _last = new Dictionary<string, float>(256);
            public bool Enabled = true;
            public int IntervalMs = 250;

            public SamplingLogHandler(ILogHandler inner) { _inner = inner; }

            public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
            {
                string msg = args == null || args.Length == 0 ? format : string.Format(format, args);
                if (ShouldSuppress(msg)) return;
                _inner.LogFormat(logType, context, format, args);
            }

            public void LogException(Exception exception, UnityEngine.Object context)
            {
                _inner.LogException(exception, context);
            }

            private bool ShouldSuppress(string msg)
            {
                if (!Enabled) return false;
                if (string.IsNullOrEmpty(msg)) return false;
                // 關鍵詞判斷
                if (!(msg.Contains("PLACEMENT") || msg.Contains("Placement") || msg.Contains("Placer"))) return false;
                // 取前 96 字作為 key
                string key = msg.Length <= 96 ? msg : msg.Substring(0, 96);
                float now = UnityEngine.Time.realtimeSinceStartup;
                if (_last.TryGetValue(key, out var t0))
                {
                    if ((now - t0) * 1000f < IntervalMs) return true; // 抑制
                    _last[key] = now;
                    return false;
                }
                _last[key] = now;
                return false;
            }
        }
    }
}