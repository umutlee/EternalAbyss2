using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace DeepAbyssHive.QA.Smoke.Dev.HUD
{
    /// <summary>
    /// 以 IMGUI 顯示健康心跳資訊：FPS、記憶體、Units/Buildings 數量。
    /// - 位置可拖拽並持久化（EditorPrefs/PlayerPrefs）。
    /// - 頻率沿用 GameConfig.healthLogInterval。
    /// - 可由 GameConfig.healthHudToggleKey（字串對應 KeyCode）或 Editor 選單切換顯示。
    /// - 日誌分類：HUD（一般訊息）、CONFIG（啟動值輸出）。
    /// </summary>
    public sealed class HealthHUD : MonoBehaviour
    {
        private const string RectPrefKey = "DeepAbyssHive.HealthHUD.Rect";
        private static HealthHUD s_instance;

        private Rect _rect = new Rect(20, 200, 300, 140);
        private bool _visible = true;
        private float _interval = 10f;
        private KeyCode? _toggleKey = null;

        // 顯示資料
        private string _fps = "N/A";
        private string _mem = "N/A";
        private string _units = "N/A";
        private string _buildings = "N/A";
        private float _nextAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (s_instance != null) return;
            var go = new GameObject("DevHUD_Health"); 
            DontDestroyOnLoad(go);
            s_instance = go.AddComponent<HealthHUD>();
        }

        private void Awake()
        {
            LoadRect();
            ReadConfig(out var enabled, out var showHUD, out var interval, out var key);
            _visible = showHUD ?? enabled ?? true;
            _interval = Mathf.Max(0.5f, interval ?? 10f);
            _toggleKey = key;

            SmartConsoleShim.Log("CONFIG", $"HealthHUD config → enabled={enabled}, showHUD={showHUD}, interval={_interval}, toggleKey={_toggleKey}");
            SmartConsoleShim.Log("HUD", "HealthHUD initialized");
            StartCoroutine(Heartbeat());
        }

        private void Update()
        {
            if (_toggleKey.HasValue && Input.GetKeyDown(_toggleKey.Value))
            {
                Toggle();
            }
        }

        private IEnumerator Heartbeat()
        {
            while (true)
            {
                RefreshStats();
                _nextAt = Time.realtimeSinceStartup + _interval;
                yield return new WaitForSeconds(_interval);
            }
        }

        private void RefreshStats()
        {
            // FPS（取 smoothDeltaTime 的倒數）
            _fps = (1f / Mathf.Max(0.0001f, Time.smoothDeltaTime)).ToString("0.0");

            // 記憶體（GC 總量，MB）
            long bytes = GC.GetTotalMemory(false);
            _mem = (bytes / (1024f * 1024f)).ToString("0.0");

            // Units / Buildings 數（盡量輕量；每 interval 才做一次）
            _units = TryCountUnits();
            _buildings = TryCountBuildings();
        }

        private string TryCountUnits()
        {
            try
            {
                // 嘗試從 UnitManager 取值：常見欄位 ActiveCount / Count / Units.Count
                var mgr = FindTypeInstance("DeepAbyssHive.Units.Managers.UnitManager");
                if (mgr != null)
                {
                    var t = mgr.GetType();
                    foreach (var n in new[] { "ActiveCount", "Count" })
                    {
                        var pi = t.GetProperty(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
                        if (pi != null && pi.PropertyType == typeof(int)) return pi.GetValue(mgr).ToString();
                        var fi = t.GetField(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
                        if (fi != null && fi.FieldType == typeof(int)) return fi.GetValue(mgr).ToString();
                    }
                    foreach (var n in new[] { "Units", "AllUnits" })
                    {
                        var pi = t.GetProperty(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
                        if (pi != null && typeof(System.Collections.ICollection).IsAssignableFrom(pi.PropertyType))
                            return (pi.GetValue(mgr) as System.Collections.ICollection)?.Count.ToString() ?? "N/A";
                        var fi = t.GetField(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
                        if (fi != null && typeof(System.Collections.ICollection).IsAssignableFrom(fi.FieldType))
                            return (fi.GetValue(mgr) as System.Collections.ICollection)?.Count.ToString() ?? "N/A";
                    }
                }
            }
            catch { }
            return "N/A";
        }

        private string TryCountBuildings()
        {
            try
            {
                int layer = LayerMask.NameToLayer("Building");
                if (layer < 0) return "N/A";
                int count = 0;
                var all = UnityEngine.Object.FindObjectsOfType<Transform>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    var go = all[i].gameObject;
                    if (go.layer == layer && go.activeInHierarchy) count++;
                }
                return count.ToString();
            }
            catch { }
            return "N/A";
        }

        private static UnityEngine.Object FindTypeInstance(string fqcn)
        {
            var t = Type.GetType(fqcn);
            if (t == null) return null;
            var arr = UnityEngine.Object.FindObjectsByType(t, FindObjectsSortMode.None);
            return arr != null && arr.Length > 0 ? arr[0] : null;
        }

        private void OnGUI()
        {
            if (!_visible) return;
            _rect = GUI.Window(0xDA0001, _rect, DrawWindow, "Health HUD");
        }

        private void DrawWindow(int id)
        {
            GUILayout.Label($"FPS: {_fps}");
            GUILayout.Label($"Memory (MB): {_mem}");
            GUILayout.Label($"Units: {_units}  |  Buildings: {_buildings}");
            var remain = Mathf.Max(0f, _nextAt - Time.realtimeSinceStartup);
            GUILayout.Label($"Next update in: {remain:0.0}s");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh")) RefreshStats();
            if (GUILayout.Button("Close")) { _visible = false; SaveRect(); }
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        public static void Toggle()
        {
            if (s_instance == null) return;
            s_instance._visible = !s_instance._visible;
            SmartConsoleShim.Log("HUD", $"HealthHUD visible = {s_instance._visible}");
            if (s_instance._visible) s_instance.RefreshStats();
            s_instance.SaveRect();
        }

        private void OnDisable() => SaveRect();
        private void OnDestroy() => SaveRect();

        private void LoadRect()
        {
#if UNITY_EDITOR
            string s = UnityEditor.EditorPrefs.GetString(RectPrefKey, string.Empty);
#else
            string s = PlayerPrefs.GetString(RectPrefKey, string.Empty);
#endif
            if (!string.IsNullOrEmpty(s))
            {
                var parts = s.Split(',');
                if (parts.Length == 4 &&
                    float.TryParse(parts[0], out var x) &&
                    float.TryParse(parts[1], out var y) &&
                    float.TryParse(parts[2], out var w) &&
                    float.TryParse(parts[3], out var h))
                {
                    _rect = new Rect(x, y, w, h);
                }
            }
        }

        private void SaveRect()
        {
            string s = $"{_rect.x:F0},{_rect.y:F0},{_rect.width:F0},{_rect.height:F0}";
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetString(RectPrefKey, s);
#else
            PlayerPrefs.SetString(RectPrefKey, s);
            PlayerPrefs.Save();
#endif
        }

        private static void ReadConfig(out bool? enabled, out bool? showHUD, out float? interval, out KeyCode? key)
        {
            enabled = null; showHUD = null; interval = null; key = null;
            var cfg = Resources.Load("Configs/GameConfig"); if (cfg == null) return;
            var t = cfg.GetType();
            enabled = ReadBool(t, cfg, "healthLoggingEnabled", "healthEnabled");
            showHUD = ReadBool(t, cfg, "showHealthHUD", "healthHudVisible");
            interval = ReadFloat(t, cfg, "healthLogInterval", "healthInterval");
            var keyStr = ReadString(t, cfg, "healthHudToggleKey", "healthHUDKey");
            if (!string.IsNullOrEmpty(keyStr) && Enum.TryParse<KeyCode>(keyStr, true, out var parsed))
                key = parsed;
        }

        private static bool? ReadBool(Type t, object o, params string[] names)
        {
            foreach (var n in names)
            {
                var fi = t.GetField(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase);
                if (fi != null && fi.FieldType == typeof(bool)) return (bool)fi.GetValue(o);
                var pi = t.GetProperty(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase);
                if (pi != null && pi.PropertyType == typeof(bool)) return (bool)pi.GetValue(o, null);
            }
            return null;
        }

        private static float? ReadFloat(Type t, object o, params string[] names)
        {
            foreach (var n in names)
            {
                var fi = t.GetField(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase);
                if (fi != null && (fi.FieldType == typeof(float) || fi.FieldType == typeof(double)))
                    return Convert.ToSingle(fi.GetValue(o));
                var pi = t.GetProperty(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase);
                if (pi != null && (pi.PropertyType == typeof(float) || pi.PropertyType == typeof(double)))
                    return Convert.ToSingle(pi.GetValue(o, null));
            }
            return null;
        }

        private static string ReadString(Type t, object o, params string[] names)
        {
            foreach (var n in names)
            {
                var fi = t.GetField(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase);
                if (fi != null && fi.FieldType == typeof(string)) return (string)fi.GetValue(o);
                var pi = t.GetProperty(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase);
                if (pi != null && pi.PropertyType == typeof(string)) return (string)pi.GetValue(o, null);
            }
            return null;
        }

        /// <summary>Smart Console 輕量橋接。</summary>
        private static class SmartConsoleShim
        {
            private static Action<string,string> s_log;
            private static bool s_warned;
            public static void Log(string category, string message)
            {
                if (s_log == null) s_log = FindLogger();
                if (s_log != null) s_log(category, message);
                else if (!s_warned) { s_warned = true; Debug.LogWarning($"[{category}] {message} (Smart Console logger not found; fallback once)"); }
            }
            private static Action<string,string> FindLogger()
            {
                // 直接使用Debug.Log避免反射遞歸問題
                return (c,m) => Debug.Log($"[{c}] {m}");
            }
        }
    }
}