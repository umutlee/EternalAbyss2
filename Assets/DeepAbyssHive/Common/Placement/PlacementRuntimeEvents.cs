using System;
using UnityEngine;

namespace DeepAbyssHive.Common.Placement
{
    /// <summary>
    /// [EA-M4-T07|2025-09-10] 施工期的輕量事件匯流排：通知「可通行性」區域變動。
    /// 單位可據此做即時 re-path，避免等待輪詢。
    /// </summary>
    public static class PlacementRuntimeEvents
    {
        /// <param name="center">變動的近似中心（世界座標）。</param>
        /// <param name="radius">影響半徑（水平 XZ）。</param>
        public static event Action<Vector3, float> ObstaclesChanged;

        public static void RaiseObstaclesChanged(Vector3 center, float radius)
        {
            ObstaclesChanged?.Invoke(center, Mathf.Max(0f, radius));
        }
    }
}