using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Core.Lifecycle
{
    /// <summary>
    /// 每隔 lifecyclePollInterval 低頻掃描場景中的 MonoBehaviour，
    /// 對「上一輪 Enabled、這一輪 Disabled 或 Destroyed」的物件呼叫 StopAllCoroutines()，
    /// 防止幽靈協程；所有記錄走 LIFECYCLE 分類。
    /// </summary>
    public class LifecycleSentinel : MonoBehaviour
    {
        private static bool _enabled = true;
        private static float _interval = 1.0f;
        private static bool _stopOnDisable = true;
        private static bool _logDetails = false;

        private float _acc;
        private readonly Dictionary<int, bool> _wasEnabled = new Dictionary<int, bool>(4096);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindObjectOfType<LifecycleSentinel>() != null) return;
            var go = new GameObject("Lifecycle"); go.AddComponent<LifecycleSentinel>();
            var managers = GameObject.Find("Managers"); if (managers != null) go.transform.SetParent(managers.transform);
            DontDestroyOnLoad(go);
            TryLoadFromGameConfig();
            DAHLog.Info(LogCategory.CONFIG, $"Lifecycle enabled={_enabled}, interval={_interval}s, stopOnDisable={_stopOnDisable}");
            DAHLog.Info(LogCategory.SERVICE, "LifecycleSentinel created"); 
        }

        private void Update()
        {
            _acc += Time.unscaledDeltaTime;
            if (_acc < _interval) return;
            _acc = 0f;
            if (!_enabled) return;
            try { SweepOnce(); } catch (Exception ex) { DAHLog.Debug(LogCategory.COMMON, "Sweep failed " + ex.Message); }
        }

        private void SweepOnce()
        {
            var all = GameObject.FindObjectsOfType<MonoBehaviour>(true);
            var seen = new HashSet<int>();
            int stopped = 0, transitions = 0;

            for (int i = 0; i < all.Length; i++)
            {
                var mb = all[i];
                if (mb == null) continue;
                int id = mb.GetInstanceID();
                seen.Add(id);

                bool nowEnabled = mb.enabled && mb.gameObject.activeInHierarchy;
                bool wasEnabled = _wasEnabled.TryGetValue(id, out var we) ? we : false;

                if (wasEnabled && !nowEnabled)
                {
                    transitions++;
                    if (_stopOnDisable)
                    {
                        try { mb.StopAllCoroutines(); stopped++; if (_logDetails) DAHLog.Debug(LogCategory.COMMON, $"Stopped coroutines on {mb.GetType().Name}@{mb.gameObject.name}"); }
                        catch (Exception ex) { DAHLog.Debug(LogCategory.COMMON, $"StopAllCoroutines failed on {mb.GetType().Name} {ex.Message}"); }
                    }
                }

                _wasEnabled[id] = nowEnabled;
            }

            // 清掉已銷毀的
            var toRemove = new List<int>();
            foreach (var kv in _wasEnabled) if (!seen.Contains(kv.Key)) toRemove.Add(kv.Key);
            for (int i = 0; i < toRemove.Count; i++) _wasEnabled.Remove(toRemove[i]);

            if (transitions > 0 || stopped > 0)
                DAHLog.Debug(LogCategory.COMMON, $"Sweep transitions={transitions}, stopped={stopped}, tracked={_wasEnabled.Count}"); 
        }

        private static void TryLoadFromGameConfig()
        {
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var p = asm.GetType("GameConfigProvider") ?? asm.GetType("DeepAbyssHive.Core.Config.GameConfigProvider");
                    if (p == null) continue;
                    var cfg = p.GetProperty("Current", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                    if (cfg == null) continue;
                    var t = cfg.GetType();
                    _enabled       = GetBool(t, cfg, "lifecycleGuardEnabled", true);
                    _interval      = Mathf.Clamp(GetFloat(t, cfg, "lifecyclePollInterval", 1.0f), 0.2f, 10f);
                    _stopOnDisable = GetBool(t, cfg, "stopCoroutinesOnDisable", true);
                    _logDetails    = GetBool(t, cfg, "lifecycleLogDetails", false);
                    break;
                }
            } catch {}
        }

        private static bool GetBool(Type t, object cfg, string name, bool defVal)
        {
            var f = t.GetField(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
            if (f != null && f.FieldType == typeof(bool)) return (bool)f.GetValue(cfg);
            var p = t.GetProperty(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
            if (p != null && p.PropertyType == typeof(bool)) return (bool)p.GetValue(cfg);
            return defVal;
        }
        private static float GetFloat(Type t, object cfg, string name, float defVal)
        {
            var f = t.GetField(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
            if (f != null) return Convert.ToSingle(f.GetValue(cfg));
            var p = t.GetProperty(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
            if (p != null) return Convert.ToSingle(p.GetValue(cfg));
            return defVal;
        }
    }
}