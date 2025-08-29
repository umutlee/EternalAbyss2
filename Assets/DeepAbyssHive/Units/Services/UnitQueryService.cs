using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepAbyssHive.Units.Services
{
    /// <summary>
    /// 單位查詢服務 - 提供單位數據查詢功能
    /// </summary>
    public partial class UnitQueryService : IUnitQueryService
    {
        private readonly IUnitDataService _unitDataService;
        private readonly IUnitStateService _unitStateService;

        public UnitQueryService(IUnitDataService unitDataService, IUnitStateService unitStateService)
        {
            _unitDataService = unitDataService ?? throw new ArgumentNullException(nameof(unitDataService));
            _unitStateService = unitStateService ?? throw new ArgumentNullException(nameof(unitStateService));
        }

        public UnitQueryService()
            : this(DeepAbyssHive.Core.Services.ServiceLocator.Get<IUnitDataService>(),
                   DeepAbyssHive.Core.Services.ServiceLocator.Get<IUnitStateService>()) { }

        // TODO: 實作查詢方法
        public bool IsUnitAvailable(string unitId)
        {
            return _unitDataService != null && _unitStateService != null;
        }
    }

    /// <summary>
    /// 單位查詢服務介面
    /// </summary>
    public interface IUnitQueryService
    {
        bool IsUnitAvailable(string unitId);
    }
}