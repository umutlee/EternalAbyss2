using System.Runtime.CompilerServices;
using UnityEngine;

namespace DeepAbyssHive.Core.Perf
{
    /// <summary>熱路徑小技巧：GetComponent 快取、NonAlloc Physics。</summary>
    public static class PerfHints
    {
        /// <summary>
        /// 把 GetComponent 結果快存在欄位裡；呼叫點只需 `comp = PerfHints.GetCached(this, ref comp);`
        /// 這是個小內聯，避免每幀反覆 GetComponent/Boxing。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetCached<T>(this Component host, ref T cached) where T : Component
        {
            if (!cached) cached = host.GetComponent<T>();
            return cached;
        }

        /// <summary>NonAlloc 的 Raycast 包裝：呼叫端傳入共用的命中陣列。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RaycastNonAlloc(Vector3 origin, Vector3 dir, RaycastHit[] hits, float dist, int layerMask)
        {
            return Physics.RaycastNonAlloc(new Ray(origin, dir), hits, dist, layerMask);
        }
    }
}