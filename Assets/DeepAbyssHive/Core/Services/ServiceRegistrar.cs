using UnityEngine;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Units.Services;
using DeepAbyssHive.Buildings.Services;
using DeepAbyssHive.Terrain.Services;
using DeepAbyssHive.Creep.Services;
using DeepAbyssHive.SpatialIndex.Services;

namespace DeepAbyssHive.Core.Services
{
    /// <summary>
    /// 服务注册器
    /// 负责注册所有系统服务到ServiceManager
    /// </summary>
    public class ServiceRegistrar : MonoBehaviour
    {
        [Header("服务注册配置")]
        [SerializeField] private bool autoRegisterOnAwake = true;
        [SerializeField] private bool autoInitializeOnStart = true;

        private void Awake()
        {
            if (autoRegisterOnAwake)
            {
                RegisterAllServices();
            }
        }

        private void Start()
        {
            if (autoInitializeOnStart)
            {
                ServiceManager.Instance.InitializeAllServices();
            }
        }

        /// <summary>
        /// 注册所有服务
        /// </summary>
        public void RegisterAllServices()
        {
            Debug.Log("[ServiceRegistrar] 开始注册所有服务...");

            var serviceManager = ServiceManager.Instance;

            // 注册单位服务
            RegisterUnitServices(serviceManager);

            // 注册建筑服务
            RegisterBuildingServices(serviceManager);

            // 注册地形服务
            RegisterTerrainServices(serviceManager);

            // 注册菌毯服务
            RegisterCreepServices(serviceManager);

            // 注册空间索引服务
            RegisterSpatialIndexServices(serviceManager);

            Debug.Log("[ServiceRegistrar] 所有服务注册完成");
        }

        /// <summary>
        /// 注册单位服务
        /// </summary>
        private void RegisterUnitServices(ServiceManager serviceManager)
        {
            // 注册单位相关服务
            serviceManager.RegisterService<IUnitQueryService>(new UnitQueryService());
            serviceManager.RegisterService<IUnitCommandService>(new UnitCommandService());
            
            Debug.Log("[ServiceRegistrar] 单位服务注册完成（待实现）");
        }

        /// <summary>
        /// 注册建筑服务
        /// </summary>
        private void RegisterBuildingServices(ServiceManager serviceManager)
        {
            // 注册建筑相关服务
            serviceManager.RegisterService<IBuildingQueryService>(new BuildingQueryService());
            serviceManager.RegisterService<IBuildingConstructionService>(new BuildingConstructionService());
            serviceManager.RegisterService<IResearchService>(new ResearchService());
            
            Debug.Log("[ServiceRegistrar] 建筑服务注册完成（待实现）");
        }

        /// <summary>
        /// 注册地形服务
        /// </summary>
        private void RegisterTerrainServices(ServiceManager serviceManager)
        {
            // 地形服务已统一到 ITerrainManager，由 TerrainManager 直接提供
            // 不再需要单独的 Service 接口注册
            
            Debug.Log("[ServiceRegistrar] 地形服务注册完成（使用 ITerrainManager）");
        }

        /// <summary>
        /// 注册菌毯服务
        /// </summary>
        private void RegisterCreepServices(ServiceManager serviceManager)
        {
            // 这里暂时创建空的服务实现，后续会替换为真实的Manager服务
            // serviceManager.RegisterService<ICreepQueryService>(new CreepQueryService());
            // serviceManager.RegisterService<ICreepSimulationService>(new CreepSimulationService());
            
            Debug.Log("[ServiceRegistrar] 菌毯服务注册完成（待实现）");
        }

        /// <summary>
        /// 注册空间索引服务
        /// </summary>
        private void RegisterSpatialIndexServices(ServiceManager serviceManager)
        {
            // 这里暂时创建空的服务实现，后续会替换为真实的Manager服务
            // serviceManager.RegisterService<ISpatialIndexService>(new SpatialIndexService());
            
            Debug.Log("[ServiceRegistrar] 空间索引服务注册完成（待实现）");
        }

        /// <summary>
        /// 清理所有服务
        /// </summary>
        private void OnDestroy()
        {
            if (ServiceManager.Instance != null)
            {
                ServiceManager.Instance.CleanupAllServices();
            }
        }
    }
}