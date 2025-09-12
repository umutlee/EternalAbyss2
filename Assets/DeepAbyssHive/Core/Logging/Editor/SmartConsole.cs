using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace DeepAbyssHive.Core.Logging.Editor
{
    /// <summary>
    /// Smart Console Editor 視窗。支援分類過濾、Solo 模式、搜尋、摺疊。
    /// 快捷鍵：Ctrl+Alt+L
    /// </summary>
    public class SmartConsole : EditorWindow
    {
        [MenuItem("DeepAbyssHive/Smart Console %&l")]
        public static void ShowWindow()
        {
            var window = GetWindow<SmartConsole>("Smart Console");
            window.Show();
        }

        // 日誌條目
        [Serializable]
        public class LogEntry
        {
            public string fullMessage;
            public LogType type;
            public LogCategory category;
            public DateTime timestamp;
            public int frameCount;
            public string cleanMessage; // 去除前綴的純訊息
            public int duplicateCount = 1;
        }

        // 過濾器狀態
        private Dictionary<LogCategory, bool> _categoryFilters = new Dictionary<LogCategory, bool>();
        private Dictionary<LogType, bool> _typeFilters = new Dictionary<LogType, bool>();
        private LogCategory? _soloCategory = null;
        private string _searchText = "";
        private bool _collapseMode = true;
        private bool _autoScroll = true;

        // 日誌資料
        private List<LogEntry> _allLogs = new List<LogEntry>();
        private List<LogEntry> _filteredLogs = new List<LogEntry>();
        private Vector2 _scrollPosition;

        // 樣式
        private GUIStyle _logStyle;
        private GUIStyle _categoryButtonStyle;

        // 正則解析 DAHLog 格式：[CAT][HH:mm:ss.fff][frame] message
        private static readonly Regex LogPattern = new Regex(@"^\[(\w+)\]\[(\d{2}:\d{2}:\d{2}\.\d{3})\](?:\[(\d+)\])?\s*(.*)$");

        void OnEnable()
        {
            // 初始化過濾器
            foreach (LogCategory cat in Enum.GetValues(typeof(LogCategory)))
            {
                if (!_categoryFilters.ContainsKey(cat))
                    _categoryFilters[cat] = true;
            }

            foreach (LogType type in Enum.GetValues(typeof(LogType)))
            {
                if (!_typeFilters.ContainsKey(type))
                    _typeFilters[type] = true;
            }

            // 訂閱 Unity Console
            Application.logMessageReceived += OnLogMessageReceived;
            
            RefreshFilteredLogs();
        }

        void OnDisable()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
        }

        void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            var entry = ParseLogEntry(logString, type);
            if (entry != null)
            {
                // 摺疊模式：檢查是否與最後一條相同
                if (_collapseMode && _allLogs.Count > 0)
                {
                    var last = _allLogs[_allLogs.Count - 1];
                    if (last.cleanMessage == entry.cleanMessage && last.type == entry.type && last.category == entry.category)
                    {
                        last.duplicateCount++;
                        RefreshFilteredLogs();
                        return;
                    }
                }

                _allLogs.Add(entry);
                RefreshFilteredLogs();
            }
        }

        LogEntry ParseLogEntry(string logString, LogType type)
        {
            var match = LogPattern.Match(logString);
            if (match.Success)
            {
                // DAHLog 格式
                if (Enum.TryParse<LogCategory>(match.Groups[1].Value, out var category))
                {
                    var timeStr = match.Groups[2].Value;
                    var frameStr = match.Groups[3].Value;
                    var message = match.Groups[4].Value;

                    return new LogEntry
                    {
                        fullMessage = logString,
                        type = type,
                        category = category,
                        timestamp = DateTime.Now, // 簡化：使用當前時間
                        frameCount = int.TryParse(frameStr, out var frame) ? frame : 0,
                        cleanMessage = message
                    };
                }
            }

            // 非 DAHLog 格式，歸類為 SYSTEM
            return new LogEntry
            {
                fullMessage = logString,
                type = type,
                category = LogCategory.SYSTEM,
                timestamp = DateTime.Now,
                frameCount = Time.frameCount,
                cleanMessage = logString
            };
        }

        void RefreshFilteredLogs()
        {
            _filteredLogs.Clear();

            foreach (var log in _allLogs)
            {
                // Solo 模式過濾
                if (_soloCategory.HasValue && log.category != _soloCategory.Value)
                    continue;

                // 分類過濾
                if (!_categoryFilters.GetValueOrDefault(log.category, true))
                    continue;

                // 類型過濾
                if (!_typeFilters.GetValueOrDefault(log.type, true))
                    continue;

                // 搜尋過濾
                if (!string.IsNullOrEmpty(_searchText) && 
                    !log.cleanMessage.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                _filteredLogs.Add(log);
            }

            Repaint();
        }

        void OnGUI()
        {
            InitStyles();
            DrawToolbar();
            DrawLogList();
        }

        void InitStyles()
        {
            if (_logStyle == null)
            {
                _logStyle = new GUIStyle(EditorStyles.label)
                {
                    wordWrap = true,
                    richText = true
                };
            }

            if (_categoryButtonStyle == null)
            {
                _categoryButtonStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    fixedWidth = 60
                };
            }
        }

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 清除按鈕
            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                _allLogs.Clear();
                RefreshFilteredLogs();
            }

            GUILayout.Space(10);

            // 摺疊模式
            var newCollapse = GUILayout.Toggle(_collapseMode, "Collapse", EditorStyles.toolbarButton);
            if (newCollapse != _collapseMode)
            {
                _collapseMode = newCollapse;
                RefreshFilteredLogs();
            }

            // 自動滾動
            _autoScroll = GUILayout.Toggle(_autoScroll, "Auto Scroll", EditorStyles.toolbarButton);

            GUILayout.FlexibleSpace();

            // 搜尋框
            GUILayout.Label("Search:", GUILayout.Width(50));
            var newSearch = GUILayout.TextField(_searchText, EditorStyles.toolbarTextField, GUILayout.Width(200));
            if (newSearch != _searchText)
            {
                _searchText = newSearch;
                RefreshFilteredLogs();
            }

            EditorGUILayout.EndHorizontal();

            // 分類過濾器
            DrawCategoryFilters();

            // 類型過濾器
            DrawTypeFilters();
        }

        void DrawCategoryFilters()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Categories:", GUILayout.Width(70));

            // Solo 模式按鈕
            if (_soloCategory.HasValue)
            {
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button($"Solo: {_soloCategory.Value}", _categoryButtonStyle))
                {
                    _soloCategory = null;
                    RefreshFilteredLogs();
                }
                GUI.backgroundColor = Color.white;
            }

            foreach (LogCategory cat in Enum.GetValues(typeof(LogCategory)))
            {
                var isActive = _categoryFilters.GetValueOrDefault(cat, true);
                var isSolo = _soloCategory == cat;

                if (isSolo) GUI.backgroundColor = Color.yellow;
                else if (isActive) GUI.backgroundColor = Color.green;
                else GUI.backgroundColor = Color.gray;

                if (GUILayout.Button(cat.ToString(), _categoryButtonStyle))
                {
                    if (Event.current.control) // Ctrl+Click = Solo
                    {
                        _soloCategory = _soloCategory == cat ? null : cat;
                    }
                    else
                    {
                        _categoryFilters[cat] = !isActive;
                    }
                    RefreshFilteredLogs();
                }
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        void DrawTypeFilters()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Types:", GUILayout.Width(70));

            foreach (LogType type in Enum.GetValues(typeof(LogType)))
            {
                var isActive = _typeFilters.GetValueOrDefault(type, true);
                GUI.backgroundColor = isActive ? Color.green : Color.gray;

                if (GUILayout.Button(type.ToString(), _categoryButtonStyle))
                {
                    _typeFilters[type] = !isActive;
                    RefreshFilteredLogs();
                }
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        void DrawLogList()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var log in _filteredLogs)
            {
                DrawLogEntry(log);
            }

            // 自動滾動到底部
            if (_autoScroll && Event.current.type == EventType.Repaint)
            {
                _scrollPosition.y = float.MaxValue;
            }

            EditorGUILayout.EndScrollView();

            // 狀態列
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label($"Showing: {_filteredLogs.Count} / {_allLogs.Count}");
            if (_soloCategory.HasValue)
            {
                GUILayout.Label($"Solo: {_soloCategory.Value}", EditorStyles.boldLabel);
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawLogEntry(LogEntry log)
        {
            var color = GetLogColor(log.type);
            var duplicateText = log.duplicateCount > 1 ? $" ({log.duplicateCount})" : "";
            var displayText = $"<color={color}>[{log.category}] {log.cleanMessage}{duplicateText}</color>";

            EditorGUILayout.SelectableLabel(displayText, _logStyle, GUILayout.MinHeight(20));
        }

        string GetLogColor(LogType type)
        {
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                    return "#ff4444";
                case LogType.Warning:
                    return "#ffaa00";
                case LogType.Assert:
                    return "#ff8800";
                default:
                    return "#ffffff";
            }
        }
    }
}