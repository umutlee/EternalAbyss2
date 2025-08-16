using UnityEngine;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Terrain.Enums;
using DeepAbyssHive.Terrain.Data;

namespace DeepAbyssHive.Terrain.Services
{
    /// <summary>
    /// 地形修改服务接口
    /// 提供地形修改相关功能
    /// </summary>
    public interface ITerrainModificationService : ICommandService
    {
        /// <summary>
        /// 修改地形高度
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="radius">影响半径</param>
        /// <param name="heightDelta">高度变化量</param>
        /// <param name="falloff">衰减曲线</param>
        /// <returns>是否成功</returns>
        bool ModifyHeight(Vector3 position, float radius, float heightDelta, AnimationCurve falloff = null);

        /// <summary>
        /// 设置地形类型
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="radius">影响半径</param>
        /// <param name="terrainType">地形类型</param>
        /// <returns>是否成功</returns>
        bool SetTerrainType(Vector3 position, float radius, TerrainType terrainType);

        /// <summary>
        /// 平整地形
        /// </summary>
        /// <param name="center">中心位置</param>
        /// <param name="size">区域大小</param>
        /// <param name="targetHeight">目标高度（-1表示使用中心点高度）</param>
        /// <returns>是否成功</returns>
        bool FlattenTerrain(Vector3 center, Vector2 size, float targetHeight = -1f);

        /// <summary>
        /// 挖掘地形
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="radius">半径</param>
        /// <param name="depth">深度</param>
        /// <returns>是否成功</returns>
        bool DigTerrain(Vector3 position, float radius, float depth);

        /// <summary>
        /// 填充地形
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="radius">半径</param>
        /// <param name="height">填充高度</param>
        /// <returns>是否成功</returns>
        bool FillTerrain(Vector3 position, float radius, float height);

        /// <summary>
        /// 创建坡道
        /// </summary>
        /// <param name="start">起始位置</param>
        /// <param name="end">结束位置</param>
        /// <param name="width">坡道宽度</param>
        /// <returns>是否成功</returns>
        bool CreateRamp(Vector3 start, Vector3 end, float width);

        /// <summary>
        /// 创建隧道
        /// </summary>
        /// <param name="start">起始位置</param>
        /// <param name="end">结束位置</param>
        /// <param name="radius">隧道半径</param>
        /// <returns>是否成功</returns>
        bool CreateTunnel(Vector3 start, Vector3 end, float radius);

        /// <summary>
        /// 应用地形修改
        /// </summary>
        /// <param name="modification">地形修改数据</param>
        /// <returns>是否成功</returns>
        bool ApplyModification(TerrainModification modification);

        /// <summary>
        /// 撤销地形修改
        /// </summary>
        /// <param name="modificationId">修改ID</param>
        /// <returns>是否成功</returns>
        bool UndoModification(int modificationId);

        /// <summary>
        /// 重做地形修改
        /// </summary>
        /// <param name="modificationId">修改ID</param>
        /// <returns>是否成功</returns>
        bool RedoModification(int modificationId);

        /// <summary>
        /// 清除修改历史
        /// </summary>
        /// <param name="keepCount">保留的修改数量</param>
        void ClearModificationHistory(int keepCount = 0);

        /// <summary>
        /// 保存地形修改
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否成功</returns>
        bool SaveModifications(string filePath);

        /// <summary>
        /// 加载地形修改
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否成功</returns>
        bool LoadModifications(string filePath);

        /// <summary>
        /// 重新生成地形块
        /// </summary>
        /// <param name="chunkX">块X坐标</param>
        /// <param name="chunkZ">块Z坐标</param>
        /// <returns>是否成功</returns>
        bool RegenerateChunk(int chunkX, int chunkZ);

        /// <summary>
        /// 批量应用修改
        /// </summary>
        /// <param name="modifications">修改列表</param>
        /// <returns>成功应用的修改数量</returns>
        int ApplyModificationBatch(TerrainModification[] modifications);
    }

    /// <summary>
    /// 地形修改数据
    /// </summary>
    public struct TerrainModification
    {
        public int Id;
        public TerrainModificationType Type;
        public Vector3 Position;
        public float Radius;
        public float Value;
        public TerrainType TerrainType;
        public AnimationCurve Falloff;
        public System.DateTime Timestamp;
    }

    /// <summary>
    /// 地形修改类型
    /// </summary>
    public enum TerrainModificationType
    {
        HeightChange,
        TypeChange,
        Flatten,
        Dig,
        Fill,
        Ramp,
        Tunnel
    }
}