using System.Collections.Generic;
using UnityEngine;

namespace DeepAbyssHive.Dev.Logging
{
    /// <summary>簡易 in-game 浮窗（僅顯示 >= overlayMinLevel）。預設關閉。</summary>
    public class RuntimeMiniConsole : MonoBehaviour
    {
        private readonly Queue<string> _last = new();
        private DevLogSettingsSO _settings;

        private void Awake()
        {
            _settings = Resources.Load<DevLogSettingsSO>("DevLogSettings");
            if (_settings == null || !_settings.enableRuntimeOverlay)
            {
                enabled = false; return;
            }
            Application.logMessageReceived += OnLog;
        }
        private void OnDestroy() => Application.logMessageReceived -= OnLog;

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (_settings == null) return;
            var lvl = DevLogSettingsSO.FromUnityLogType(type);
            if (lvl < _settings.overlayMinLevel) return;
            _last.Enqueue(condition);
            while (_last.Count > 12) _last.Dequeue();
        }

        private void OnGUI()
        {
            if (_settings == null) return;
            var r = new Rect(10, 10, Screen.width * 0.5f, 18 * (_last.Count + 2));
            GUILayout.BeginArea(r, GUI.skin.box);
            GUILayout.Label($"MiniConsole (≥{_settings.overlayMinLevel})");
            foreach (var s in _last) GUILayout.Label(s);
            GUILayout.EndArea();
        }
    }
}