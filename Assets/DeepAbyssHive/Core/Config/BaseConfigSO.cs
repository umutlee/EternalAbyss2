using UnityEngine;

namespace DeepAbyssHive.Core.Config
{
    /// <summary>
    /// 配置ScriptableObject基类
    /// 提供配置验证、热重载和默认值支持
    /// </summary>
    public abstract class BaseConfigSO : ScriptableObject
    {
        [Header("配置信息")]
        [SerializeField] private string _configName;
        [SerializeField] private string _version = "1.0";
        [SerializeField] private string _description;
        
        [Header("调试选项")]
        [SerializeField] private bool _enableDebugLog = false;
        [SerializeField] private bool _enableValidation = true;

        /// <summary>
        /// 配置名称
        /// </summary>
        public string ConfigName => _configName;
        
        /// <summary>
        /// 配置版本
        /// </summary>
        public string Version => _version;
        
        /// <summary>
        /// 配置描述
        /// </summary>
        public string Description => _description;
        
        /// <summary>
        /// 是否启用调试日志
        /// </summary>
        public bool EnableDebugLog => _enableDebugLog;

        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        /// <returns>验证结果</returns>
        public virtual bool ValidateConfig()
        {
            if (!_enableValidation) return true;
            
            bool isValid = OnValidateConfig();
            
            if (_enableDebugLog)
            {
                Debug.Log($"[{GetType().Name}] 配置验证结果: {(isValid ? "通过" : "失败")}");
            }
            
            return isValid;
        }

        /// <summary>
        /// 子类实现具体的验证逻辑
        /// </summary>
        /// <returns>验证结果</returns>
        protected virtual bool OnValidateConfig()
        {
            return true;
        }

        /// <summary>
        /// 应用默认值（当配置不存在时使用）
        /// </summary>
        public virtual void ApplyDefaults()
        {
            OnApplyDefaults();
            
            if (_enableDebugLog)
            {
                Debug.Log($"[{GetType().Name}] 已应用默认配置");
            }
        }

        /// <summary>
        /// 子类实现具体的默认值设置逻辑
        /// </summary>
        protected virtual void OnApplyDefaults()
        {
            // 子类重写此方法设置默认值
        }

        /// <summary>
        /// 配置热重载回调
        /// </summary>
        public virtual void OnConfigReloaded()
        {
            if (_enableDebugLog)
            {
                Debug.Log($"[{GetType().Name}] 配置已重新加载");
            }
        }

        /// <summary>
        /// Unity编辑器验证回调
        /// </summary>
        protected virtual void OnValidate()
        {
            if (_enableValidation && Application.isPlaying)
            {
                ValidateConfig();
            }
        }
    }
}