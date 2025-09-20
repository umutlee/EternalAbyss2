using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Core.Services
{
    /// <summary>
    /// 服务管理器
    /// 负责服务的注册、初始化、更新和依赖管理
    /// </summary>
    public class ServiceManager : MonoBehaviour
    {
        private static ServiceManager _instance;
        public static ServiceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ServiceManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("ServiceManager");
                        _instance = go.AddComponent<ServiceManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("服务管理配置")]
        [SerializeField] private bool enableServiceLogging = true;
        [SerializeField] private bool enablePerformanceMonitoring = true;
        [SerializeField] private float performanceLogInterval = 5f;

        // 服务注册表
        private readonly Dictionary<Type, IService> _services = new Dictionary<Type, IService>();
        private readonly Dictionary<Type, ServiceAttribute> _serviceMetadata = new Dictionary<Type, ServiceAttribute>();
        private readonly List<IUpdatableService> _updatableServices = new List<IUpdatableService>();
        
        // 性能监控
        private readonly Dictionary<Type, float> _serviceUpdateTimes = new Dictionary<Type, float>();
        private float _performanceLogTimer = 0f;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            UpdateServices(UnityEngine.Time.deltaTime);
            
            if (enablePerformanceMonitoring)
            {
                MonitorPerformance(UnityEngine.Time.deltaTime);
            }
        }

        /// <summary>
        /// 注册服务
        /// </summary>
        /// <typeparam name="T">服务接口类型</typeparam>
        /// <param name="service">服务实例</param>
        public void RegisterService<T>(T service) where T : class, IService
        {
            var serviceType = typeof(T);
            
            if (_services.ContainsKey(serviceType))
            {
                DAHLog.Warning(LogCategory.SERVICE, $"[ServiceManager] 服务已存在，将被替换: {serviceType.Name}");
            }

            _services[serviceType] = service;
            
            // 获取服务元数据
            var attribute = serviceType.GetCustomAttributes(typeof(ServiceAttribute), false)
                .FirstOrDefault() as ServiceAttribute;
            if (attribute != null)
            {
                _serviceMetadata[serviceType] = attribute;
            }

            // 如果是可更新服务，添加到更新列表
            if (service is IUpdatableService updatableService)
            {
                _updatableServices.Add(updatableService);
                // 按优先级排序
                _updatableServices.Sort((a, b) => GetServicePriority(a.GetType()).CompareTo(GetServicePriority(b.GetType())));
            }

            if (enableServiceLogging)
            {
                DAHLog.Info(LogCategory.SERVICE, $"[ServiceManager] 服务注册成功: {serviceType.Name} ({service.ServiceName})");
            }
        }

        /// <summary>
        /// 获取服务
        /// </summary>
        /// <typeparam name="T">服务接口类型</typeparam>
        /// <returns>服务实例</returns>
        public T GetService<T>() where T : class, IService
        {
            var serviceType = typeof(T);
            
            if (_services.TryGetValue(serviceType, out IService service))
            {
                return service as T;
            }

            DAHLog.Warning(LogCategory.SERVICE, $"[ServiceManager] 服务未找到: {serviceType.Name}");
            return null;
        }

        /// <summary>
        /// 检查服务是否存在
        /// </summary>
        /// <typeparam name="T">服务接口类型</typeparam>
        /// <returns>是否存在</returns>
        public bool HasService<T>() where T : class, IService
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// 初始化所有服务
        /// </summary>
        public void InitializeAllServices()
        {
            if (enableServiceLogging)
            {
                DAHLog.Info(LogCategory.SERVICE, "[ServiceManager] 开始初始化所有服务...");
            }

            // 按优先级和依赖关系排序
            var sortedServices = SortServicesByDependencies();

            foreach (var service in sortedServices)
            {
                try
                {
                    if (!service.IsInitialized)
                    {
                        service.Initialize();
                        
                        if (enableServiceLogging)
                        {
                            DAHLog.Info(LogCategory.SERVICE, $"[ServiceManager] 服务初始化成功: {service.ServiceName}");
                        }
                    }
                }
                catch (Exception e)
                {
                    DAHLog.Error(LogCategory.SERVICE, $"[ServiceManager] 服务初始化失败: {service.ServiceName} - {e.Message}");
                }
            }

            if (enableServiceLogging)
            {
                DAHLog.Info(LogCategory.SERVICE, $"[ServiceManager] 所有服务初始化完成，共 {_services.Count} 个服务");
            }
        }

        /// <summary>
        /// 清理所有服务
        /// </summary>
        public void CleanupAllServices()
        {
            if (enableServiceLogging)
            {
                DAHLog.Info(LogCategory.SERVICE, "[ServiceManager] 开始清理所有服务...");
            }

            // 反向清理（与初始化顺序相反）
            var sortedServices = SortServicesByDependencies().AsEnumerable().Reverse();

            foreach (var service in sortedServices)
            {
                try
                {
                    service.Cleanup();
                    
                    if (enableServiceLogging)
                    {
                        DAHLog.Info(LogCategory.SERVICE, $"[ServiceManager] 服务清理成功: {service.ServiceName}");
                    }
                }
                catch (Exception e)
                {
                    DAHLog.Error(LogCategory.SERVICE, $"[ServiceManager] 服务清理失败: {service.ServiceName} - {e.Message}");
                }
            }

            _services.Clear();
            _serviceMetadata.Clear();
            _updatableServices.Clear();
            _serviceUpdateTimes.Clear();

            if (enableServiceLogging)
            {
                DAHLog.Info(LogCategory.SERVICE, "[ServiceManager] 所有服务清理完成");
            }
        }

        /// <summary>
        /// 更新所有可更新服务
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateServices(float deltaTime)
        {
            foreach (var service in _updatableServices)
            {
                if (service.IsInitialized)
                {
                    var startTime = UnityEngine.Time.realtimeSinceStartup;
                    
                    try
                    {
                        service.Update(deltaTime);
                    }
                    catch (Exception e)
                    {
                        DAHLog.Error(LogCategory.SERVICE, $"[ServiceManager] 服务更新失败: {service.ServiceName} - {e.Message}");
                    }

                    if (enablePerformanceMonitoring)
                    {
                        var updateTime = UnityEngine.Time.realtimeSinceStartup - startTime;
                        _serviceUpdateTimes[service.GetType()] = updateTime;
                    }
                }
            }
        }

        /// <summary>
        /// 性能监控
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        private void MonitorPerformance(float deltaTime)
        {
            _performanceLogTimer += deltaTime;
            
            if (_performanceLogTimer >= performanceLogInterval)
            {
                LogPerformanceStats();
                _performanceLogTimer = 0f;
            }
        }

        /// <summary>
        /// 记录性能统计
        /// </summary>
        private void LogPerformanceStats()
        {
            if (_serviceUpdateTimes.Count == 0) return;

            var totalTime = _serviceUpdateTimes.Values.Sum();
            var avgTime = totalTime / _serviceUpdateTimes.Count;
            var maxTime = _serviceUpdateTimes.Values.Max();

            DAHLog.Info(LogCategory.SERVICE, $"[ServiceManager] 性能统计 - 总时间: {totalTime:F4}ms, 平均: {avgTime:F4}ms, 最大: {maxTime:F4}ms");

            // 记录最耗时的服务
            var slowestService = _serviceUpdateTimes.OrderByDescending(kvp => kvp.Value).First();
            DAHLog.Info(LogCategory.SERVICE, $"[ServiceManager] 最耗时服务: {slowestService.Key.Name} ({slowestService.Value:F4}ms)");
        }

        /// <summary>
        /// 按依赖关系排序服务
        /// </summary>
        /// <returns>排序后的服务列表</returns>
        private List<IService> SortServicesByDependencies()
        {
            var result = new List<IService>();
            var visited = new HashSet<Type>();
            var visiting = new HashSet<Type>();

            foreach (var serviceType in _services.Keys)
            {
                if (!visited.Contains(serviceType))
                {
                    VisitService(serviceType, visited, visiting, result);
                }
            }

            return result;
        }

        /// <summary>
        /// 深度优先遍历服务依赖
        /// </summary>
        private void VisitService(Type serviceType, HashSet<Type> visited, HashSet<Type> visiting, List<IService> result)
        {
            if (visiting.Contains(serviceType))
            {
                DAHLog.Warning(LogCategory.SERVICE, $"[ServiceManager] 检测到循环依赖: {serviceType.Name}");
                return;
            }

            if (visited.Contains(serviceType))
            {
                return;
            }

            visiting.Add(serviceType);

            // 先处理依赖
            if (_serviceMetadata.TryGetValue(serviceType, out ServiceAttribute metadata))
            {
                foreach (var dependency in metadata.Dependencies)
                {
                    if (_services.ContainsKey(dependency))
                    {
                        VisitService(dependency, visited, visiting, result);
                    }
                }
            }

            visiting.Remove(serviceType);
            visited.Add(serviceType);
            result.Add(_services[serviceType]);
        }

        /// <summary>
        /// 获取服务优先级
        /// </summary>
        /// <param name="serviceType">服务类型</param>
        /// <returns>优先级</returns>
        private ServicePriority GetServicePriority(Type serviceType)
        {
            if (_serviceMetadata.TryGetValue(serviceType, out ServiceAttribute metadata))
            {
                return metadata.Priority;
            }
            return ServicePriority.Normal;
        }
    }
}