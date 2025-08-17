namespace DeepAbyssHive.Units.Managers
{
    /// <summary>
    /// 单位管理器进化部分 - 委托给 UnitCommandService 处理
    /// </summary>
    public partial class UnitManager
    {
        /// <summary>
        /// 进化单位 - 委托给命令服务
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="evolutionPath">进化路径ID</param>
        /// <returns>是否成功</returns>
        public bool EvolveUnit(int unitId, string evolutionPath)
        {
            return _commandService?.EvolveUnit(unitId, evolutionPath) ?? false;
        }

        /// <summary>
        /// 使单位适应环境 - 委托给命令服务
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="environmentType">环境类型</param>
        public void AdaptToEnvironment(int unitId, string environmentType)
        {
            _commandService?.AdaptToEnvironment(unitId, environmentType);
        }
    }
}
