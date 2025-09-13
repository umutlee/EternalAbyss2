using UnityEngine;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Core.Tests
{
    /// <summary>
    /// 服务系统测试
    /// </summary>
    public class ServiceSystemTest : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private bool runTestOnStart = true;
        
        private void Start()
        {
            if (runTestOnStart)
            {
                TestServiceSystem();
            }
        }
        
        /// <summary>
        /// 测试服务系统
        /// </summary>
        [ContextMenu("测试服务系统")]
        public void TestServiceSystem()
        {
            DAHLog.Info(LogCategory.TEST, "=== 服务系统测试开始 ===");
            
            // 1. 测试ServiceManager单例
            var serviceManager = ServiceManager.Instance;
            if (serviceManager != null)
            {
                DAHLog.Info(LogCategory.TEST, "✓ ServiceManager单例创建成功");
            }
            else
            {
                DAHLog.Error(LogCategory.TEST, "✗ ServiceManager单例创建失败");
                return;
            }
            
            // 2. 测试服务注册器
            var registrar = FindObjectOfType<ServiceRegistrar>();
            if (registrar != null)
            {
                DAHLog.Info(LogCategory.TEST, "✓ ServiceRegistrar找到");
                registrar.RegisterAllServices();
            }
            else
            {
                DAHLog.Warning(LogCategory.TEST, "⚠ ServiceRegistrar未找到，手动创建");
                var go = new GameObject("ServiceRegistrar");
                registrar = go.AddComponent<ServiceRegistrar>();
                registrar.RegisterAllServices();
            }
            
            // 3. 测试服务初始化
            try
            {
                serviceManager.InitializeAllServices();
                DAHLog.Info(LogCategory.TEST, "✓ 服务初始化成功");
            }
            catch (System.Exception e)
            {
                DAHLog.Error(LogCategory.TEST, $"✗ 服务初始化失败: {e.Message}");
            }
            
            // 4. 测试服务查询（目前服务还未实现，所以会返回null）
            // var unitQueryService = serviceManager.GetService<IUnitQueryService>();
            // DAHLog.Info(LogCategory.TEST, $"单位查询服务: {(unitQueryService != null ? "已注册" : "未注册")}");
            
            DAHLog.Info(LogCategory.TEST, "=== 服务系统测试完成 ===");
        }
        
        /// <summary>
        /// 测试服务清理
        /// </summary>
        [ContextMenu("测试服务清理")]
        public void TestServiceCleanup()
        {
            DAHLog.Info(LogCategory.TEST, "=== 服务清理测试开始 ===");
            
            var serviceManager = ServiceManager.Instance;
            if (serviceManager != null)
            {
                serviceManager.CleanupAllServices();
                DAHLog.Info(LogCategory.TEST, "✓ 服务清理完成");
            }
            else
            {
                DAHLog.Error(LogCategory.TEST, "✗ ServiceManager不存在");
            }
            
            DAHLog.Info(LogCategory.TEST, "=== 服务清理测试完成 ===");
        }
    }
}