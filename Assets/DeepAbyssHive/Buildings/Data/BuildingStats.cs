using System;
using UnityEngine;

namespace DeepAbyssHive.Buildings
{
    /// <summary>
    /// 建筑统计信息
    /// </summary>
    [Serializable]
    public class BuildingStats
    {
        [SerializeField] private int _totalBuildings;
        [SerializeField] private int _constructedBuildings;
        [SerializeField] private int _underConstructionBuildings;
        [SerializeField] private int _damagedBuildings;
        [SerializeField] private float _totalConstructionProgress;
        [SerializeField] private float _averageHealth;

        /// <summary>
        /// 总建筑数量
        /// </summary>
        public int TotalBuildings
        {
            get => _totalBuildings;
            set => _totalBuildings = value;
        }

        /// <summary>
        /// 已建造完成的建筑数量
        /// </summary>
        public int ConstructedBuildings
        {
            get => _constructedBuildings;
            set => _constructedBuildings = value;
        }

        /// <summary>
        /// 正在建造的建筑数量
        /// </summary>
        public int UnderConstructionBuildings
        {
            get => _underConstructionBuildings;
            set => _underConstructionBuildings = value;
        }

        /// <summary>
        /// 受损建筑数量
        /// </summary>
        public int DamagedBuildings
        {
            get => _damagedBuildings;
            set => _damagedBuildings = value;
        }

        /// <summary>
        /// 总建造进度
        /// </summary>
        public float TotalConstructionProgress
        {
            get => _totalConstructionProgress;
            set => _totalConstructionProgress = value;
        }

        /// <summary>
        /// 平均生命值百分比
        /// </summary>
        public float AverageHealth
        {
            get => _averageHealth;
            set => _averageHealth = value;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public BuildingStats()
        {
            _totalBuildings = 0;
            _constructedBuildings = 0;
            _underConstructionBuildings = 0;
            _damagedBuildings = 0;
            _totalConstructionProgress = 0f;
            _averageHealth = 100f;
        }

        /// <summary>
        /// 重置统计信息
        /// </summary>
        public void Reset()
        {
            _totalBuildings = 0;
            _constructedBuildings = 0;
            _underConstructionBuildings = 0;
            _damagedBuildings = 0;
            _totalConstructionProgress = 0f;
            _averageHealth = 100f;
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        public override string ToString()
        {
            return $"BuildingStats[Total:{_totalBuildings}, Constructed:{_constructedBuildings}, UnderConstruction:{_underConstructionBuildings}, Damaged:{_damagedBuildings}]";
        }
    }
}