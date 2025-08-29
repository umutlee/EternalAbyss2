using System;
using UnityEngine;
using DeepAbyssHive.Units.Enums;
using DeepAbyssHive.Units.Data;
using DeepAbyssHive.Core.Services;

namespace DeepAbyssHive.Units.Services
{
    /// <summary>
    /// 單位指令服務 - 處理單位相關指令
    /// </summary>
    public partial class UnitCommandService : IUnitCommandService, IService, ICommandService
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

        #region IUnitCommandService 實作
        public int CreateUnit(UnitType unitType, Vector3 position, int playerId, Quaternion? rotation = null)
        {
            // TODO: 實作創建單位
            return -1;
        }

        public bool DestroyUnit(int unitId)
        {
            // TODO: 實作銷毀單位
            return false;
        }

        public bool MoveUnit(int unitId, Vector3 targetPosition)
        {
            // TODO: 實作移動單位
            return false;
        }

        public bool AttackTarget(int attackerId, int targetId)
        {
            // TODO: 實作攻擊目標
            return false;
        }

        public bool AttackPosition(int attackerId, Vector3 targetPosition)
        {
            // TODO: 實作攻擊位置
            return false;
        }

        public bool StopUnit(int unitId)
        {
            // TODO: 實作停止單位
            return false;
        }

        public bool EvolveUnit(int unitId, UnitType targetType)
        {
            // TODO: 實作單位進化
            return false;
        }

        public bool SetUnitState(int unitId, UnitState state)
        {
            // TODO: 實作設置單位狀態
            return false;
        }

        public bool ModifyUnitAttribute(int unitId, UnitAttributeType attributeType, float value)
        {
            // TODO: 實作修改單位屬性
            return false;
        }

        public bool HealUnit(int unitId, float healAmount)
        {
            // TODO: 實作治療單位
            return false;
        }

        public bool DamageUnit(int unitId, float damage, DamageType damageType = DamageType.Physical)
        {
            // TODO: 實作對單位造成傷害
            return false;
        }

        public bool SetUnitBehavior(int unitId, UnitBehaviorType behaviorType)
        {
            // TODO: 實作設置單位AI行為
            return false;
        }

        public bool AdaptToEnvironment(int unitId, EnvironmentType environmentType)
        {
            // TODO: 實作單位適應環境
            return false;
        }

        public int MoveUnitsInFormation(int[] unitIds, Vector3 targetPosition, FormationType formation = FormationType.None)
        {
            // TODO: 實作批量移動單位
            return 0;
        }

        // ICommandService 實作
        public bool IsCommandAvailable { get; set; } = true;
        
        public bool CheckCommandAvailable(string commandId, int entityId)
        {
            // TODO: 實作命令可用性檢查
            return false;
        }

        public bool ExecuteCommand(string commandId, int entityId, object[] parameters = null)
        {
            // TODO: 實作命令執行
            return false;
        }
        #endregion
    }


}