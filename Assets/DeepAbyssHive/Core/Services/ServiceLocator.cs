using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepAbyssHive.Core.Services
{
    /// <summary>
    /// 全域服務定位器，用於註冊和獲取服務
    /// 提供集中化的服務管理和依賴注入功能
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private static readonly Dictionary<Type, string> _serviceNames = new Dictionary<Type, string>();
        private static bool _isInitialized = false;

        /// <summary>
        /// 註冊服務實例
        /// </summary>
        /// <typeparam name="TService">服務介面類型</typeparam>
        /// <param name="service">服務實例</param>
        /// <param name="serviceName">服務名稱（可選，用於調試）</param>
        public static void Register<TService>(TService service, string serviceName = null)
        {
            var serviceType = typeof(TService);
            
            if (_services.ContainsKey(serviceType))
            {
                Debug.LogWarning($"[ServiceLocator] 服務已存在，將被覆蓋: {serviceType.Name}");
            }

            _services[serviceType] = service;
            _serviceNames[serviceType] = serviceName ?? serviceType.Name;
            
            Debug.Log($"[ServiceLocator] 註冊服務: {_serviceNames[serviceType]} ({serviceType.Name})");
        }

        /// <summary>
        /// 獲取服務實例
        /// </summary>
        /// <typeparam name="TService">服務介面類型</typeparam>
        /// <returns>服務實例</returns>
        /// <exception cref="ServiceNotFoundException">當服務未註冊時拋出</exception>
        public static TService Get<TService>()
        {
            var serviceType = typeof(TService);
            
            if (_services.TryGetValue(serviceType, out object service))
            {
                return (TService)service;
            }
            else
            {
                throw new ServiceNotFoundException($"服務未註冊: {serviceType.Name}");
            }
        }

        /// <summary>
        /// 嘗試獲取服務實例
        /// </summary>
        /// <typeparam name="TService">服務介面類型</typeparam>
        /// <param name="service">輸出的服務實例</param>
        /// <returns>是否成功獲取服務</returns>
        public static bool TryGet<TService>(out TService service)
        {
            var serviceType = typeof(TService);
            
            if (_services.TryGetValue(serviceType, out object serviceObj))
            {
                service = (TService)serviceObj;
                return true;
            }
            
            service = default(TService);
            return false;
        }

        /// <summary>
        /// 檢查服務是否已註冊
        /// </summary>
        /// <typeparam name="TService">服務介面類型</typeparam>
        /// <returns>是否已註冊</returns>
        public static bool IsRegistered<TService>()
        {
            return _services.ContainsKey(typeof(TService));
        }

        /// <summary>
        /// 取消註冊服務
        /// </summary>
        /// <typeparam name="TService">服務介面類型</typeparam>
        /// <returns>是否成功取消註冊</returns>
        public static bool Unregister<TService>()
        {
            var serviceType = typeof(TService);
            
            if (_services.Remove(serviceType))
            {
                _serviceNames.Remove(serviceType);
                Debug.Log($"[ServiceLocator] 取消註冊服務: {serviceType.Name}");
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// 清除所有已註冊的服務
        /// </summary>
        public static void Clear()
        {
            Debug.Log($"[ServiceLocator] 清除所有服務 ({_services.Count} 個)");
            _services.Clear();
            _serviceNames.Clear();
            _isInitialized = false;
        }

        /// <summary>
        /// 獲取所有已註冊的服務類型
        /// </summary>
        /// <returns>服務類型列表</returns>
        public static Type[] GetRegisteredServiceTypes()
        {
            var types = new Type[_services.Count];
            _services.Keys.CopyTo(types, 0);
            return types;
        }

        /// <summary>
        /// 獲取服務註冊狀態信息
        /// </summary>
        /// <returns>狀態信息字符串</returns>
        public static string GetStatusInfo()
        {
            var info = $"[ServiceLocator] 已註冊 {_services.Count} 個服務:\n";
            
            foreach (var kvp in _serviceNames)
            {
                var serviceType = kvp.Key;
                var serviceName = kvp.Value;
                var serviceInstance = _services[serviceType];
                
                info += $"  - {serviceName} ({serviceType.Name}) -> {serviceInstance.GetType().Name}\n";
            }
            
            return info;
        }

        /// <summary>
        /// 標記服務定位器為已初始化
        /// </summary>
        internal static void MarkAsInitialized()
        {
            _isInitialized = true;
            Debug.Log("[ServiceLocator] 服務定位器初始化完成");
        }

        /// <summary>
        /// 檢查服務定位器是否已初始化
        /// </summary>
        public static bool IsInitialized => _isInitialized;
    }

    /// <summary>
    /// 服務未找到異常
    /// </summary>
    public class ServiceNotFoundException : Exception
    {
        public ServiceNotFoundException(string message) : base(message) { }
        public ServiceNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }
}