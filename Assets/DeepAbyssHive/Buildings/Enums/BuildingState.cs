namespace DeepAbyssHive.Buildings.Enums
{
    /// <summary>
    /// 建筑状态枚举
    /// </summary>
    public enum BuildingState
    {
        /// <summary>
        /// 建造中
        /// </summary>
        UnderConstruction = 0,
        
        /// <summary>
        /// 运行中
        /// </summary>
        Operational = 1,
        
        /// <summary>
        /// 待命中
        /// </summary>
        Idle = 2,
        
        /// <summary>
        /// 受损中
        /// </summary>
        Damaged = 3,
        
        /// <summary>
        /// 修复中
        /// </summary>
        Repairing = 4,
        
        /// <summary>
        /// 升级中
        /// </summary>
        Upgrading = 5,
        
        /// <summary>
        /// 已摧毁
        /// </summary>
        Destroyed = 6
    }
}