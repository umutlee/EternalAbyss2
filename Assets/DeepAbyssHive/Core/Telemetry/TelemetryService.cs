using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DeepAbyssHive.Core.Telemetry
{
    /// <summary>
    /// 輕量級統計與格式化：維護 FPS 時序、近似 P50/P95，並提供 Manager 數據反射查詢。
    /// 避免高頻配置：只提供被 Runner 低頻（預設 10s）呼叫的 API。
    /// </summary>
    public static class TelemetryService
    {
        private const int FrameWindow = 180; // 約3秒 @60fps，用於近似分位
        private static readonly float[] _frameTimes = new float[FrameWindow];
        private static int _count;
        private static int _idx;

        /// <summary>每幀記錄（由 Runner 在 Update 記）。</summary>
        public static void RecordFrame(float unscaledDeltaTime)
        {
            _frameTimes[_idx] = unscaledDeltaTime;
            _idx = (_idx + 1) % FrameWindow;
            if (_count < FrameWindow) _count++;
        }

        public static void GetFrameStats(out float fpsAvg, out float msP50, out float msP95)
        {
            if (_count == 0) { fpsAvg = 0; msP50 = 0; msP95 = 0; return; }
            // 複製有效段並排序（小陣列，成本低；僅在出報時呼叫）
            var arr = new float[_count];
            int j = 0; int start = (_idx - _count + FrameWindow) % FrameWindow;
            for (int i = 0; i < _count; i++) { arr[j++] = _frameTimes[(start + i) % FrameWindow]; }
            Array.Sort(arr); // 升序（秒）
            float median = arr[_count * 50 / 100];
            float p95 = arr[_count * 95 / 100];
            msP50 = median * 1000f;
            msP95 = p95 * 1000f;
            // 平均 FPS 用窗口內的平均 dt 估算
            float sum = 0f; for (int i = 0; i < _count; i++) sum += arr[i];
            float avgDt = sum / _count;
            fpsAvg = avgDt > 1e-6f ? (1f / avgDt) : 0f;
        }

        /// <summary>嘗試以反射從 Manager/服務取值，取不到回 null。</summary>
        public static object TryGetStaticOrInstance(string[] typeNames, string member)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            foreach (var tn in typeNames)
            {
                var t = asm.GetType(tn);
                if (t == null) continue;
                // 靜態欄位/屬性
                var f = t.GetField(member, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);
                if (f != null) return f.GetValue(null);
                var p = t.GetProperty(member, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);
                if (p != null) return p.GetValue(null, null);
                // 例項欄位/屬性
                var inst = UnityEngine.Object.FindObjectOfType(t);
                if (inst != null)
                {
                    var fi = t.GetField(member, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
                    if (fi != null) return fi.GetValue(inst);
                    var pi = t.GetProperty(member, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
                    if (pi != null) return pi.GetValue(inst, null);
                }
            }
            return null;
        }
    }
}