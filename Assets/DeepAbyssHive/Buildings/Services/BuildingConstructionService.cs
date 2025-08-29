using System;
using UnityEngine;

namespace DeepAbyssHive.Buildings.Services
{
    /// <summary>
    /// 建築建造服務 - 處理建築建造相關功能
    /// </summary>
    public partial class BuildingConstructionService : IBuildingConstructionService
    {
        private readonly IBuildingDataService _buildingDataService;
        private readonly IBuildingStateService _buildingStateService;

        public BuildingConstructionService(IBuildingDataService buildingDataService, IBuildingStateService buildingStateService)
        {
            _buildingDataService = buildingDataService ?? throw new ArgumentNullException(nameof(buildingDataService));
            _buildingStateService = buildingStateService ?? throw new ArgumentNullException(nameof(buildingStateService));
        }

        public BuildingConstructionService()
            : this(DeepAbyssHive.Core.Services.ServiceLocator.Get<IBuildingDataService>(),
                   DeepAbyssHive.Core.Services.ServiceLocator.Get<IBuildingStateService>()) { }

        // TODO: 實作建造方法
        public bool StartConstruction(string buildingId, Vector3 position)
        {
            return _buildingDataService != null && _buildingStateService != null;
        }
    }

    /// <summary>
    /// 建築建造服務介面
    /// </summary>
    public interface IBuildingConstructionService
    {
        bool StartConstruction(string buildingId, Vector3 position);
    }
}