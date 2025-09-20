using System;
using DeepAbyssHive.Core.Services;

namespace DeepAbyssHive.Core.Interfaces
{
    /// <summary>
    /// 時間服務接口 - 統一管理遊戲時間控制
    /// </summary>
    public interface ITimeService : DeepAbyssHive.Core.Services.IService
    {
        /// <summary>
        /// 當前時間倍率（1.0 = 正常速度）
        /// </summary>
        float TimeScale { get; }
        
        /// <summary>
        /// 是否暫停
        /// </summary>
        bool IsPaused { get; }
        
        /// <summary>
        /// 設置時間倍率
        /// </summary>
        void SetTimeScale(float scale);
        
        /// <summary>
        /// 暫停/恢復遊戲
        /// </summary>
        void SetPaused(bool paused);
        
        /// <summary>
        /// 切換暫停狀態
        /// </summary>
        void TogglePause();
        
        /// <summary>
        /// 時間倍率變更事件
        /// </summary>
        event Action<float> OnTimeScaleChanged;
        
        /// <summary>
        /// 暫停狀態變更事件
        /// </summary>
        event Action<bool> OnPauseStateChanged;
    }
}