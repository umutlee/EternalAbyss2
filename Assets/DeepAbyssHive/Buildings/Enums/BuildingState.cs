namespace DeepAbyssHive.Buildings.Enums
{
    /// <summary>
    /// 建筑状态枚举
    /// 定义建筑在不同阶段的状态
    /// </summary>
    public enum BuildingState
    {
        /// <summary>
        /// 建造中
        /// </summary>
        UnderConstruction,
        
        /// <summary>
        /// 正常运行
        /// </summary>
        Operational,
        
        /// <summary>
        /// 升级中
        /// </summary>
        Upgrading,
        
        /// <summary>
        /// 受损
        /// </summary>
        Damaged,
        
        /// <summary>
        /// 修理中
        /// </summary>
        Repairing,
        
        /// <summary>
        /// 已摧毁
        /// </summary>
        Destroyed,
        
        /// <summary>
        /// 暂停/离线
        /// </summary>
        Paused,
        
        /// <summary>
        /// 维护中
        /// </summary>
        Maintenance
    }
}