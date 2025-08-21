using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Creep.Data;

namespace DeepAbyssHive.Creep.Services
{
    /// <summary>
    /// 菌毯网格服务接口
    /// 负责菌毯网格数据的管理和操作
    /// </summary>
    public interface ICreepGridService : IService
    {
        /// <summary>
        /// 网格单元格大小
        /// </summary>
        float GridCellSize { get; }

        /// <summary>
        /// 网格宽度
        /// </summary>
        int GridWidth { get; }

        /// <summary>
        /// 网格高度
        /// </summary>
        int GridHeight { get; }

        /// <summary>
        /// 初始化网格
        /// </summary>
        /// <param name="width">网格宽度</param>
        /// <param name="height">网格高度</param>
        /// <param name="cellSize">单元格大小</param>
        void InitializeGrid(int width, int height, float cellSize);

        /// <summary>
        /// 清理网格
        /// </summary>
        void ClearGrid();

        /// <summary>
        /// 设置网格单元格数据
        /// </summary>
        /// <param name="gridPosition">网格位置</param>
        /// <param name="data">菌毯数据</param>
        void SetGridCell(Vector2Int gridPosition, CreepData data);

        /// <summary>
        /// 获取网格单元格数据
        /// </summary>
        /// <param name="gridPosition">网格位置</param>
        /// <returns>菌毯数据</returns>
        CreepData GetGridCell(Vector2Int gridPosition);

        /// <summary>
        /// 移除网格单元格
        /// </summary>
        /// <param name="gridPosition">网格位置</param>
        void RemoveGridCell(Vector2Int gridPosition);

        /// <summary>
        /// 检查网格位置是否有效
        /// </summary>
        /// <param name="gridPosition">网格位置</param>
        /// <returns>是否有效</returns>
        bool IsValidGridPosition(Vector2Int gridPosition);

        /// <summary>
        /// 检查网格位置是否有菌毯
        /// </summary>
        /// <param name="gridPosition">网格位置</param>
        /// <returns>是否有菌毯</returns>
        bool HasCreepAt(Vector2Int gridPosition);

        /// <summary>
        /// 世界位置转网格位置
        /// </summary>
        /// <param name="worldPosition">世界位置</param>
        /// <returns>网格位置</returns>
        Vector2Int WorldToGridPosition(Vector3 worldPosition);

        /// <summary>
        /// 网格位置转世界位置
        /// </summary>
        /// <param name="gridPosition">网格位置</param>
        /// <returns>世界位置</returns>
        Vector3 GridToWorldPosition(Vector2Int gridPosition);

        /// <summary>
        /// 获取相邻网格位置
        /// </summary>
        /// <param name="gridPosition">中心网格位置</param>
        /// <param name="includeDiagonal">是否包含对角线</param>
        /// <returns>相邻位置数组</returns>
        Vector2Int[] GetNeighborPositions(Vector2Int gridPosition, bool includeDiagonal = true);

        /// <summary>
        /// 获取指定范围内的网格位置
        /// </summary>
        /// <param name="center">中心位置</param>
        /// <param name="radius">半径（网格单位）</param>
        /// <returns>范围内的网格位置</returns>
        NativeArray<Vector2Int> GetGridPositionsInRange(Vector2Int center, int radius);

        /// <summary>
        /// 获取所有活跃的菌毯网格位置
        /// </summary>
        /// <returns>活跃网格位置列表</returns>
        NativeArray<Vector2Int> GetActiveCreepPositions();

        /// <summary>
        /// 批量更新网格数据
        /// </summary>
        /// <param name="updates">更新数据</param>
        void BatchUpdateGrid(NativeArray<CreepGridUpdate> updates);

        /// <summary>
        /// 获取网格统计信息
        /// </summary>
        /// <returns>网格统计</returns>
        CreepGridStatistics GetGridStatistics();
        
        /// <summary>
        /// 设置暂停状态
        /// </summary>
        /// <param name="paused">是否暂停</param>
        void SetPaused(bool paused);
    }

    /// <summary>
    /// 菌毯网格更新数据
    /// </summary>
    public struct CreepGridUpdate
    {
        public Vector2Int GridPosition;
        public CreepData Data;
        public bool Remove;
    }

    /// <summary>
    /// 菌毯网格统计信息
    /// </summary>
    public struct CreepGridStatistics
    {
        public int TotalCells;
        public int ActiveCells;
        public float AverageStrength;
        public float TotalCoverage;
        public int NetworkCount;
    }
}