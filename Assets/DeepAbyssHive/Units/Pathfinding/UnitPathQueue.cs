using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Units.Pathfinding
{
    /// <summary>
    /// 輕量路徑請求佇列：每幀處理上限 N 筆，避免尖峰卡頓。
    /// 透過 GridProvider 提供 IPathGrid；未指定時使用內建 PathGridSampler。
    /// </summary>
    public static class UnitPathQueue
    {
        public static Func<IPathGrid> GridProvider;    // 外部可替換；未設置時用 _defaultGrid
        private static IPathGrid _defaultGrid;

        private struct Req
        {
            public Vector3 s, g;
            public Action<List<Vector3>, bool> cb;
        }

        private static readonly Queue<Req> _q = new Queue<Req>(128);
        internal static void Enqueue(Vector3 start, Vector3 goal, Action<List<Vector3>, bool> cb)
            => _q.Enqueue(new Req { s = start, g = goal, cb = cb });

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            // 準備 Runner（掛到 Managers 物件下；找不到就建立）
            var root = GameObject.Find("Managers") ?? new GameObject("Managers");
            if (root.transform.parent == null) UnityEngine.Object.DontDestroyOnLoad(root);

            var runner = root.GetComponent<UnitPathQueueRunner>() ?? root.AddComponent<UnitPathQueueRunner>();
            runner.hideFlags = HideFlags.DontSave;
        }

        private static IPathGrid EnsureGrid()
        {
            if (GridProvider != null)
            {
                var g = GridProvider();
                if (g != null) return g;
            }
            if (_defaultGrid == null)
            {
                // 內建取樣器：origin=0, cell=1；可走規則沿 PathGridSampler 預設
                _defaultGrid = new PathGridSampler(Vector3.zero, 1f);
            }
            return _defaultGrid;
        }

        /// <summary>內部 Runner：每幀吃掉最多 N 筆請求。</summary>
        private class UnitPathQueueRunner : MonoBehaviour
        {
            [Tooltip("每幀最多處理多少筆求路請求")]
            public int requestsPerFrame = 32;
            [Tooltip("是否允許對角線移動")]
            public bool allowDiagonal = true;
            [Tooltip("展開節點上限（避免最壞情況卡住）")]
            public int maxExpand = 4096;

            void Update()
            {
                int budget = Mathf.Max(1, requestsPerFrame);
                while (budget-- > 0 && _q.Count > 0)
                {
                    var r = _q.Dequeue();
                    var grid = EnsureGrid();
                    bool ok = GridAStar.TryFindPath(grid, r.s, r.g, out var path, allowDiagonal, maxExpand);
                    try { r.cb?.Invoke(path, ok); } catch (Exception e) { DAHLog.Error(LogCategory.UNITS, $"UnitPathQueue callback exception: {e}"); }
                }
            }
        }
    }
}