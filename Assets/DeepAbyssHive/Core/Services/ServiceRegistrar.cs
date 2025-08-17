lunng UnityEngine;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Core.Managers;
using DeepAbyssHive.Units.Services;
using DeepAbyssHive.Units.Interfaces;
using DeepAbyssHive.Buildings.Services;
using DeepAbyssHive.Buildings.Interfaces;
using DeepAbyssHive.Terrain.Services;
using DeepAbyssHive.Terrain.Interfaces;
using DeepAbyssHive.Creep.Services;
using DeepAbyssHive.Creep.Interfaces;
using DeepAbyssHive.SpatialIndex.Services;
using DeepAbyssHive.SpatialIndex.Interfaces;
using DeepAbyssHive.SpatialIndex.Managers;

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
        [SerializeField] private bool enableServiceLogging = true;

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
            if (enableServiceLogging)
                Debug.Log("[ServiceRegistrar] 开始注册所有系统服务...");

            var serviceManager = ServiceManager.Instance;

            // 按依赖顺序注册服务
            RegisterSpatialIndexServices(serviceManager);
            RegisterTerrainServices(serviceManager);
            RegisterCreepServices(serviceManager);
            RegisterUnitServices(serviceManager);
            RegisterBuildingServices(serviceManager);

            if (enableServiceLogging)
                Debug.Log("[ServiceRegistrar] 所有系统服务注册完成");
        }

        /// <summary>
        /// 注册空间索引服务
        /// </summary>
        private void RegisterSpatialIndexServices(ServiceManager serviceManager)
        {
            try
            {
                var spatialIndexManager = FindObjectOfType<SpatialIndexManager>();
                if (spatialIndexManager != null)
                {
                    // 直接創建空間索引服務實例
                    var spatialIndexService = new SpatialIndexService();
                    serviceManager.RegisterService<ISpatialIndexService>(spatialIndexService);
                    if (enableServiceLogging)
                        Debug.Log("[ServiceRegistrar] 空间索引服务注册成功");
                }
                else
                {
                    Debug.LogWarning("[ServiceRegistrar] 未找到 SpatialIndexManager，跳过空间索引服务注册");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ServiceRegistrar] 空间索引服务注册失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 注册地形服务
        /// </summary>
        private void RegisterTerrainServices(ServiceManager serviceManager)
        {
            try
            {
                var terrainManager = GameManager.Instance?.TerrainManager;
                if (terrainManager != null)
                {
                    // 從 TerrainManager 獲取服務實例
                    var queryService = terrainManager.GetService<ITerrainQueryService>();
                    var modificationService = terrainManager.GetService<ITerrainModificationService>();
                    var generationService = terrainManager.GetService<ITerrainGenerationService>();

                    if (queryService != null)
                        serviceManager.RegisterService<ITerrainQueryService>(queryService);
                    if (modificationService != null)
                        serviceManager.RegisterService<ITerrainModificationService>(modificationService);
                    if (generationService != null)
                        serviceManager.RegisterService<ITerrainGenerationService>(generationService);

                    if (enableServiceLogging)
                        Debug.Log("[ServiceRegistrar] 地形服务注册成功");
                }
                else
                {
                    Debug.LogWarning("[ServiceRegistrar] 未找到 TerrainManager，跳过地形服务注册");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ServiceRegistrar] 地形服务注册失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 注册菌毯服务
        /// </summary>
        private void RegisterCreepServices(ServiceManager serviceManager)
        {
            try
            {
                var creepManager = GameManager.Instance?.CreepManager;
                if (creepManager != null)
                {
                    // 從 CreepManager 獲取服務實例
                    var gridService = creepManager.GetService<ICreepGridService>();
                    var queryService = creepManager.GetService<ICreepQueryService>();
                    var expansionService = creepManager.GetService<ICreepExpansionService>();
                    var sourceService = creepManager.GetService<ICreepSourceService>();
                    var networkService = creepManager.GetService<ICreepNetworkService>();

                    if (gridService != null)
                        serviceManager.RegisterService<ICreepGridService>(gridService);
                    if (queryService != null)
                        serviceManager.RegisterService<ICreepQueryService>(queryService);
                    if (expansionService != null)
                        serviceManager.RegisterService<ICreepExpansionService>(expansionService);
                    if (sourceService != null)
                        serviceManager.RegisterService<ICreepSourceService>(sourceService);
                    if (networkService != null)
                        serviceManager.RegisterService<ICreepNetworkService>(networkService);

                    if (enableServiceLogging)
                        Debug.Log("[ServiceRegistrar] 菌毯服务注册成功");
                }
                else
                {
                    Debug.LogWarning("[ServiceRegistrar] 未找到 CreepManager，跳过菌毯服务注册");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ServiceRegistrar] 菌毯服务注册失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 注册单位服务
        /// </summary>
        private void RegisterUnitServices(ServiceManager serviceManager)
        {
            try
            {
                var unitManager = GameManager.Instance?.UnitManager;
                if (unitManager != null)
                {
                    // 從 UnitManager 獲取服務實例
                    var queryService = unitManager.GetService<IUnitQueryService>();
                    var commandService = unitManager.GetService<IUnitCommandService>();

                    if (queryService != null)
                        serviceManager.RegisterService<IUnitQueryService>(queryService);
                    if (commandService != null)
                        serviceManager.RegisterService<IUnitCommandService>(commandService);

                    if (enableServiceLogging)
                        Debug.Log("[ServiceRegistrar] 单位服务注册成功");
                }
                else
                {
                    Debug.LogWarning("[ServiceRegistrar] 未找到 UnitManager，跳过单位服务注册");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ServiceRegistrar] 单位服务注册失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 注册建筑服务
        /// </summary>
        private void RegisterBuildingServices(ServiceManager serviceManager)
        {
            try
            {
                var buildingManager = GameManager.Instance?.BuildingManager;
                if (buildingManager != null)
                {
                    // 從 BuildingManager 獲取服務實例
                    var queryService = buildingManager.GetService<IBuildingQueryService>();
                    var constructionService = buildingManager.GetService<IBuildingConstructionService>();
                    var researchService = buildingManager.GetService<IResearchService>();

                    if (queryService != null)
                        serviceManager.RegisterService<IBuildingQueryService>(queryService);
                    if (constructionService != null)
                        serviceManager.RegisterService<IBuildingConstructionService>(constructionService);
                    if (researchService != null)
                        serviceManager.RegisterService<IResearchService>(researchService);

                    if (enableServiceLogging)
                        Debug.Log("[ServiceRegistrar] 建筑服务注册成功");
                }
                else
                {
                    Debug.LogWarning("[ServiceRegistrar] 未找到 BuildingManager，跳过建筑服务注册");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ServiceRegistrar] 建筑服务注册失败: {ex.Message}");
            }
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
