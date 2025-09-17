using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using DeepAbyssHive.Core.Config;

namespace DeepAbyssHive.Core.Services.Config
{
    /// <summary>
    /// 統一的 GameConfig 取得入口，避免直接依賴不存在的靜態屬性（如 GameConfigSO.Current）。
    /// 優先順序：Resources 路徑 > Provider 反射 > 已載入資產掃描。
    /// </summary>
    internal static class ConfigProviderHooks
    {
        public static GameConfigSO GetConfig()
        {
            // 1) 直接使用既有的 GameConfigProvider.Current（最佳路徑）
            try
            {
                var cfg = GameConfigProvider.Current;
                if (cfg != null) return cfg;
            }
            catch { /* ignore and fallthrough */ }

            // 2) Resources: 建議路徑 Assets/Resources/Configs/GameConfig.asset
            try
            {
                var cfg = Resources.Load<GameConfigSO>("Configs/GameConfig");
                if (cfg != null) return cfg;
            }
            catch { /* ignore and fallthrough */ }

            // 3) 備援：掃描已載入資產（避免硬性失敗）
            try
            {
                var any = Resources.FindObjectsOfTypeAll<GameConfigSO>().FirstOrDefault();
                if (any != null) return any;
            }
            catch { /* ignore */ }

            return null; // 容忍為 null，呼叫端應處理（例如只列印 NOT_FOUND）。
        }
    }
}