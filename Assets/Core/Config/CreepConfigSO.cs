using UnityEngine;
using DeepAbyssHive.Core.Config;

/// <summary>
/// 菌毯系统配置数据
/// 包含网格尺寸、扩张参数、衰减参数等所有配置
/// </summary>
[CreateAssetMenu(fileName = "CreepConfig", menuName = "DeepAbyssHive/Configs/CreepConfig")]
public class CreepConfigSO : BaseConfigSO
{
    [Header("网格配置")]
    [Tooltip("菌毯网格的单元格大小")]
    [Range(0.1f, 10f)]
    public float gridCellSize = 1f;
    
    [Tooltip("网格的宽度（单元格数量）")]
    [Range(10, 1000)]
    public int gridWidth = 100;
    
    [Tooltip("网格的高度（单元格数量）")]
    [Range(10, 1000)]
    public int gridHeight = 100;

    [Header("扩张配置")]
    [Tooltip("菌毯扩张速度（每秒）")]
    [Range(0.1f, 10f)]
    public float expansionRate = 1f;
    
    [Tooltip("最大扩张距离")]
    [Range(1f, 50f)]
    public float maxExpansionDistance = 10f;
    
    [Tooltip("扩张时的密度增长速度")]
    [Range(0.01f, 1f)]
    public float densityGrowthRate = 0.1f;
    
    [Tooltip("扩张阈值（密度达到此值才能向外扩张）")]
    [Range(0.1f, 1f)]
    public float expansionThreshold = 0.8f;

    [Header("衰减配置")]
    [Tooltip("菌毯衰减速度（每秒）")]
    [Range(0.01f, 1f)]
    public float decayRate = 0.05f;
    
    [Tooltip("最小衰减密度（低于此值的菌毯会被移除）")]
    [Range(0.01f, 0.5f)]
    public float minDecayDensity = 0.1f;
    
    [Tooltip("衰减延迟时间（菌毯失去源点后多久开始衰减）")]
    [Range(0f, 60f)]
    public float decayDelay = 5f;

    [Header("网络配置")]
    [Tooltip("网络连接的最大距离")]
    [Range(1f, 20f)]
    public float maxNetworkDistance = 5f;
    
    [Tooltip("网络修复速度")]
    [Range(0.1f, 5f)]
    public float networkRepairRate = 1f;
    
    [Tooltip("网络分割检测间隔（秒）")]
    [Range(0.1f, 10f)]
    public float networkCheckInterval = 2f;

    [Header("性能配置")]
    [Tooltip("批处理大小（每帧处理的最大单元格数）")]
    [Range(10, 1000)]
    public int batchSize = 100;
    
    [Tooltip("更新间隔（秒，0表示每帧更新）")]
    [Range(0f, 1f)]
    public float updateInterval = 0.1f;
    
    [Tooltip("是否启用多线程处理")]
    public bool enableMultithreading = true;

    [Header("视觉配置")]
    [Tooltip("菌毯材质")]
    public Material creepMaterial;
    
    [Tooltip("菌毯高度偏移")]
    [Range(0f, 1f)]
    public float heightOffset = 0.01f;
    
    [Tooltip("密度可视化颜色渐变")]
    public Gradient densityGradient = new Gradient();
    
    [Tooltip("是否显示网格线（调试用）")]
    public bool showGridLines = false;

    [Header("源点配置")]
    [Tooltip("默认源点强度")]
    [Range(0.1f, 10f)]
    public float defaultSourceStrength = 1f;
    
    [Tooltip("源点影响半径")]
    [Range(1f, 20f)]
    public float sourceInfluenceRadius = 5f;
    
    [Tooltip("源点密度补充速度")]
    [Range(0.1f, 2f)]
    public float sourceReplenishRate = 0.5f;

    protected override void OnValidate()
    {
        base.OnValidate();
        
        // 确保网格尺寸合理
        gridCellSize = Mathf.Max(0.1f, gridCellSize);
        gridWidth = Mathf.Max(10, gridWidth);
        gridHeight = Mathf.Max(10, gridHeight);
        
        // 确保扩张参数合理
        expansionRate = Mathf.Max(0.1f, expansionRate);
        maxExpansionDistance = Mathf.Max(1f, maxExpansionDistance);
        densityGrowthRate = Mathf.Clamp01(densityGrowthRate);
        expansionThreshold = Mathf.Clamp01(expansionThreshold);
        
        // 确保衰减参数合理
        decayRate = Mathf.Max(0.01f, decayRate);
        minDecayDensity = Mathf.Clamp(minDecayDensity, 0.01f, 0.5f);
        decayDelay = Mathf.Max(0f, decayDelay);
        
        // 确保网络参数合理
        maxNetworkDistance = Mathf.Max(1f, maxNetworkDistance);
        networkRepairRate = Mathf.Max(0.1f, networkRepairRate);
        networkCheckInterval = Mathf.Max(0.1f, networkCheckInterval);
        
        // 确保性能参数合理
        batchSize = Mathf.Max(10, batchSize);
        updateInterval = Mathf.Max(0f, updateInterval);
        
        // 确保源点参数合理
        defaultSourceStrength = Mathf.Max(0.1f, defaultSourceStrength);
        sourceInfluenceRadius = Mathf.Max(1f, sourceInfluenceRadius);
        sourceReplenishRate = Mathf.Max(0.1f, sourceReplenishRate);
        
        // 初始化默认渐变
        if (densityGradient.colorKeys.Length == 0)
        {
            densityGradient.SetKeys(
                new GradientColorKey[] 
                {
                    new GradientColorKey(Color.clear, 0f),
                    new GradientColorKey(new Color(0.5f, 0.2f, 0.8f, 0.5f), 0.5f),
                    new GradientColorKey(new Color(0.8f, 0.3f, 1f, 1f), 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.5f, 0.5f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
        }
    }
}