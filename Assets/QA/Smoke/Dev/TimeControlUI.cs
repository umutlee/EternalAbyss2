using UnityEngine;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Core.Services;

namespace DeepAbyssHive.QA.Smoke.Dev
{
    /// <summary>
    /// Time Control UI - 提供 Play/Pause 和速度控制按鈕
    /// </summary>
    public class TimeControlUI : MonoBehaviour
    {
        private ITimeService _timeService;
        private readonly float[] _timeScales = { 1f, 2f, 4f };
        private int _currentTimeScaleIndex = 0;
        private Rect _windowRect = new Rect(Screen.width - 250, 10, 230, 160);
        private bool _showUI = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindObjectOfType<TimeControlUI>() != null) return;
            
            var go = new GameObject("TimeControlUI");
            go.AddComponent<TimeControlUI>();
            DontDestroyOnLoad(go);
            
            Debug.Log("[TimeControlUI] Created");
        }

        private void Start()
        {
            // 延遲初始化，確保 ServiceManager 已經準備好
            Invoke(nameof(InitializeTimeService), 1f);
        }

        private void InitializeTimeService()
        {
            try
            {
                // 檢查 ServiceManager 是否存在
                if (ServiceManager.Instance == null)
                {
                    Debug.LogWarning("[TimeControlUI] ServiceManager.Instance is null");
                    return;
                }

                _timeService = ServiceManager.Instance.GetService<ITimeService>();
                if (_timeService != null)
                {
                    Debug.Log("[TimeControlUI] TimeService initialized successfully");
                }
                else
                {
                    Debug.LogWarning("[TimeControlUI] TimeService is null, using direct Unity timeScale control");
                    
                    // 嘗試手動創建 TimeService
                    _timeService = new TimeService();
                    Debug.Log("[TimeControlUI] Created TimeService manually");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TimeControlUI] Failed to get TimeService: {ex.Message}");
                Debug.Log("[TimeControlUI] Will use direct Unity timeScale control");
            }
        }

        private void Update()
        {
            // 保持鍵盤快捷鍵功能
            var config = GameConfigProvider.Current;
            if (config == null) return;

            // Space - 暫停/恢復
            if (config.pauseToggleKey != KeyCode.None && Input.GetKeyDown(config.pauseToggleKey))
            {
                Debug.Log($"[TimeControlUI] Pause key pressed: {config.pauseToggleKey}");
                TogglePause();
            }

            // T - 時間倍率循環
            if (config.timeScaleCycleKey != KeyCode.None && Input.GetKeyDown(config.timeScaleCycleKey))
            {
                Debug.Log($"[TimeControlUI] TimeScale key pressed: {config.timeScaleCycleKey}");
                CycleTimeScale();
            }

            // 額外調試：直接檢測 Space 和 T 鍵
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log($"[TimeControlUI] Space key detected, config.pauseToggleKey = {config.pauseToggleKey}");
            }
            if (Input.GetKeyDown(KeyCode.T))
            {
                Debug.Log($"[TimeControlUI] T key detected, config.timeScaleCycleKey = {config.timeScaleCycleKey}");
            }

            // F1 - 切換 UI 顯示
            if (Input.GetKeyDown(KeyCode.F1))
            {
                _showUI = !_showUI;
            }
        }

        private void OnGUI()
        {
            if (!_showUI) return;

            _windowRect = GUI.Window(12345, _windowRect, DrawTimeControlWindow, "Time Control");
        }

        private void DrawTimeControlWindow(int windowID)
        {
            GUILayout.BeginVertical(GUILayout.Width(210), GUILayout.Height(140));

            // 當前狀態顯示
            var timeScale = GetCurrentTimeScale();
            var isPaused = GetCurrentPauseState();
            
            GUILayout.Label($"Status: {(isPaused ? "Paused" : $"{timeScale:F1}x")}", GUI.skin.box, GUILayout.Height(25));

            // Play/Pause 按鈕
            if (isPaused)
            {
                if (GUILayout.Button("▶ Play", GUILayout.Height(35)))
                {
                    SetPaused(false);
                }
            }
            else
            {
                if (GUILayout.Button("⏸ Pause", GUILayout.Height(35)))
                {
                    SetPaused(true);
                }
            }

            // 速度按鈕
            GUILayout.Label("Speed:", GUILayout.Height(20));
            GUILayout.BeginHorizontal();
            
            for (int i = 0; i < _timeScales.Length; i++)
            {
                var scale = _timeScales[i];
                var isSelected = !isPaused && Mathf.Approximately(timeScale, scale);
                
                var oldColor = GUI.backgroundColor;
                if (isSelected)
                    GUI.backgroundColor = Color.yellow;
                
                if (GUILayout.Button($"{scale:F0}x", GUILayout.Height(30)))
                {
                    SetTimeScale(scale);
                    _currentTimeScaleIndex = i;
                }
                
                GUI.backgroundColor = oldColor;
            }
            
            GUILayout.EndHorizontal();
            
            // 提示信息
            GUILayout.Label("F1: Toggle UI", GUI.skin.label, GUILayout.Height(15));

            GUILayout.EndVertical();

            // 允許拖動窗口
            GUI.DragWindow();
        }

        private void TogglePause()
        {
            var isPaused = GetCurrentPauseState();
            SetPaused(!isPaused);
        }

        private void CycleTimeScale()
        {
            if (GetCurrentPauseState())
            {
                SetPaused(false);
            }
            
            _currentTimeScaleIndex = (_currentTimeScaleIndex + 1) % _timeScales.Length;
            SetTimeScale(_timeScales[_currentTimeScaleIndex]);
        }

        private void SetPaused(bool paused)
        {
            if (_timeService != null)
            {
                _timeService.SetPaused(paused);
                Debug.Log($"[TimeControlUI] Pause set via service: {paused}");
            }
            else
            {
                Time.timeScale = paused ? 0f : _timeScales[_currentTimeScaleIndex];
                Debug.Log($"[TimeControlUI] Direct pause set: {paused}, timeScale: {Time.timeScale}");
            }
        }

        private void SetTimeScale(float scale)
        {
            if (_timeService != null)
            {
                _timeService.SetTimeScale(scale);
                Debug.Log($"[TimeControlUI] TimeScale set via service: {scale}x");
            }
            else
            {
                Time.timeScale = scale;
                Debug.Log($"[TimeControlUI] Direct timeScale set: {scale}x");
            }
        }

        private float GetCurrentTimeScale()
        {
            return _timeService?.TimeScale ?? Time.timeScale;
        }

        private bool GetCurrentPauseState()
        {
            return _timeService?.IsPaused ?? (Time.timeScale == 0f);
        }
    }
}