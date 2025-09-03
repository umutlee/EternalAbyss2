using UnityEngine;
using DeepAbyssHive.Terrain.Interfaces;
using DeepAbyssHive.Terrain.Enums;

// Unity 2022.3.62f1 / Targets: PC/Android/iOS/MacOS
namespace DeepAbyssHive.Terrain.Providers
{
    /// <summary>
    /// 平地地形提供者
    /// EA-M1-T01: 預設實現，提供平坦地形（高度=0，類型=Ground）
    /// </summary>
    public class FlatTerrainProvider : ITerrainProvider
    {
        /// <summary>
        /// 採樣地形高度（固定返回 0）
        /// </summary>
        /// <param name="worldPosition">世界座標</param>
        /// <returns>固定高度 0</returns>
        public float SampleHeight(Vector3 worldPosition)
        {
            return 0f;
        }

        /// <summary>
        /// 採樣地形類型（固定返回 Ground）
        /// </summary>
        /// <param name="worldPosition">世界座標</param>
        /// <returns>固定類型 Ground</returns>
        public TerrainType SampleType(Vector3 worldPosition)
        {
            return TerrainType.Ground;
        }
    }
}