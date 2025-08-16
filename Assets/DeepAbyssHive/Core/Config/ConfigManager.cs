using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepAbyssHive.Core.Config
{
    /// <summary>
    /// 配置管理器
    /// 负责加载、缓存和管理所有ScriptableObject配置
    /// </summary>
    public class ConfigManager : MonoBehaviour
    {
        [Header("配置路径")]
        [SerializeField] private string _configResourcePath = "Configs";
        
        [Header("调试选项")]
        [SerializeField] private bool _enableDebugLog = false;
        [SerializeField] private bool _enableHotReload = true;
        
        // 配置缓存
        private Dictionary<Type, BaseConfigSO> _configCache = new Dictionary<Type, BaseConfigSO>();
        private Dictionary<string, BaseConfigSO> _configByName = new Dictionary<string, BaseConfigSO>();
        
        // 单例
        private static ConfigManager _instance;
        public static ConfigManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ConfigManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ConfigManager");
                        _instance = go.AddComponent<ConfigManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeConfigs();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 初始化所有配置
        /// </summary>
        private void InitializeConfigs()
        {
            if (_enableDebugLog)
            {
                Debug.Log("[ConfigManager] 开始初始化配置系统");
            }

            // 加载所有配置文件
            LoadAllConfigs();
            
            if (_enableDebugLog)
            {
                Debug.Log($"[ConfigManager] 配置系统初始化完成，共加载 {_configCache.Count} 个配置");
            }
        }

        /// <summary>
        /// 加载所有配置文件
        /// </summary>
        private void LoadAllConfigs()
        {
            // 从Resources文件夹加载所有配置
            BaseConfigSO[] configs = Resources.LoadAll<BaseConfigSO>(_configResourcePath);
            
            foreach (var config in configs)
            {
                if (config != null)
                {
                    RegisterConfig(config);
                }
            }
        }

        /// <summary>
        /// 注册配置到缓存
        /// </summary>
        /// <param name="config">配置对象</param>
        private void RegisterConfig(BaseConfigSO config)
        {
            Type configType = config.GetType();
            
            // 验证配置
            if (!config.ValidateConfig())
            {
                Debug.LogError($"[ConfigManager] 配置验证失败: {configType.Name}");
                return;
            }
            
            // 添加到缓存
            _configCache[configType] = config;
            _configByName[config.ConfigName] = config;
            
            if (_enableDebugLog)
            {
                Debug.Log($"[ConfigManager] 已注册配置: {configType.Name} ({config.ConfigName})");
            }
        }

        /// <summary>
        /// 获取指定类型的配置
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <returns>配置对象，如果不存在则返回null</returns>
        public T GetConfig<T>() where T : BaseConfigSO
        {
            Type configType = typeof(T);
            
            if (_configCache.TryGetValue(configType, out BaseConfigSO config))
            {
                return config as T;
            }
            
            // 尝试从Resources加载
            T loadedConfig = TryLoadConfig<T>();
            if (loadedConfig != null)
            {
                RegisterConfig(loadedConfig);
                return loadedConfig;
            }
            
            if (_enableDebugLog)
            {
                Debug.LogWarning($"[ConfigManager] 未找到配置: {configType.Name}");
            }
            
            return null;
        }

        /// <summary>
        /// 获取指定类型的配置，如果不存在则创建默认配置
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <returns>配置对象</returns>
        public T GetConfigOrDefault<T>() where T : BaseConfigSO
        {
            T config = GetConfig<T>();
            
            if (config == null)
            {
                // 创建默认配置
                config = CreateDefaultConfig<T>();
                if (config != null)
                {
                    RegisterConfig(config);
                }
            }
            
            return config;
        }

        /// <summary>
        /// 根据名称获取配置
        /// </summary>
        /// <param name="configName">配置名称</param>
        /// <returns>配置对象</returns>
        public BaseConfigSO GetConfigByName(string configName)
        {
            _configByName.TryGetValue(configName, out BaseConfigSO config);
            return config;
        }

        /// <summary>
        /// 尝试从Resources加载配置
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <returns>配置对象</returns>
        private T TryLoadConfig<T>() where T : BaseConfigSO
        {
            string configPath = $"{_configResourcePath}/{typeof(T).Name}";
            return Resources.Load<T>(configPath);
        }

        /// <summary>
        /// 创建默认配置
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <returns>默认配置对象</returns>
        private T CreateDefaultConfig<T>() where T : BaseConfigSO
        {
            try
            {
                T config = ScriptableObject.CreateInstance<T>();
                config.ApplyDefaults();
                
                if (_enableDebugLog)
                {
                    Debug.Log($"[ConfigManager] 已创建默认配置: {typeof(T).Name}");
                }
                
                return config;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConfigManager] 创建默认配置失败: {typeof(T).Name}, 错误: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 重新加载所有配置（热重载）
        /// </summary>
        public void ReloadAllConfigs()
        {
            if (!_enableHotReload) return;
            
            if (_enableDebugLog)
            {
                Debug.Log("[ConfigManager] 开始热重载所有配置");
            }
            
            // 清空缓存
            _configCache.Clear();
            _configByName.Clear();
            
            // 重新加载
            LoadAllConfigs();
            
            // 通知所有配置重载完成
            foreach (var config in _configCache.Values)
            {
                config.OnConfigReloaded();
            }
        }

        /// <summary>
        /// 获取所有已加载的配置
        /// </summary>
        /// <returns>配置列表</returns>
        public List<BaseConfigSO> GetAllConfigs()
        {
            return new List<BaseConfigSO>(_configCache.Values);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器专用：强制重新加载配置
        /// </summary>
        [ContextMenu("重新加载所有配置")]
        public void EditorReloadConfigs()
        {
            ReloadAllConfigs();
        }
#endif
    }
}