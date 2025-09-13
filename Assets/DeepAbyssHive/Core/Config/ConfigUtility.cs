using UnityEngine;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Core.Config
{
    /// <summary>
    /// 配置工具类
    /// 提供便捷的配置访问方法
    /// </summary>
    public static class ConfigUtility
    {
        /// <summary>
        /// 快速获取配置
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <returns>配置对象</returns>
        public static T GetConfig<T>() where T : BaseConfigSO
        {
            return ConfigManager.Instance.GetConfig<T>();
        }

        /// <summary>
        /// 快速获取配置或默认值
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <returns>配置对象</returns>
        public static T GetConfigOrDefault<T>() where T : BaseConfigSO
        {
            return ConfigManager.Instance.GetConfigOrDefault<T>();
        }

        /// <summary>
        /// 检查配置是否存在
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <returns>是否存在</returns>
        public static bool HasConfig<T>() where T : BaseConfigSO
        {
            return GetConfig<T>() != null;
        }

        /// <summary>
        /// 安全获取配置值
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="valueGetter">值获取函数</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>配置值或默认值</returns>
        public static TValue GetConfigValue<T, TValue>(System.Func<T, TValue> valueGetter, TValue defaultValue) 
            where T : BaseConfigSO
        {
            T config = GetConfig<T>();
            if (config != null)
            {
                try
                {
                    return valueGetter(config);
                }
                catch (System.Exception e)
                {
                    DAHLog.Error(LogCategory.CONFIG, $"[ConfigUtility] 获取配置值失败: {typeof(T).Name}, 错误: {e.Message}");
                }
            }
            
            return defaultValue;
        }
    }
}