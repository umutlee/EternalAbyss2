using UnityEngine;

namespace DeepAbyssHive.Creep.Config
{
    /// <summary>
    /// 菌毯配置
    /// </summary>
    [CreateAssetMenu(fileName = "CreepConfig", menuName = "DeepAbyssHive/Creep/CreepConfig")]
    public class CreepConfig : ScriptableObject
    {
        [Header("基础设置")]
        [SerializeField] private float _defaultRadius = 10f;
        [SerializeField] private float _maxRadius = 50f;
        [SerializeField] private float _growthRate = 1f;
        [SerializeField] private float _decayRate = 0.5f;
        
        [Header("密度设置")]
        [SerializeField] private float _maxDensity = 1f;
        [SerializeField] private float _minDensity = 0.1f;
        [SerializeField] private AnimationCurve _densityCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        
        [Header("更新设置")]
        [SerializeField] private float _updateInterval = 0.1f;
        [SerializeField] private int _maxUpdatesPerFrame = 10;
        
        [Header("空间索引设置")]
        [SerializeField] private float _spatialCellSize = 20f;
        [SerializeField] private int _maxObjectsPerCell = 50;
        
        /// <summary>
        /// 默认半径
        /// </summary>
        public float DefaultRadius => _defaultRadius;
        
        /// <summary>
        /// 最大半径
        /// </summary>
        public float MaxRadius => _maxRadius;
        
        /// <summary>
        /// 生长速率
        /// </summary>
        public float GrowthRate => _growthRate;
        
        /// <summary>
        /// 衰减速率
        /// </summary>
        public float DecayRate => _decayRate;
        
        /// <summary>
        /// 最大密度
        /// </summary>
        public float MaxDensity => _maxDensity;
        
        /// <summary>
        /// 最小密度
        /// </summary>
        public float MinDensity => _minDensity;
        
        /// <summary>
        /// 密度曲线
        /// </summary>
        public AnimationCurve DensityCurve => _densityCurve;
        
        /// <summary>
        /// 更新间隔
        /// </summary>
        public float UpdateInterval => _updateInterval;
        
        /// <summary>
        /// 每帧最大更新数
        /// </summary>
        public int MaxUpdatesPerFrame => _maxUpdatesPerFrame;
        
        /// <summary>
        /// 空间网格大小
        /// </summary>
        public float SpatialCellSize => _spatialCellSize;
        
        /// <summary>
        /// 每个网格最大对象数
        /// </summary>
        public int MaxObjectsPerCell => _maxObjectsPerCell;
    }
}