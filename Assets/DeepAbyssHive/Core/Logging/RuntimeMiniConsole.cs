using System.Collections.Generic;
using UnityEngine;

namespace DeepAbyssHive.Dev.Logging
{
    /// <summary>簡易 in-game 浮窗（僅顯示 >= overlayMinLevel）。預設關閉。</summary>
    public class RuntimeMiniConsole : MonoBehaviour
    {
        private readonly Queue<string> _last = new();
        private DevLogSettingsSO _settings;
        // 浮窗控制
        private Rect _rect = new Rect(12, 12, 520, 220);
        private Vector2 _scroll;
        private bool _visible;          // 顯示總開關（可熱鍵切換）
        private bool _pinned = true;    // 釘選（不會自動隱藏）
        private KeyCode _toggleKey = KeyCode.BackQuote; // 可由 GameConfig 覆蓋
        private const string PrefKey = "DAH.RuntimeMiniConsole.Rect";
        private const string PrefVis = "DAH.RuntimeMiniConsole.Visible";
        private const string PrefPin = "DAH.RuntimeMiniConsole.Pinned";

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _settings = Resources.Load<DevLogSettingsSO>("DevLogSettings");
            // 載入持久化狀態
            LoadPrefs();
            // 讀 GameConfig 的熱鍵與預設（若存在，反射容忍）
            TryLoadGameConfigKeys();
            Application.logMessageReceived += OnLog;
        }
        private void OnDestroy() => Application.logMessageReceived -= OnLog;

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey)) { _visible = !_visible; SavePrefs(); }
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (_settings != null && (int)type < (int)_settings.overlayMinLevel) return;
            _last.Enqueue(condition);
            while (_last.Count > 12) _last.Dequeue();
        }

        private void OnGUI()
        {
            if (!_visible && !_pinned) return; // 未釘選且未顯示 → 不畫

            // 視窗
            _rect = GUI.Window(0xDA11CE, _rect, DrawWindow, _settings != null ? $"MiniConsole (≥{_settings.overlayMinLevel})" : "MiniConsole");
            // 夾在螢幕範圍內
            _rect.x = Mathf.Clamp(_rect.x, 0, Screen.width - _rect.width);
            _rect.y = Mathf.Clamp(_rect.y, 0, Screen.height - _rect.height);
        }

        private void DrawWindow(int id)
        {
            // 標題列成為拖曳區
            var drag = new Rect(0, 0, _rect.width, 20);
            GUI.DragWindow(drag);

            GUILayout.Space(2);
            // 工具列：Pin / Clear / Visible 狀態
            GUILayout.BeginHorizontal();
            bool newPinned = GUILayout.Toggle(_pinned, "Pin");
            if (newPinned != _pinned) { _pinned = newPinned; SavePrefs(); }
            if (GUILayout.Button("Clear", GUILayout.Width(60))) { _last.Clear(); }
            GUILayout.FlexibleSpace();
            GUILayout.Label(_visible ? "Visible" : "Hidden");
            GUILayout.EndHorizontal();

            // 日誌列表
            _scroll = GUILayout.BeginScrollView(_scroll);
            foreach (var s in _last) GUILayout.Label(s);
            GUILayout.EndScrollView();
        }

        private void LoadPrefs()
        {
            // Rect
            var s = PlayerPrefs.GetString(PrefKey, "");
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
            _visible = PlayerPrefs.GetInt(PrefVis, 1) != 0;
            _pinned = PlayerPrefs.GetInt(PrefPin, 1) != 0;
        }

        private void SavePrefs()
        {
            PlayerPrefs.SetString(PrefKey, $"{_rect.x},{_rect.y},{_rect.width},{_rect.height}");
            PlayerPrefs.SetInt(PrefVis, _visible ? 1 : 0);
            PlayerPrefs.SetInt(PrefPin, _pinned ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void TryLoadGameConfigKeys()
        {
            // 以反射嘗試 GameConfig，欄位可能名為 smartConsoleToggleKey / consoleToggleKey / backquoteToggleKey 等
            var cfg = Resources.Load<ScriptableObject>("Configs/GameConfig");
            if (cfg == null) return;
            var t = cfg.GetType();
            var keyProp = t.GetField("smartConsoleToggleKey", System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)
                        ?? t.GetField("consoleToggleKey", System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            if (keyProp != null)
            {
                try
                {
                    var v = keyProp.GetValue(cfg);
                    if (v is KeyCode kc) _toggleKey = kc;
                    else if (v is string s && System.Enum.TryParse(s, out KeyCode kc2)) _toggleKey = kc2;
                } catch {}
            }
            var pinnedProp = t.GetField("smartConsolePinOnStart", System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            if (pinnedProp != null)
            {
                try { _pinned = System.Convert.ToBoolean(pinnedProp.GetValue(cfg)); } catch {}
            }
            var startVis = t.GetField("smartConsoleStartVisible", System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            if (startVis != null)
            {
                try { _visible = System.Convert.ToBoolean(startVis.GetValue(cfg)); } catch {}
            }
        }
    }
}