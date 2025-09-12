#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace DeepAbyssHive.Dev.Logging.Editor
{
    public class SmartConsoleWindow : EditorWindow
    {
        // ==== 型別 ====
        private class Queued
        {
            public string condition;
            public string stackTrace;
            public LogType type;
            public DateTime timeUtc;
        }
        private class Entry
        {
            public DateTime timeUtc;
            public Dev.Logging.LogLevel level;
            public string category;
            public string message;
            public string stack;
            public int count = 1;
            public double lastHit;
        }

        // ==== 狀態 ====
        private static readonly object _lock = new();
        private static readonly Queue<Queued> _queue = new();
        private static readonly List<Entry> _buffer = new();
        private static DevLogSettingsSO _settings;

        private Vector2 _scroll;
        private string _search = "";
        private Dev.Logging.LogLevel _minLevel;
        private readonly HashSet<string> _selectedCats = new();
        private double _lastSecond = 0;
        private readonly Dictionary<string,int> _secondCount = new();

        // 匹配 [TAG] 前綴
        private static readonly Regex _tagRx = new(@"^\s*\[([^\[\]]+)\]\s*", RegexOptions.Compiled);

        [MenuItem("Window/DeepAbyss/Smart Console %#l")] // Shift+Ctrl/Cmd+L
        public static void Open()
        {
            var win = GetWindow<SmartConsoleWindow>("Smart Console");
            win.minSize = new Vector2(520, 220);
            win.Show();
        }

        private void OnEnable()
        {
            EnsureSettings();
            _minLevel = _settings.minLevel;
            _selectedCats.Clear();
            foreach (var c in _settings.knownCategories) _selectedCats.Add(c);

            Application.logMessageReceivedThreaded += OnLogThreaded;
            EditorApplication.update += UpdatePump;
        }

        private void OnDisable()
        {
            Application.logMessageReceivedThreaded -= OnLogThreaded;
            EditorApplication.update -= UpdatePump;
        }

        private static void EnsureSettings()
        {
            if (_settings) return;
            _settings = Resources.Load<DevLogSettingsSO>("DevLogSettings");
            if (!_settings)
            {
                // 自動建立
                _settings = ScriptableObject.CreateInstance<DevLogSettingsSO>();
                System.IO.Directory.CreateDirectory("Assets/Resources");
                AssetDatabase.CreateAsset(_settings, "Assets/Resources/DevLogSettings.asset");
                AssetDatabase.SaveAssets();
            }
        }

        private static void OnLogThreaded(string condition, string stackTrace, LogType type)
        {
            lock (_lock) { _queue.Enqueue(new Queued {
                condition = condition, stackTrace = stackTrace, type = type, timeUtc = DateTime.UtcNow
            }); }
        }

        private void UpdatePump()
        {
            // 取設定
            EnsureSettings();

            // 速率統計清零（每秒）
            var now = EditorApplication.timeSinceStartup;
            if (Math.Abs(now - _lastSecond) > 1.0)
            {
                _secondCount.Clear();
                _lastSecond = now;
            }

            // 取出佇列
            int safety = 4096;
            while (safety-- > 0)
            {
                Queued q = null;
                lock (_lock) { if (_queue.Count > 0) q = _queue.Dequeue(); }
                if (q == null) break;

                var entry = BuildEntry(q);
                if (!_settings.enableRateLimit)
                {
                    Push(entry);
                    continue;
                }

                // 分類速率限制
                var key = entry.category ?? "General";
                _secondCount.TryGetValue(key, out var cnt);
                if (cnt >= _settings.maxLogsPerSecond)
                {
                    // 第一次超限時寫一條省略訊息
                    if (cnt == _settings.maxLogsPerSecond)
                    {
                        Push(new Entry{
                            timeUtc = DateTime.UtcNow,
                            level   = Dev.Logging.LogLevel.Debug,
                            category= key,
                            message = $"… rate limit: more messages skipped",
                            stack   = ""
                        });
                    }
                    _secondCount[key] = cnt + 1;
                    continue;
                }
                _secondCount[key] = cnt + 1;
                Push(entry);
            }
        }

        private Entry BuildEntry(Queued q)
        {
            var level = DevLogSettingsSO.FromUnityLogType(q.type);
            // 嘗試讀取 [Level][Category] 前綴
            string category = null;
            string message  = q.condition;

            // 若 message 形如 [Level][Category] xxx
            var m2 = Regex.Match(message, @"^\s*\[(Trace|Debug|Info|Warn|Error|Fatal)\]\[([^\[\]]+)\]\s*(.*)$");
            if (m2.Success)
            {
                level    = (Dev.Logging.LogLevel) Enum.Parse(typeof(Dev.Logging.LogLevel), m2.Groups[1].Value, true);
                category = m2.Groups[2].Value;
                message  = m2.Groups[3].Value;
            }
            else if (_settings.enableTagBridge)
            {
                var m = _tagRx.Match(message);
                if (m.Success)
                {
                    category = m.Groups[1].Value.Trim();
                    message  = message.Substring(m.Length);
                }
            }

            if (string.IsNullOrEmpty(category))
                category = _settings.fallbackCategory;

            // 動態補進分類清單
            if (!_settings.knownCategories.Contains(category))
            {
                _settings.knownCategories.Add(category);
                EditorUtility.SetDirty(_settings);
            }

            return new Entry {
                timeUtc = q.timeUtc,
                level   = level,
                category= category,
                message = message,
                stack   = q.stackTrace,
                lastHit = EditorApplication.timeSinceStartup
            };
        }

        private void Push(Entry e)
        {
            // 層級門檻
            if (e.level < _minLevel) return;

            if (_settings.foldDuplicates)
            {
                // 在時間窗內找可折疊項（相同 level+category+message）
                for (int i = _buffer.Count - 1; i >= 0; --i)
                {
                    var x = _buffer[i];
                    if (x.level == e.level && x.category == e.category && x.message == e.message)
                    {
                        if (EditorApplication.timeSinceStartup - x.lastHit <= _settings.foldWindowSeconds)
                        {
                            x.count++;
                            x.lastHit = EditorApplication.timeSinceStartup;
                            Repaint();
                            return;
                        }
                        break;
                    }
                }
            }

            _buffer.Add(e);
            // 緩衝上限
            var cap = Mathf.Max(100, _settings.ringBufferCapacity);
            if (_buffer.Count > cap) _buffer.RemoveRange(0, _buffer.Count - cap);
            Repaint();
        }

        private bool PassFilters(Entry e)
        {
            if (e.level < _minLevel) return false;
            if (_selectedCats.Count > 0 && !_selectedCats.Contains(e.category)) return false;
            if (!string.IsNullOrEmpty(_search))
            {
                if (e.message == null || e.message.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }
            return true;
        }

        private static Color LevelColor(Dev.Logging.LogLevel lv) => lv switch
        {
            Dev.Logging.LogLevel.Trace => new Color(0.6f,0.6f,0.6f),
            Dev.Logging.LogLevel.Debug => new Color(0.75f,0.75f,0.75f),
            Dev.Logging.LogLevel.Info  => Color.white,
            Dev.Logging.LogLevel.Warn  => new Color(1f, 0.85f, 0.5f),
            Dev.Logging.LogLevel.Error => new Color(1f, 0.6f, 0.6f),
            Dev.Logging.LogLevel.Fatal => new Color(1f, 0.4f, 0.4f),
            _ => Color.white
        };

        private void OnGUI()
        {
            EnsureSettings();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // 層級
                _minLevel = (Dev.Logging.LogLevel) EditorGUILayout.EnumPopup(new GUIContent("Level"), _minLevel, GUILayout.Width(180));

                // 搜尋
                GUILayout.Space(8);
                _search = GUILayout.TextField(_search, EditorStyles.toolbarTextField, GUILayout.MinWidth(120));

                // 折疊
                GUILayout.Space(8);
                _settings.foldDuplicates = GUILayout.Toggle(_settings.foldDuplicates, "Collapse", EditorStyles.toolbarButton, GUILayout.Width(80));

                // 右側：清除
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    _buffer.Clear();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                // 左側：分類勾選
                using (new GUILayout.VerticalScope(GUILayout.Width(160)))
                {
                    GUILayout.Label("Categories", EditorStyles.boldLabel);
                    var cats = _settings.knownCategories;
                    var toAdd = new List<string>();
                    var toDel = new List<string>();

                    foreach (var c in cats)
                    {
                        bool sel = _selectedCats.Contains(c);
                        bool next = EditorGUILayout.ToggleLeft(c, sel);
                        if (next && !sel) toAdd.Add(c);
                        else if (!next && sel) toDel.Add(c);
                    }
                    foreach (var a in toAdd) _selectedCats.Add(a);
                    foreach (var d in toDel) _selectedCats.Remove(d);
                }

                // 右側：列表
                using (var scroll = new GUILayout.ScrollViewScope(_scroll))
                {
                    _scroll = scroll.scrollPosition;
                    for (int i = 0; i < _buffer.Count; i++)
                    {
                        var e = _buffer[i];
                        if (!PassFilters(e)) continue;

                        var col = LevelColor(e.level);
                        var prev = GUI.color; GUI.color = col;

                        using (new GUILayout.HorizontalScope("box"))
                        {
                            GUILayout.Label(e.timeUtc.ToLocalTime().ToString("HH:mm:ss.fff"), GUILayout.Width(90));
                            GUILayout.Label($"[{e.level}]", GUILayout.Width(60));
                            GUILayout.Label($"[{e.category}]", GUILayout.Width(120));
                            GUILayout.Label(e.message, GUILayout.ExpandWidth(true));
                            if (e.count > 1)
                                GUILayout.Label($"×{e.count}", GUILayout.Width(36));
                        }

                        GUI.color = prev;
                    }
                }
            }
        }
    }
}
#endif