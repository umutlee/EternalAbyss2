using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using DeepAbyssHive.Core.Logging;
using DeepAbyssHive.Core.Config;

namespace DeepAbyssHive.Units.Runtime
{
    /// <summary>
    /// 單位貼地與走失保險（正式版，無 HUD）。
    /// - 每幀最多處理 N 隻（GameConfig.unitGroundPerFrame；預設 64）。
    /// - 每隻至少每 unitGroundSampleInterval 取樣一次（預設 0.2s）。
    /// - 取樣優先 TerrainManager.SampleHeight(pos)；失敗則 Physics.Raycast(↓)（優先 Terrain 層）。
    /// - 走失保險：|pos| > unitSafeRadius 或 y < unitMinY → 傳回 lastGround 或 (0,2,0)。
    /// - 啟動時輸出一行 CONFIG，缺欄位時印一次 WARN（避免刷屏）。
    /// </summary>
    public sealed class UnitGroundFollowerManager : MonoBehaviour
    {
        private static UnitGroundFollowerManager s_inst;

        private readonly List<Tracked> _units = new List<Tracked>(256);
        private int _cursor;
        private bool _warnedMissingConfig;

        // 綁定的設定（由 GameConfig 讀取；若缺以預設）
        private bool _enabled = true;
        private int _perFrame = 64;
        private float _sampleInterval = 0.2f;
        private float _offset = 0.05f;
        private float _safeRadius = 4000f;
        private float _minY = -50f;
        private float _castUp = 5f;
        private float _castDown = 500f;
        private LayerMask _terrainMask;

        // Terrain.SampleHeight 反射
        private Component _terrainMgr;
        private MethodInfo _miSampleHeight;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (s_inst != null) return;
            var go = new GameObject("UnitGroundFollowerManager");
            DontDestroyOnLoad(go);
            s_inst = go.AddComponent<UnitGroundFollowerManager>();
        }

        private void Awake()
        {
            ReadConfigOrDefaults();

            // 參數保底，避免異常值造成行為失控
            _sampleInterval = Mathf.Max(0.02f, _sampleInterval);
            _perFrame = Mathf.Max(1, _perFrame);

            BindTerrainSampleIfPossible();
            _terrainMask = LayerMask.GetMask("Terrain"); // 若專案無 Terrain 層 → 0，稍後落回 Default Raycast

            DAHLog.Info(LogCategory.CONFIG,
                $"[UnitGrounding] enabled={_enabled} perFrame={_perFrame} interval={_sampleInterval}s offset={_offset} safeRadius={_safeRadius} minY={_minY} castUp={_castUp} castDown={_castDown}");

            InvokeRepeating(nameof(RescanUnits), 0.5f, 1.0f);
        }

        private void Update()
        {
            if (!_enabled) return;
            int count = _units.Count;
            if (count == 0) return;

            // 本幀處理配額（保底）
            int budget = Mathf.Clamp(_perFrame, 1, 2048);
            int processed = 0;
            float now = Time.unscaledTime;

            // 關鍵修復：最多僅拜訪「count 次」，避免當前無任何 due 單位時 busy-loop。
            int visits = 0;
            while (processed < budget && visits < count)
            {
                if (_cursor >= _units.Count) _cursor = 0;
                if (_units.Count == 0) break; // 可能剛好被清理

                var t = _units[_cursor];
                _cursor++;
                visits++;

                if (t == null || t.Tr == null) continue;

                if (now >= t.NextAt)
                {
                    t.NextAt = now + _sampleInterval;
                    Step(t);
                    processed++;
                }
                // 若未到期，直接跳過；不增加 processed，讓本幀可繼續找下一個，
                // 但最多拜訪 count 次，確保本幀結束。
            }
        }

        private void Step(Tracked t)
        {
            var tr = t.Tr;
            Vector3 pos = tr.position;

            // 走失保險
            if (pos.y < _minY || new Vector2(pos.x, pos.z).magnitude > _safeRadius)
            {
                Vector3 back = t.HasGround ? t.LastGround : new Vector3(0, 2, 0);
                tr.position = back;
                DAHLog.Warn(LogCategory.UNITS, $"[GroundGuard] Teleport {tr.name} back to {back} (pos={pos})");
                return;
            }

            // 取樣地面高度
            if (TrySampleHeight(pos, out float h))
            {
                // 新：加上個別單位的 footOffset（pivot→模型底部的距離）
                float targetY = h + t.FootOffset + _offset;
                if (Mathf.Abs(pos.y - targetY) > 0.02f)
                {
                    pos.y = targetY;
                    tr.position = pos;
                }
                t.LastGround = new Vector3(pos.x, targetY, pos.z);
                t.HasGround = true;
            }
            else
            {
                // 無地面：若下墜過多，拉回 lastGround（若有）
                if (t.HasGround && pos.y < t.LastGround.y - 5f)
                {
                    pos.y = t.LastGround.y;
                    tr.position = pos;
                    DAHLog.Info(LogCategory.UNITS, $"[GroundGuard] Lift {tr.name} to last ground y={t.LastGround.y:0.00}");
                }
            }
        }

        private bool TrySampleHeight(Vector3 pos, out float height)
        {
            // 1) TerrainManager.SampleHeight
            if (_miSampleHeight != null && _terrainMgr != null)
            {
                try
                {
                    object ret = _miSampleHeight.Invoke(_terrainMgr, new object[] { pos });
                    if (ret is float f && !float.IsNaN(f) && !float.IsInfinity(f))
                    {
                        height = f;
                        return true;
                    }
                }
                catch { /* fallthrough */ }
            }

            // 2) 物理 Raycast ↓
            Vector3 origin = pos + Vector3.up * _castUp;
            Ray ray = new Ray(origin, Vector3.down);
            RaycastHit hit;
            int mask = _terrainMask.value != 0 ? _terrainMask : Physics.DefaultRaycastLayers;
            if (Physics.Raycast(ray, out hit, _castDown + _castUp, mask, QueryTriggerInteraction.Ignore))
            {
                height = hit.point.y;
                return true;
            }

            height = 0f;
            return false;
        }

        private void RescanUnits()
        {
            var all = UnityEngine.Object.FindObjectsByType<Component>(FindObjectsSortMode.None);
            var set = new HashSet<Transform>();
            foreach (var c in all)
            {
                if (c == null) continue;
                if (c.GetType().Name == "UnitAgent")
                    set.Add(c.transform);
            }

            foreach (var tr in set)
            {
                if (!_units.Exists(u => u.Tr == tr))
                {
                    var u = new Tracked { Tr = tr, NextAt = 0f, HasGround = false };
                    u.FootOffset = ComputeFootOffset(tr); // 計算個別 offset
                    _units.Add(u);
                }
            }
            _units.RemoveAll(u => u == null || u.Tr == null);
        }

        /// <summary>計算 pivot 到模型/碰撞體底部的距離（世界座標）。</summary>
        private float ComputeFootOffset(Transform tr)
        {
            // 1) 優先使用任何 Collider 的 bounds
            var col = tr.GetComponentInChildren<Collider>();
            if (col != null)
            {
                var b = col.bounds;
                return Mathf.Max(0f, tr.position.y - b.min.y);
            }
            // 2) 退回所有 Renderer 的 bounds
            var rends = tr.GetComponentsInChildren<Renderer>();
            if (rends != null && rends.Length > 0)
            {
                var b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                return Mathf.Max(0f, tr.position.y - b.min.y);
            }
            // 3) 無可用資訊時，用 0.9（capsule 預設半高）作為保守值
            return 0.9f;
        }

        private void ReadConfigOrDefaults()
        {
            // 透過 GameConfigProvider.Current 反射讀欄位；缺時使用預設並僅警告一次。
            try
            {
                var prov = Type.GetType("DeepAbyssHive.Core.Config.GameConfigProvider, Assembly-CSharp");
                var cfg = prov?.GetProperty("Current", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (cfg != null)
                {
                    var t = cfg.GetType();
                    bool anyMissing = false;

                    _enabled        = ReadBool (t, cfg, GameConfigKeys_UnitGrounding.Enable) ?? _enabled;
                    _perFrame       = (int)(ReadFloat(t, cfg, GameConfigKeys_UnitGrounding.PerFrame) ?? _perFrame);
                    _sampleInterval = ReadFloat(t, cfg, GameConfigKeys_UnitGrounding.SampleInterval) ?? _sampleInterval;
                    _offset         = ReadFloat(t, cfg, GameConfigKeys_UnitGrounding.Offset) ?? _offset;
                    _safeRadius     = ReadFloat(t, cfg, GameConfigKeys_UnitGrounding.SafeRadius) ?? _safeRadius;
                    _minY           = ReadFloat(t, cfg, GameConfigKeys_UnitGrounding.MinY) ?? _minY;
                    _castUp         = ReadFloat(t, cfg, GameConfigKeys_UnitGrounding.CastUp) ?? _castUp;
                    _castDown       = ReadFloat(t, cfg, GameConfigKeys_UnitGrounding.CastDown) ?? _castDown;

                    // 檢查欄位是否存在（用於提示你把它們加進 GameConfigSO）
                    anyMissing |= !HasMember(t, GameConfigKeys_UnitGrounding.Enable);
                    anyMissing |= !HasMember(t, GameConfigKeys_UnitGrounding.PerFrame);
                    anyMissing |= !HasMember(t, GameConfigKeys_UnitGrounding.SampleInterval);
                    anyMissing |= !HasMember(t, GameConfigKeys_UnitGrounding.Offset);
                    anyMissing |= !HasMember(t, GameConfigKeys_UnitGrounding.SafeRadius);
                    anyMissing |= !HasMember(t, GameConfigKeys_UnitGrounding.MinY);
                    anyMissing |= !HasMember(t, GameConfigKeys_UnitGrounding.CastUp);
                    anyMissing |= !HasMember(t, GameConfigKeys_UnitGrounding.CastDown);

                    if (anyMissing && !_warnedMissingConfig)
                    {
                        _warnedMissingConfig = true;
                        DAHLog.Warn(LogCategory.CONFIG, "[UnitGrounding] Some GameConfig fields are missing; using defaults. Please add fields defined in GameConfigKeys_UnitGrounding.");
                    }
                }
            }
            catch
            {
                if (!_warnedMissingConfig)
                {
                    _warnedMissingConfig = true;
                    DAHLog.Warn(LogCategory.CONFIG, "[UnitGrounding] GameConfig not found; using defaults.");
                }
            }
        }

        private void BindTerrainSampleIfPossible()
        {
            try
            {
                foreach (var c in GameObject.FindObjectsByType<Component>(FindObjectsSortMode.None))
                {
                    if (c.GetType().FullName == "DeepAbyssHive.Terrain.Managers.TerrainManager")
                    {
                        _terrainMgr = c;
                        break;
                    }
                }
                if (_terrainMgr != null)
                {
                    _miSampleHeight = _terrainMgr.GetType().GetMethod("SampleHeight",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, new[] { typeof(Vector3) }, null);
                }
            }
            catch { }
        }

        private static bool HasMember(Type t, string name)
        {
            const BindingFlags F = BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase;
            return t.GetField(name, F) != null || t.GetProperty(name, F) != null;
        }

        private static bool? ReadBool(Type t, object o, string name)
        {
            const BindingFlags F = BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase;
            var f = t.GetField(name, F); if (f != null && f.FieldType == typeof(bool)) return (bool)f.GetValue(o);
            var p = t.GetProperty(name, F); if (p != null && p.PropertyType == typeof(bool)) return (bool)p.GetValue(o, null);
            return null;
        }
        private static float? ReadFloat(Type t, object o, string name)
        {
            const BindingFlags F = BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase;
            var f = t.GetField(name, F);
            if (f != null)
            {
                var ft = f.FieldType;
                if (ft == typeof(float) || ft == typeof(int) || ft == typeof(double)) return Convert.ToSingle(f.GetValue(o));
            }
            var p = t.GetProperty(name, F);
            if (p != null)
            {
                var pt = p.PropertyType;
                if (pt == typeof(float) || pt == typeof(int) || pt == typeof(double)) return Convert.ToSingle(p.GetValue(o, null));
            }
            return null;
        }

        private sealed class Tracked
        {
            public Transform Tr;
            public float NextAt;
            public bool HasGround;
            public Vector3 LastGround;
            public float FootOffset; // 新增：pivot→模型/碰撞體底部的距離
        }
    }
}