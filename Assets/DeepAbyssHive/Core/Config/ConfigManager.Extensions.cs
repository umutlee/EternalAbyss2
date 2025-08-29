using System;
using UnityEngine;

namespace DeepAbyssHive.Core.Config
{
    /// <summary>
    /// ConfigManager 擴展方法
    /// </summary>
    public static class ConfigManagerExtensions
    {
        /// <summary>
        /// 獲取配置值，如果不存在則返回預設值
        /// </summary>
        public static T GetConfigValue<T>(this ConfigManager manager, string key, T defaultValue = default(T))
        {
            if (manager == null) return defaultValue;
            
            // TODO: 實作配置值獲取邏輯
            return defaultValue;
        }

        /// <summary>
        /// 設置配置值
        /// </summary>
        public static void SetConfigValue<T>(this ConfigManager manager, string key, T value)
        {
            if (manager == null) return;
            
            // TODO: 實作配置值設置邏輯
        }

        /// <summary>
        /// 檢查配置鍵是否存在
        /// </summary>
        public static bool HasConfig(this ConfigManager manager, string key)
        {
            if (manager == null) return false;
            
            // TODO: 實作配置鍵檢查邏輯
            return false;
        }
    }
}