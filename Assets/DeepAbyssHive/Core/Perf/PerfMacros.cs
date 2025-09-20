using System.Diagnostics;
using UnityEngine.Profiling;

namespace DeepAbyssHive.Core.Perf
{
    /// <summary>在定義 DAH_PERF 或 GameConfig 開啟時，提供自訂取樣點。</summary>
    public static class PerfMacros
    {
        [Conditional("DAH_PERF")]
        public static void Sample(string name, System.Action body)
        {
            var s = CustomSampler.Create(name);
            s.Begin(); try { body?.Invoke(); } finally { s.End(); }
        }
    }
}