using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

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
            // 1) Resources: 建議路徑 Assets/Resources/Configs/GameConfig.asset
            var cfg = Resources.Load<GameConfigSO>("Configs/GameConfig");
            if (cfg != null) return cfg;

            // 2) 透過 Provider 嘗試（容忍不同命名）
            try
            {
                var providerType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a =>
                    {
                        try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                    })
                    .FirstOrDefault(t => t.Name.Contains("GameConfigProvider", StringComparison.OrdinalIgnoreCase));

                if (providerType != null)
                {
                    var instance =
                        providerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) ??
                        providerType.GetProperty("Current", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);

                    if (instance != null)
                    {
                        var configProp = providerType.GetProperty("Config", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (configProp != null)
                        {
                            var viaProvider = configProp.GetValue(instance) as GameConfigSO;
                            if (viaProvider != null) return viaProvider;
                        }
                    }

                    // 少數情況：直接靜態 Config
                    var staticCfg = providerType.GetProperty("Config", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) as GameConfigSO;
                    if (staticCfg != null) return staticCfg;
                }
            }
            catch { /* ignore and fallthrough */ }

            // 3) 備援：掃描已載入資產（避免硬性失敗）。
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