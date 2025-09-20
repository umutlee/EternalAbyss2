using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Core.Logging;
using DeepAbyssHive.Core.Config;

namespace DeepAbyssHive.Units.Movement
{
    /// <summary>
    /// 單例：每幀僅處理配額 N 的 GroundFollower，對其做射線採樣貼地與（可選）坡面對齊。
    /// </summary>
    public class GroundingManager : MonoBehaviour
    {
        private static GroundingManager _instance;
        public static void Register(GroundFollower f) { Ensure(); _instance._list.Add(f); }
        public static void Unregister(GroundFollower f) { if (_instance != null) _instance._list.Remove(f); }

        private static void Ensure()
        {
            if (_instance != null) return;
            var go = new GameObject("GroundingManager");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<GroundingManager>();
        }

        private readonly List<GroundFollower> _list = new List<GroundFollower>(1024);
        private int _cursor;
        private int _quota = 64;
        private float _rayMax = 200f;
        private float _snapSpeed = 12f;
        private bool _alignSlope = false;
        private LayerMask _rayMask = Physics.DefaultRaycastLayers;

        private System.Type _terrainMgrType;
        private object _terrainMgr;
        private System.Reflection.MethodInfo _sampleHeight; // optional

        private void Awake()
        {
            // 讀 GameConfig 參數（存在才覆蓋）
            GroundingConfigCompat.TryBindInt("unitGroundUpdatesPerFrame", ref _quota, 64);
            GroundingConfigCompat.TryBindFloat("unitGroundRayMaxDistance", ref _rayMax, 200f);
            GroundingConfigCompat.TryBindFloat("unitGroundSnapSpeed", ref _snapSpeed, 12f);
            GroundingConfigCompat.TryBindBool("unitAlignToSlope", ref _alignSlope, false);
            GroundingConfigCompat.TryBindLayerMask("unitGroundRayMask", ref _rayMask);

            // TerrainManager（若存在則優先使用其 Sample）
            foreach (var m in Object.FindObjectsOfType<MonoBehaviour>())
            {
                var t = m.GetType();
                if (t.Name == "TerrainManager")
                {
                    _terrainMgrType = t;
                    _terrainMgr = m;
                    _sampleHeight = t.GetMethod("SampleHeight", System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                    break;
                }
            }

            DAHLog.Info(LogCategory.UNITS, $"[GROUND] quota={_quota}, rayMax={_rayMax}, snapSpeed={_snapSpeed}, alignSlope={_alignSlope}");
        }

        private void Update()
        {
            int n = Mathf.Min(_quota, _list.Count);
            for (int i = 0; i < n; i++)
            {
                if (_list.Count == 0) break;
                _cursor %= _list.Count;
                var f = _list[_cursor++];
                if (f == null) continue;
                StepFollower(f);
            }
        }

        private void StepFollower(GroundFollower f)
        {
            var tr = f.transform;
            var pos = tr.position;

            float targetY;
            Vector3 normal;

            if (TrySampleTerrain(pos, out targetY, out normal) || TryRaycastDown(pos + Vector3.up * 50f, out targetY, out normal))
            {
                // 平滑貼附
                var y = Mathf.Lerp(pos.y, targetY, 1f - Mathf.Exp(-_snapSpeed * UnityEngine.Time.deltaTime));
                tr.position = new Vector3(pos.x, y, pos.z);

                // 可選：對齊坡面
                if (_alignSlope && normal.sqrMagnitude > 0.1f)
                {
                    var fwd = Vector3.ProjectOnPlane(tr.forward, normal).normalized;
                    if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.Cross(normal, Vector3.right).normalized;
                    var targetRot = Quaternion.LookRotation(fwd, normal);
                    tr.rotation = Quaternion.Slerp(tr.rotation, targetRot, 1f - Mathf.Exp(-_snapSpeed * UnityEngine.Time.deltaTime));
                }
            }
        }

        private bool TrySampleTerrain(Vector3 pos, out float y, out Vector3 normal)
        {
            y = 0; normal = Vector3.up;
            if (_terrainMgr == null || _sampleHeight == null) return false;
            var h = _sampleHeight.Invoke(_terrainMgr, new object[]{ pos });
            if (h is float f)
            {
                y = f; // 若 TerrainManager 不提供法線，就維持 up
                return true;
            }
            return false;
        }

        private bool TryRaycastDown(Vector3 origin, out float y, out Vector3 normal)
        {
            y = 0; normal = Vector3.up;
            if (Physics.Raycast(origin, Vector3.down, out var hit, _rayMax, _rayMask, QueryTriggerInteraction.Ignore))
            {
                y = hit.point.y;
                normal = hit.normal;
                return true;
            }
            return false;
        }
    }
}