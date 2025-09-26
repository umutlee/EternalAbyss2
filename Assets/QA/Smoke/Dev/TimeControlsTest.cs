using UnityEngine;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Core.Services;

namespace DeepAbyssHive.QA.Smoke.Dev
{
    /// <summary>
    /// 簡單的時間控制測試腳本
    /// </summary>
    public class TimeControlsTest : MonoBehaviour
    {
        private ITimeService _timeService;
        private readonly float[] _timeScales = { 1f, 2f, 4f };
        private int _currentTimeScaleIndex = 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindObjectOfType<TimeControlsTest>() != null) return;
            
            var go = new GameObject("TimeControlsTest");
            go.AddComponent<TimeControlsTest>();
            DontDestroyOnLoad(go);
            
            Debug.Log("[TimeControlsTest] Created - Space=Pause, T=TimeScale");
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
                _timeService = ServiceManager.Instance?.GetService<ITimeService>();
                if (_timeService != null)
                {
                    Debug.Log("[TimeControlsTest] TimeService initialized successfully");
                }
                else
                {
                    Debug.LogWarning("[TimeControlsTest] TimeService is null");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TimeControlsTest] Failed to get TimeService: {ex.Message}");
            }
        }

        private void Update()
        {
            var config = GameConfigProvider.Current;
            if (config == null) return;

            // 暫停/恢復 (Space)
            if (config.pauseToggleKey != KeyCode.None && Input.GetKeyDown(config.pauseToggleKey))
            {
                if (_timeService != null)
                {
                    _timeService.TogglePause();
                    Debug.Log($"[TimeControlsTest] Pause toggled - IsPaused: {_timeService.IsPaused}");
                }
                else
                {
                    // 直接控制 Unity 的 timeScale 作為回退
                    Time.timeScale = Time.timeScale == 0f ? 1f : 0f;
                    Debug.Log($"[TimeControlsTest] Direct timeScale toggle - TimeScale: {Time.timeScale}");
                }
            }

            // 時間倍率循環 (T)
            if (config.timeScaleCycleKey != KeyCode.None && Input.GetKeyDown(config.timeScaleCycleKey))
            {
                _currentTimeScaleIndex = (_currentTimeScaleIndex + 1) % _timeScales.Length;
                var newScale = _timeScales[_currentTimeScaleIndex];
                
                if (_timeService != null)
                {
                    _timeService.SetTimeScale(newScale);
                    Debug.Log($"[TimeControlsTest] TimeScale set via service: {newScale}x");
                }
                else
                {
                    // 直接控制 Unity 的 timeScale 作為回退
                    Time.timeScale = newScale;
                    Debug.Log($"[TimeControlsTest] Direct timeScale set: {newScale}x");
                }
            }
        }


    }
}