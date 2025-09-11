// [EA-M4-T12|2025-09-11] Path jobs 分幀配額：以最小侵入方式平滑算路尖峰。
// 原理：把「呼叫 UnitPathQueue.Enqueue(...)」排入本地佇列，由 Runner 每幀最多啟動 N 筆（GameConfig.pathJobsPerFrame）。
using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Core.Config;

namespace DeepAbyssHive.Units.Pathfinding
{
    /// <summary>
    /// 對外 API：與 UnitPathQueue.Enqueue 相同，但會依配額分幀觸發真正的 Enqueue。
    /// </summary>
    public static class PathJobScheduler
    {
        private struct Job
        {
            public Vector3 from, to;
            public Action<List<Vector3>> cb;
        }

        private static readonly Queue<Job> _q = new Queue<Job>(256);
        private static Runner _runner;

        /// <summary>
        /// 入列一個算路工作；將在未來數幀內依配額啟動真正的 UnitPathQueue.Enqueue。
        /// </summary>
        public static void Enqueue(Vector3 from, Vector3 to, Action<List<Vector3>> onPathReady)
        {
            _q.Enqueue(new Job { from = from, to = to, cb = onPathReady });
            EnsureRunner();
        }

        private static void EnsureRunner()
        {
            if (_runner != null) return;
            var go = new GameObject("~PathJobScheduler");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<Runner>();
        }

        /// <summary>
        /// 實際執行者：每幀處理固定數量工作，避免一次性尖峰。
        /// </summary>
        private class Runner : MonoBehaviour
        {
            void Update()
            {
                // 從 GameConfig 讀配額；<=0 退回預設 8
                var cfg = GameConfigProvider.Current;
                int budget = (cfg != null && cfg.pathJobsPerFrame > 0) ? cfg.pathJobsPerFrame : 8;
                int n = Mathf.Min(budget, _q.Count);
                for (int i = 0; i < n; i++)
                {
                    var job = _q.Dequeue();
                    // 最小侵入：仍用既有 UnitPathQueue 來算，只是把觸發分散到多幀
                    UnitPathQueue.Enqueue(job.from, job.to, job.cb);
                }
            }
        }
    }
}