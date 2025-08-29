using System;
using UnityEngine;

namespace DeepAbyssHive.Units.Services
{
    /// <summary>
    /// 單位指令服務 - 處理單位相關指令
    /// </summary>
    public partial class UnitCommandService : IUnitCommandService
    {
        private readonly IUnitDataService _unitDataService;
        private readonly IUnitStateService _unitStateService;

        public UnitCommandService(IUnitDataService unitDataService, IUnitStateService unitStateService)
        {
            _unitDataService = unitDataService ?? throw new ArgumentNullException(nameof(unitDataService));
            _unitStateService = unitStateService ?? throw new ArgumentNullException(nameof(unitStateService));
        }

        public UnitCommandService()
            : this(DeepAbyssHive.Core.Services.ServiceLocator.Get<IUnitDataService>(),
                   DeepAbyssHive.Core.Services.ServiceLocator.Get<IUnitStateService>()) { }

        // TODO: 實作指令方法
        public bool ExecuteCommand(string command, string unitId)
        {
            return _unitDataService != null && _unitStateService != null;
        }
    }

    /// <summary>
    /// 單位指令服務介面
    /// </summary>
    public interface IUnitCommandService
    {
        bool ExecuteCommand(string command, string unitId);
    }
}