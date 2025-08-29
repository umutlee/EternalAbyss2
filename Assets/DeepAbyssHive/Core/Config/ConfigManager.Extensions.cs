using UnityEngine;

namespace DeepAbyssHive.Core.Config
{
    /// <summary>
    /// ConfigManager 相容性擴充方法
    /// </summary>
    public static class ConfigManagerExtensions
    {
        /// <summary>
        /// 獲取配置物件，找不到時創建空的 ScriptableObject 避免 NRE
        /// </summary>
        /// <typeparam name="T">配置物件類型</typeparam>
        /// <param name="manager">ConfigManager 實例</param>
        /// <returns>配置物件實例</returns>
        /// <remarks>
        /// 最小回退機制：尋找名為 typeof(T).Name 的資源；找不到則新建一個空 ScriptableObject 以避免 NRE。
        /// 後續可改為導到正式的設定倉儲。
        /// </remarks>
        public static T GetConfig<T>(this ConfigManager manager) where T : ScriptableObject
        {
            // 期望資源命名即型別名，例如 UnitConfigSO => "UnitConfigSO"
            var asset = Resources.Load<T>(typeof(T).Name);
            if (asset != null) return asset;
            
            // 找不到資源時創建空實例避免 NRE
            return ScriptableObject.CreateInstance<T>();
        }

        /// <summary>
        /// 獲取配置物件或返回預設值
        /// </summary>
        /// <typeparam name="T">配置物件類型</typeparam>
        /// <param name="manager">ConfigManager 實例</param>
        /// <param name="defaultValue">預設值</param>
        /// <returns>配置物件或預設值</returns>
        public static T GetConfigOrDefault<T>(this ConfigManager manager, T defaultValue = null)
            where T : ScriptableObject
        {
            var cfg = manager.GetConfig<T>();
            return cfg != null ? cfg : defaultValue;
        }

        /// <summary>
        /// 嘗試獲取配置物件
        /// </summary>
        /// <typeparam name="T">配置物件類型</typeparam>
        /// <param name="manager">ConfigManager 實例</param>
        /// <param name="config">輸出的配置物件</param>
        /// <returns>是否成功獲取</returns>
        public static bool TryGetConfig<T>(this ConfigManager manager, out T config)
            where T : ScriptableObject
        {
            config = manager.GetConfig<T>();
            return config != null;
        }
    }
}