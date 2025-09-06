using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Units.Pathfinding;

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

        public void SetDestination(Vector3 worldTarget)
        {
            var start = transform.position;
            UnitPathQueue.Enqueue(start, worldTarget, OnPath);
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
            transform.position += transform.forward * (moveSpeed * Time.deltaTime);
        }
    }
}