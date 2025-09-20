using System;
using UnityEngine;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Core.Diagnostics
{
    /// <summary>
    /// 全域錯誤監聽與率限。以 Application.logMessageReceived 監看 Exception/Error，按秒率限輸出。
    /// 可用 F12 觸發測試例外（熱鍵可由 GameConfig 覆蓋）。
    /// </summary>
    public class ErrorGuardRunner : MonoBehaviour
    {
        private static bool _enabled = true;
        private static int _limitPerSec = 5;
        private static bool _shortenStack = true;
        private static KeyCode _testKey = KeyCode.F12;

        private float _windowStart;
        private int _windowCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindObjectOfType<ErrorGuardRunner>() != null) return;
            var go = new GameObject("ErrorGuard"); go.AddComponent<ErrorGuardRunner>();
            var managers = GameObject.Find("Managers"); if (managers != null) go.transform.SetParent(managers.transform);
            DontDestroyOnLoad(go);
            TryLoadFromGameConfig();
            DAHLog.Info(LogCategory.CONFIG, $"ErrorGuard: enabled={_enabled}, rate={_limitPerSec}/s, shortStack={_shortenStack}, testKey={_testKey}");
            DAHLog.Info(LogCategory.SERVICE, "ErrorGuardRunner created");
        }

        private void OnEnable()
        {
            Application.logMessageReceived += OnLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= OnLog;
        }

        private void Update()
        {
            if (!_enabled) return;
            if (Input.GetKeyDown(_testKey))
            {
                // 在自身 Update 內拋出例外，驗證捕捉與率限（仍由 Unity 記錄，我們再做整合/抑制）。
                try { throw new InvalidOperationException("Simulated test exception"); }
                catch (Exception ex) { DAHLog.Error(LogCategory.COMMON, ErrorPolicy.FormatException(ex, "Test")); }
            }
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (!_enabled) return;
            if (type != LogType.Exception && type != LogType.Error) return;

            var now = Time.unscaledTime;
            if (now - _windowStart >= 1f) { _windowStart = now; _windowCount = 0; }
            _windowCount++;
            if (_windowCount > _limitPerSec) return; // 率限：丟棄多餘同秒錯誤

            if (_shortenStack && !string.IsNullOrEmpty(stackTrace))
            {
                var lines = stackTrace.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 10) stackTrace = string.Join("\n", lines, 0, 10) + "\n…";
            }

            var cat = type == LogType.Exception ? LogCategory.COMMON : LogCategory.COMMON;
            DAHLog.Error(cat, condition + "\n" + stackTrace);
        }

        private static void TryLoadFromGameConfig()
        {
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var p = asm.GetType("GameConfigProvider") ?? asm.GetType("DeepAbyssHive.Core.Config.GameConfigProvider");
                    if (p == null) continue;
                    var cfg = p.GetProperty("Current", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.GetValue(null);
                    if (cfg == null) continue;
                    var t = cfg.GetType();
                    _enabled = GetBool(t, cfg, "errorGuardEnabled", true);
                    _limitPerSec = GetInt(t, cfg, "errorRateLimitPerSec", 5);
                    _shortenStack = GetBool(t, cfg, "shortenStackTrace", true);
                    _testKey = GetKey(t, cfg, "throwTestErrorKey", KeyCode.F12);
                    break;
                }
            } catch {}
        }

        private static bool GetBool(Type t, object cfg, string name, bool defVal)
        {
            var f = t.GetField(name, System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            if (f != null && f.FieldType == typeof(bool)) return (bool)f.GetValue(cfg);
            var p = t.GetProperty(name, System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            if (p != null && p.PropertyType == typeof(bool)) return (bool)p.GetValue(cfg);
            return defVal;
        }
        private static int GetInt(Type t, object cfg, string name, int defVal)
        {
            var f = t.GetField(name, System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            if (f != null && f.FieldType == typeof(int)) return (int)f.GetValue(cfg);
            var p = t.GetProperty(name, System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            if (p != null && p.PropertyType == typeof(int)) return (int)p.GetValue(cfg);
            return defVal;
        }
        private static KeyCode GetKey(Type t, object cfg, string name, KeyCode defVal)
        {
            var f = t.GetField(name, System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            if (f != null && f.FieldType == typeof(KeyCode)) return (KeyCode)f.GetValue(cfg);
            var p = t.GetProperty(name, System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            if (p != null && p.PropertyType == typeof(KeyCode)) return (KeyCode)p.GetValue(cfg);
            return defVal;
        }
    }
}