using UnityEngine;
using DeepAbyssHive.Core.Config;

namespace DeepAbyssHive.QA.Smoke.Dev
{
    /// <summary>
    /// [EA-M4-T09|2025-09-11] 快捷鍵提示 HUD
    /// - 可拖拽的窗口顯示所有遊戲快捷鍵
    /// - F1 切換顯示/隱藏
    /// - 自動讀取 GameConfig 中的所有熱鍵設定
    /// - 支援窗口位置記憶（PlayerPrefs）
    /// </summary>
    public class KeyHintsHUD : MonoBehaviour
    {
        [Header("HUD Settings")]
        [Tooltip("切換 HUD 顯示的按鍵")]
        public KeyCode toggleKey = KeyCode.F1;
        
        [Tooltip("窗口標題")]
        public string windowTitle = "快捷鍵提示";
        
        [Tooltip("窗口初始寬度")]
        public float windowWidth = 300f;
        
        [Tooltip("窗口初始高度")]
        public float windowHeight = 400f;

        private bool _showHUD = false;
        private Rect _windowRect;
        private Vector2 _scrollPosition;
        private GUIStyle _windowStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _headerStyle;
        
        private const string PREF_KEY_X = "KeyHintsHUD_X";
        private const string PREF_KEY_Y = "KeyHintsHUD_Y";
        private const string PREF_KEY_SHOW = "KeyHintsHUD_Show";

        void Start()
        {
            // 從 PlayerPrefs 載入窗口位置和顯示狀態
            float savedX = PlayerPrefs.GetFloat(PREF_KEY_X, 50f);
            float savedY = PlayerPrefs.GetFloat(PREF_KEY_Y, 50f);
            _showHUD = PlayerPrefs.GetInt(PREF_KEY_SHOW, 0) == 1;
            
            _windowRect = new Rect(savedX, savedY, windowWidth, windowHeight);
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                _showHUD = !_showHUD;
                
                // 儲存顯示狀態
                PlayerPrefs.SetInt(PREF_KEY_SHOW, _showHUD ? 1 : 0);
                
                var cfg = GameConfigProvider.Current;
                bool useVerbose = cfg != null && cfg.devVerboseLogs;
                
                if (useVerbose)
                {
                    Debug.Log($"[KeyHintsHUD] 切換顯示狀態: {(_showHUD ? "顯示" : "隱藏")}");
                }
            }
        }

        void OnGUI()
        {
            if (!_showHUD) return;

            // 初始化 GUI 樣式
            InitializeStyles();
            
            // 繪製可拖拽窗口
            _windowRect = GUI.Window(12345, _windowRect, DrawWindow, windowTitle, _windowStyle);
            
            // 確保窗口在螢幕範圍內
            _windowRect.x = Mathf.Clamp(_windowRect.x, 0, Screen.width - _windowRect.width);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 0, Screen.height - _windowRect.height);
            
            // 儲存窗口位置
            PlayerPrefs.SetFloat(PREF_KEY_X, _windowRect.x);
            PlayerPrefs.SetFloat(PREF_KEY_Y, _windowRect.y);
        }

        private void InitializeStyles()
        {
            if (_windowStyle == null)
            {
                _windowStyle = new GUIStyle(GUI.skin.window);
                _windowStyle.fontSize = 12;
            }
            
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label);
                _labelStyle.fontSize = 11;
                _labelStyle.wordWrap = true;
            }
            
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(GUI.skin.label);
                _headerStyle.fontSize = 12;
                _headerStyle.fontStyle = FontStyle.Bold;
                _headerStyle.normal.textColor = Color.yellow;
            }
        }

        private void DrawWindow(int windowID)
        {
            GUILayout.BeginVertical();
            
            // 滾動區域
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            
            var cfg = GameConfigProvider.Current;
            if (cfg == null)
            {
                GUILayout.Label("GameConfig 未載入", _labelStyle);
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
                GUI.DragWindow();
                return;
            }

            // 建築放置相關
            GUILayout.Label("建築放置", _headerStyle);
            DrawKeyBinding("建築放置切換", cfg.buildPlacerToggleKey);
            DrawKeyBinding("放置測試 (SMOKE)", cfg.placementSmokeKey);
            DrawKeyBinding("刪除建築 (主)", cfg.buildingDeleteKey1);
            DrawKeyBinding("刪除建築 (副)", cfg.buildingDeleteKey2);
            
            GUILayout.Space(10);
            
            // 單位相關
            GUILayout.Label("單位系統", _headerStyle);
            DrawKeyBinding("生成單位", cfg.devUnitsSpawnKey);
            DrawKeyBinding("單位測試", cfg.devUnitsTestKey);
            GUILayout.Label($"生成數量: {cfg.devSpawnCount}", _labelStyle);
            
            GUILayout.Space(10);
            
            // HUD 相關
            GUILayout.Label("介面控制", _headerStyle);
            DrawKeyBinding("快捷鍵提示", toggleKey);
            
            GUILayout.Space(10);
            
            // 系統設定
            GUILayout.Label("系統設定", _headerStyle);
            GUILayout.Label($"右鍵鎖游標: {(cfg.rmbLocksCursor ? "是" : "否")}", _labelStyle);
            GUILayout.Label($"詳細日誌: {(cfg.devVerboseLogs ? "是" : "否")}", _labelStyle);
            GUILayout.Label($"健康監測: {(cfg.healthLogEnabled ? "是" : "否")}", _labelStyle);
            
            GUILayout.Space(10);
            
            // 操作說明
            GUILayout.Label("操作說明", _headerStyle);
            GUILayout.Label("• 拖拽標題列移動窗口", _labelStyle);
            GUILayout.Label("• F1 切換顯示/隱藏", _labelStyle);
            GUILayout.Label("• 位置自動儲存", _labelStyle);
            
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            
            // 允許拖拽窗口
            GUI.DragWindow();
        }

        private void DrawKeyBinding(string description, KeyCode key)
        {
            string keyText = (key == KeyCode.None) ? "未設定" : key.ToString();
            GUILayout.Label($"{description}: {keyText}", _labelStyle);
        }

        void OnApplicationPause(bool pauseStatus)
        {
            // 確保在應用暫停時儲存設定
            if (pauseStatus)
            {
                PlayerPrefs.Save();
            }
        }

        void OnDestroy()
        {
            // 確保在銷毀時儲存設定
            PlayerPrefs.Save();
        }
    }
}