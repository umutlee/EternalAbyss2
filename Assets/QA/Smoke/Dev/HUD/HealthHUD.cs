using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using DeepAbyssHive.Core.Logging;
using QA.Smoke.Dev;

namespace DeepAbyssHive.QA.Smoke.Dev.HUD
{
    /// <summary>
    /// 以 IMGUI 顯示健康心跳資訊：FPS、記憶體、Units/Buildings 數量。
    /// - 位置可拖拽並持久化（使用 HudDragUtil）。
    /// - 頻率沿用 GameConfig.healthLogInterval。
    /// - 可由 GameConfig.healthHudToggleKey（字串對應 KeyCode）或 Editor 選單切換顯示。
    /// - 使用統一的 DAHLog 日誌系統。
    /// </summary>
    public sealed class HealthHUD : MonoBehaviour
    {
        private static HealthHUD s_instance;

        private Rect _rect;
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
            _rect = HudDragUtil.GetRect("HUD.Health", new Rect(20, 200, 300, 140));
            ReadConfig(out var enabled, out var showHUD, out var interval, out var key);
            _visible = showHUD ?? enabled ?? true;
            _interval = Mathf.Max(0.5f, interval ?? 10f);
            _toggleKey = key;

            DAHLog.Info(LogCategory.CONFIG, $"HealthHUD config → enabled={enabled}, showHUD={showHUD}, interval={_interval}, toggleKey={_toggleKey}");
            DAHLog.Info(LogCategory.DEV, "HealthHUD initialized");
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
            
            _rect = GUI.Window(0x0EA001, _rect, DrawHealthWindow, "Health HUD");
            
            // 保存位置到 HudDragUtil（保持兼容性）
            HudDragUtil.SaveRect("HUD.Health", _rect);
        }

        private void DrawHealthWindow(int windowId)
        {
            GUILayout.Label($"FPS: {_fps}");
            GUILayout.Label($"Memory (MB): {_mem}");
            GUILayout.Label($"Units: {_units}  |  Buildings: {_buildings}");
            var remain = Mathf.Max(0f, _nextAt - Time.realtimeSinceStartup);
            GUILayout.Label($"Next update in: {remain:0.0}s");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh")) 
            {
                RefreshStats();
                GUI.FocusControl(null); // 釋放焦點，避免干擾拖曳
            }
            if (GUILayout.Button("Close")) 
            { 
                _visible = false; 
                GUI.FocusControl(null); // 釋放焦點，避免干擾拖曳
            }
            GUILayout.EndHorizontal();
            
            // 啟用拖拽功能
            GUI.DragWindow();
        }

        public static void Toggle()
        {
            if (s_instance == null) return;
            s_instance._visible = !s_instance._visible;
            DAHLog.Info(LogCategory.DEV, $"HealthHUD visible = {s_instance._visible}");
            if (s_instance._visible) s_instance.RefreshStats();
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


    }
}