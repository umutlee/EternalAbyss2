using UnityEngine;
using DeepAbyssHive.Terrain.Enums;

// Unity 2022.3.62f1 / Targets: PC/Android/iOS/MacOS
namespace DeepAbyssHive.Terrain.Interfaces
{
    /// <summary>
    /// 地形數據提供者接口
    /// EA-M1-T01: 抽象地形數據來源，支持不同的地形生成策略
    /// </summary>
    public interface ITerrainProvider
    {
        /// <summary>
        /// 採樣指定世界座標的地形高度
        /// </summary>
        /// <param name="worldPosition">世界座標</param>
        /// <returns>地形高度</returns>
        float SampleHeight(Vector3 worldPosition);

        /// <summary>
        /// 採樣指定世界座標的地形類型
        /// </summary>
        /// <param name="worldPosition">世界座標</param>
        /// <returns>地形類型</returns>
        TerrainType SampleType(Vector3 worldPosition);
    }
}