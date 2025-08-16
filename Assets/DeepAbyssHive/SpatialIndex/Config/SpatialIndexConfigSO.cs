using UnityEngine;
using DeepAbyssHive.Core.Config;

namespace DeepAbyssHive.SpatialIndex.Config
{
    /// <summary>
    /// 空间索引系统配置数据
    /// 包含空间索引结构、性能优化、调试等所有配置
    /// </summary>
    [CreateAssetMenu(fileName = "SpatialIndexConfig", menuName = "DeepAbyssHive/Config/SpatialIndex Config")]
    public class SpatialIndexConfigSO : BaseConfigSO
    {
        [Header("空间索引结构")]
        [Tooltip("使用八叉树（true）或四叉树（false）")]
        public bool useOctree = true;
        
        [Tooltip("世界边界")]
        public Bounds worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
        
        [Tooltip("最大深度")]
        public int maxDepth = 8;
        
        [Tooltip("每个节点最大对象数")]
        public int maxObjectsPerNode = 10;
        
        [Tooltip("自动调整大小")]
        public bool autoResize = true;

        [Header("性能优化")]
        [Tooltip("启用批处理")]
        public bool enableBatching = true;
        
        [Tooltip("批处理大小")]
        public int batchSize = 100;
        
        [Tooltip("更新间隔（秒）")]
        public float updateInterval = 0.1f;
        
        [Tooltip("启用异步查询")]
        public bool enableAsyncQueries = true;
        
        [Tooltip("查询缓存大小")]
        public int queryCacheSize = 1000;
        
        [Tooltip("优化间隔（秒）")]
        public float optimizeInterval = 5.0f;

        [Header("内存管理")]
        [Tooltip("节点池初始大小")]
        public int nodePoolInitialSize = 500;
        
        [Tooltip("节点池最大大小")]
        public int nodePoolMaxSize = 2000;
        
        [Tooltip("自动清理未使用节点")]
        public bool autoCleanupNodes = true;
        
        [Tooltip("清理间隔（秒）")]
        public float cleanupInterval = 10.0f;

        [Header("调试设置")]
        [Tooltip("显示调试信息")]
        public bool showDebugInfo = false;
        
        [Tooltip("显示边界")]
        public bool showBounds = false;
        
        [Tooltip("边界颜色")]
        public Color boundsColor = Color.green;
        
        [Tooltip("显示节点统计")]
        public bool showNodeStats = false;
        
        [Tooltip("启用性能分析")]
        public bool enableProfiling = false;

        [Header("查询优化")]
        [Tooltip("启用空间哈希")]
        public bool enableSpatialHashing = true;
        
        [Tooltip("哈希网格大小")]
        public float hashGridSize = 10.0f;
        
        [Tooltip("最大查询结果数")]
        public int maxQueryResults = 1000;
        
        [Tooltip("查询超时时间（毫秒）")]
        public float queryTimeoutMs = 100.0f;

        protected override void OnValidate()
        {
            base.OnValidate();
            
            // 确保值在合理范围内
            maxDepth = Mathf.Clamp(maxDepth, 1, 16);
            maxObjectsPerNode = Mathf.Max(1, maxObjectsPerNode);
            batchSize = Mathf.Max(1, batchSize);
            updateInterval = Mathf.Max(0.01f, updateInterval);
            queryCacheSize = Mathf.Max(10, queryCacheSize);
            optimizeInterval = Mathf.Max(1.0f, optimizeInterval);
            
            nodePoolInitialSize = Mathf.Max(10, nodePoolInitialSize);
            nodePoolMaxSize = Mathf.Max(nodePoolInitialSize, nodePoolMaxSize);
            cleanupInterval = Mathf.Max(1.0f, cleanupInterval);
            
            hashGridSize = Mathf.Max(0.1f, hashGridSize);
            maxQueryResults = Mathf.Max(1, maxQueryResults);
            queryTimeoutMs = Mathf.Max(1.0f, queryTimeoutMs);
            
            // 确保世界边界有效
            if (worldBounds.size.x <= 0 || worldBounds.size.y <= 0 || worldBounds.size.z <= 0)
            {
                worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
            }
        }
    }
}