using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Units.Pathfinding;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Common.Placement; // +事件：ObstaclesChanged

namespace DeepAbyssHive.Units.Agents
{
    /// <summary>
    /// 最小單位代理：呼叫 SetDestination() 觸發 A*，收到路徑即沿線移動。
    /// </summary>
    [DisallowMultipleComponent]
    public class UnitAgent : MonoBehaviour
    {
        [Header("Move")]
        public float moveSpeed = 3.5f;
        public float rotateSpeed = 540f;           // deg/sec
        public float stoppingDistance = 0.15f;

        private List<Vector3> _path;
        private int _idx;
        // [EA-M4-T05|2025-09-09] 單位×菌毯取樣（已有）
        // --- Creep sampling ---
        public static System.Func<Vector3, bool> OnCreepPredicate; // 由外部（Creep 系統）指派，未指派=不啟用
        private bool _isOnCreep;
        private float _speedFactor = 1f;
        private float _creepSampleTimer;

        // [EA-M4-T06|2025-09-10] 動態障礙守門：定期檢測前方是否被 Building 層阻擋，必要時 re-path
        [Header("Dynamic Obstacle Guard")]
        [Tooltip("檢測頻率（秒）。建議 0.3~1.0。")]
        public float dynamicCheckInterval = 0.5f;
        [Tooltip("連續 re-path 的冷卻（秒），避免抖動。")]
        public float dynamicRepathCooldown = 1.0f;
        [Tooltip("SphereCast 半徑（世界單位）。大於單位半徑少許即可。")]
        public float obstacleProbeRadius = 0.35f;
        [Tooltip("探測距離額外裕度（避免貼邊漏檢）。")]
        public float obstacleProbeExtra = 0.5f;
        private float _dynCheckTimer;
        private float _lastRepathAt;
        // [EA-M4-T10|2025-09-11] DEV 日誌節流
        private float _lastVerboseLogAt;

        private const float _eventInflate = 0.5f; // 事件影響半徑的小擴張，避免邊界漏檢

        /// <summary>
        /// 僅在 GameConfig.devVerboseLogs=true 時輸出，且每 1 秒最多一次，避免 Editor 下過量日誌造成卡頓。
        /// </summary>
        private void DevLog(string message)
        {
            var cfg = GameConfigProvider.Current;
            if (cfg == null || !cfg.devVerboseLogs) return;
            float now = Time.unscaledTime;
            if (now - _lastVerboseLogAt < 1f) return;
            _lastVerboseLogAt = now;
            Debug.Log(message);
        }

        void OnEnable()
        {
            // 事件：建築增刪 → 立即嘗試局部 re-path（搭配 T06 冷卻避免抖動）
            PlacementRuntimeEvents.ObstaclesChanged += OnObstaclesChanged;
        }

        void OnDisable()
        {
            PlacementRuntimeEvents.ObstaclesChanged -= OnObstaclesChanged;
        }

        public void SetDestination(Vector3 worldTarget)
        {
            var start = transform.position;
            // 經 PathJobScheduler 以每幀配額分散啟動算路
            PathJobScheduler.Enqueue(start, worldTarget, OnPath);
        }

        private void OnPath(List<Vector3> path, bool ok)
        {
            if (!ok || path == null || path.Count == 0)
            {
                _path = null; _idx = 0;
                return;
            }
            _path = path; _idx = 0;
            // 將首點替換為當前位置（避免瞬移）
            _path[0] = transform.position;
        }

        void Update()
        {
            SampleCreepIfDue();
            DynamicObstacleGuard(); // 先守門（如需 re-path），再走路

            if (_path == null || _idx >= _path.Count) return;

            var p = _path[_idx];
            var to = p - transform.position; to.y = 0f;
            // 到站切下一點
            if (to.sqrMagnitude <= stoppingDistance * stoppingDistance)
            {
                _idx++;
                if (_idx >= _path.Count) { _path = null; return; }
                return;
            }

            // 旋轉面向
            if (to.sqrMagnitude > 1e-6f)
            {
                var targetRot = Quaternion.LookRotation(to.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
            }
            // 前進
            transform.position += transform.forward * (moveSpeed * _speedFactor * Time.deltaTime);
        }

        /// <summary>
        /// [M4-T06] 週期性以 SphereCast 沿「當前→下一 waypoint」檢測 Building 層。
        /// 命中則對「當前→最終目標」重新排路。避免新建築/刪除造成的卡路。
        /// </summary>
        private void DynamicObstacleGuard()
        {
            // 讀 GameConfig（有值則覆蓋 Inspector；否則用 Inspector 值）
            var cfg = GameConfigProvider.Current;
            float cfgInterval = (cfg && cfg.unitDynCheckInterval > 0f) ? cfg.unitDynCheckInterval : dynamicCheckInterval;
            float cfgCooldown = (cfg && cfg.unitDynRepathCooldown > 0f) ? cfg.unitDynRepathCooldown : dynamicRepathCooldown;
            float probeR      = (cfg && cfg.unitObstacleProbeRadius > 0f) ? cfg.unitObstacleProbeRadius : obstacleProbeRadius;
            float probeExtra  = (cfg && cfg.unitObstacleProbeExtra >= 0f) ? cfg.unitObstacleProbeExtra : obstacleProbeExtra;

            _dynCheckTimer += Time.deltaTime;
            if (_dynCheckTimer < cfgInterval) return;
            _dynCheckTimer = 0f;

            if (_path == null || _idx >= _path.Count) return;
            if (Time.time < _lastRepathAt + cfgCooldown) return;

            // 取得下一個導航點（或末點）；計算水平方向
            Vector3 from = transform.position;
            int nextIdx = Mathf.Min(_idx + 1, _path.Count - 1);
            Vector3 to = _path[nextIdx];
            Vector3 dir = to - from; dir.y = 0f;
            float dist = dir.magnitude;
            if (dist < 0.2f) return; // 太近不檢
            dir /= dist;

            // 僅檢 Building 層；若專案未建層，GetBuildingOnlyMask 會回 0 → 直接跳過
            int mask = PlacementLayerUtil.GetBuildingOnlyMask();
            if (mask == 0) return;

            // 從胸口高度探測，避免地面誤檢；給一點超出距離，避免貼邊漏檢
            Vector3 origin = from + Vector3.up * 0.5f;
            float castDist = dist + Mathf.Max(0f, probeExtra);
            if (Physics.SphereCast(origin, probeR, dir, out var hit, castDist, mask, QueryTriggerInteraction.Ignore))
            {
                // 命中：對「當前→最終目標」重新排路
                Vector3 goal = _path[_path.Count - 1];
                UnitPathQueue.Enqueue(from, goal, OnPath);
                _lastRepathAt = Time.time;
                DevLog($"[DEV] UnitAgent: dynamic re-path (hit {hit.collider.name})");
            }
        }

        private void SampleCreepIfDue()
        {
            var cfg = GameConfigProvider.Current;
            float dt = Time.deltaTime;
            float interval = (cfg != null && cfg.creepSampleInterval > 0f) ? cfg.creepSampleInterval : 0.25f;
            _creepSampleTimer += dt;
            if (_creepSampleTimer < interval) return;
            _creepSampleTimer = 0f;

            bool on = (OnCreepPredicate != null) && OnCreepPredicate(transform.position);
            if (on != _isOnCreep)
            {
                float onMul  = (cfg != null) ? Mathf.Max(0.01f, cfg.creepSpeedMul)     : 1f;
                float offMul = (cfg != null) ? Mathf.Max(0.01f, cfg.offCreepSpeedMul) : 1f;
                _speedFactor = on ? onMul : offMul;
                _isOnCreep = on;
                Debug.Log($"[DEV] UnitAgent: creep={(on ? "ON" : "OFF")} speedMul={_speedFactor:0.##}");
            }
            else
            {
                // 初次取樣時也要設好倍率
                if (_speedFactor <= 0f)
                {
                    float onMul  = (cfg != null) ? Mathf.Max(0.01f, cfg.creepSpeedMul)     : 1f;
                    float offMul = (cfg != null) ? Mathf.Max(0.01f, cfg.offCreepSpeedMul) : 1f;
                    _speedFactor = _isOnCreep ? onMul : offMul;
                }
            }
        }

        /// <summary>
        /// [M4-T07] 建築變更事件處理：若「當前→最終目標」線段與事件圓相交，立即 re-path。
        /// </summary>
        private void OnObstaclesChanged(Vector3 center, float radius)
        {
            if (_path == null || _path.Count == 0) return;
            if (Time.time < _lastRepathAt + dynamicRepathCooldown) return; // 與 T06 共用冷卻

            Vector3 a = transform.position;
            Vector3 b = _path[_path.Count - 1]; // 以最終目標估計路徑線段
            float d = DistPointToSegmentXZ(center, a, b);
            if (d <= (radius + _eventInflate))
            {
                UnitPathQueue.Enqueue(a, b, OnPath);
                _lastRepathAt = Time.time;
                DevLog($"[DEV] UnitAgent: event re-path (d={d:0.##} ≤ r={radius:0.##})");
            }
        }

        // 計算點到線段（XZ 平面）的距離
        private static float DistPointToSegmentXZ(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector2 P = new Vector2(p.x, p.z);
            Vector2 A = new Vector2(a.x, a.z);
            Vector2 B = new Vector2(b.x, b.z);
            Vector2 AB = B - A;
            float len2 = AB.sqrMagnitude;
            if (len2 < 1e-6f) return (P - A).magnitude;
            float t = Vector2.Dot(P - A, AB) / len2;
            t = Mathf.Clamp01(t);
            Vector2 proj = A + AB * t;
            return (P - proj).magnitude;
        }
    }
}