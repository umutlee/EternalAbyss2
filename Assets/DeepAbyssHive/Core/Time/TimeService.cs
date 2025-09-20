using System;
using UnityEngine;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Core.Time
{
    /// <summary>
    /// 時間服務實現 - 統一管理遊戲時間控制
    /// </summary>
    public class TimeService : ITimeService, IService
    {
        private float _timeScale = 1.0f;
        private bool _isPaused = false;
        
        public float TimeScale => _timeScale;
        public bool IsPaused => _isPaused;
        
        public event Action<float> OnTimeScaleChanged;
        public event Action<bool> OnPauseStateChanged;
        
        public void SetTimeScale(float scale)
        {
            if (scale < 0) scale = 0;
            if (Math.Abs(_timeScale - scale) < 0.001f) return;
            
            _timeScale = scale;
            UpdateUnityTimeScale();
            OnTimeScaleChanged?.Invoke(_timeScale);
            
            DAHLog.Info(LogCategory.SERVICE, $"[TimeService] TimeScale set to {_timeScale:F2}x");
        }
        
        public void SetPaused(bool paused)
        {
            if (_isPaused == paused) return;
            
            _isPaused = paused;
            UpdateUnityTimeScale();
            OnPauseStateChanged?.Invoke(_isPaused);
            
            DAHLog.Info(LogCategory.SERVICE, $"[TimeService] Game {(_isPaused ? "paused" : "resumed")}");
        }
        
        public void TogglePause()
        {
            SetPaused(!_isPaused);
        }
        
        private void UpdateUnityTimeScale()
        {
            UnityEngine.Time.timeScale = _isPaused ? 0f : _timeScale;
        }
    }
}