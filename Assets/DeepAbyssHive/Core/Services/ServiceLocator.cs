using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepAbyssHive.Core.Services
{
    /// <summary>
    /// 服務定位器 - 提供依賴注入支援
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private static readonly Dictionary<Type, Func<object>> _factories = new Dictionary<Type, Func<object>>();

        /// <summary>
        /// 註冊服務實例
        /// </summary>
        public static void Register<T>(T service) where T : class
        {
            _services[typeof(T)] = service;
        }

        /// <summary>
        /// 註冊服務工廠
        /// </summary>
        public static void RegisterFactory<T>(Func<T> factory) where T : class
        {
            _factories[typeof(T)] = () => factory();
        }

        /// <summary>
        /// 獲取服務實例
        /// </summary>
        public static T Get<T>() where T : class
        {
            Type serviceType = typeof(T);
            
            // 先檢查已註冊的實例
            if (_services.TryGetValue(serviceType, out object service))
            {
                return service as T;
            }
            
            // 檢查工廠方法
            if (_factories.TryGetValue(serviceType, out Func<object> factory))
            {
                var instance = factory() as T;
                if (instance != null)
                {
                    _services[serviceType] = instance; // 快取實例
                    return instance;
                }
            }
            
            // 嘗試創建預設實例
            try
            {
                var defaultInstance = Activator.CreateInstance<T>();
                _services[serviceType] = defaultInstance;
                return defaultInstance;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ServiceLocator] 無法創建服務實例: {serviceType.Name}, 錯誤: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 檢查服務是否已註冊
        /// </summary>
        public static bool IsRegistered<T>() where T : class
        {
            Type serviceType = typeof(T);
            return _services.ContainsKey(serviceType) || _factories.ContainsKey(serviceType);
        }

        /// <summary>
        /// 移除服務註冊
        /// </summary>
        public static void Unregister<T>() where T : class
        {
            Type serviceType = typeof(T);
            _services.Remove(serviceType);
            _factories.Remove(serviceType);
        }

        /// <summary>
        /// 清除所有服務
        /// </summary>
        public static void Clear()
        {
            _services.Clear();
            _factories.Clear();
        }
    }
}