using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Core.Lifecycle
{
    /// <summary>
    /// Lifecycle 輔助 API：提供協程啟動/停止的統一入口，並可擴充記錄。
    /// 現階段僅做輕量封裝，避免直接使用 host.StartCoroutine/StopAllCoroutines 分散。
    /// </summary>
    public static class LifecyclePolicy
    {
        // 簡單的追蹤表（按需要擴充）；目前 StartTrackedCoroutine 僅委派給 Unity 原生 API。
        private static readonly Dictionary<int, List<Coroutine>> _tracked = new Dictionary<int, List<Coroutine>>(512);

        /// <summary>以統一策略啟動協程（目前等同原生；保留追蹤掛點供未來擴充）。</summary>
        public static Coroutine StartTrackedCoroutine(this MonoBehaviour host, IEnumerator routine, string tag = null)
        {
            if (host == null || routine == null) return null;
            var c = host.StartCoroutine(routine);
            try
            {
                var id = host.GetInstanceID();
                if (!_tracked.TryGetValue(id, out var list)) { list = new List<Coroutine>(2); _tracked[id] = list; }
                list.Add(c);
            }
            catch { /* 忽略追蹤失敗 */ }
            return c;
        }

        /// <summary>停止該宿主的所有協程（及清理追蹤表）。</summary>
        public static void StopTrackedCoroutines(this MonoBehaviour host)
        {
            if (host == null) return;
            try { host.StopAllCoroutines(); }
            catch { /* 不拋出 */ }
            try { _tracked.Remove(host.GetInstanceID()); }
            catch { /* 忽略 */ }
            DAHLog.Debug(LogCategory.COMMON, $"StopTrackedCoroutines host={FormatHost(host)}");
        }

        private static string FormatHost(MonoBehaviour host)
        {
            return host == null ? "<null>" : $"{host.GetType().Name}@{host.gameObject.name}";
        }
    }
}