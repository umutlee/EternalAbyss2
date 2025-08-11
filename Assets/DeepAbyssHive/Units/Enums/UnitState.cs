namespace DeepAbyssHive.Units.Enums
{
    /// <summary>
    /// 单位状态枚举
    /// </summary>
    public enum UnitState
    {
        /// <summary>
        /// 空闲状态
        /// </summary>
        Idle = 0,
        
        /// <summary>
        /// 移动状态
        /// </summary>
        Moving = 1,
        
        /// <summary>
        /// 攻击状态
        /// </summary>
        Attacking = 2,
        
        /// <summary>
        /// 收集资源状态
        /// </summary>
        Gathering = 3,
        
        /// <summary>
        /// 建造状态
        /// </summary>
        Building = 4,
        
        /// <summary>
        /// 死亡状态
        /// </summary>
        Dead = 5,
        
        /// <summary>
        /// 进化状态
        /// </summary>
        Evolving = 6,
        
        /// <summary>
        /// 适应状态
        /// </summary>
        Adapting = 7
    }
}