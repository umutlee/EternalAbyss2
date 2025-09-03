using UnityEngine;

namespace DeepAbyssHive.Terrain.Config
{
    /// <summary>
    /// 地形系统配置数据
    /// 包含地形生成、分块管理、噪声参数等所有配置
    /// </summary>
    [CreateAssetMenu(fileName = "TerrainConfig", menuName = "DeepAbyssHive/Config/Terrain Config")]
    public partial class TerrainConfigSO : ScriptableObject
    {
        [Header("分块设置")]
        [Tooltip("地形分块大小")]
        public int chunkSize = 64;
        
        [Tooltip("地形瓦片大小")]
        public float tileSize = 1.0f;
        
        [Tooltip("加载半径（分块数量）")]
        public float loadRadius = 3.0f;
        
        [Tooltip("卸载半径（分块数量）")]
        public int unloadRadius = 5;

        [Header("噪声生成")]
        [Tooltip("噪声缩放")]
        public float noiseScale = 0.1f;
        
        [Tooltip("高度缩放")]
        public float heightScale = 10.0f;
        
        [Tooltip("噪声种子")]
        public int seed = 12345;
        
        [Tooltip("八度数")]
        public int octaves = 4;
        
        [Tooltip("持续性")]
        public float persistence = 0.5f;
        
        [Tooltip("间隙")]
        public float lacunarity = 2.0f;

        [Header("地形类型")]
        [Tooltip("水位高度")]
        public float waterLevel = 0.3f;
        
        [Tooltip("沙滩高度")]
        public float beachLevel = 0.4f;
        
        [Tooltip("草地高度")]
        public float grassLevel = 0.6f;
        
        [Tooltip("石头高度")]
        public float stoneLevel = 0.8f;
        
        [Tooltip("雪地高度")]
        public float snowLevel = 1.0f;

        [Header("性能设置")]
        [Tooltip("每帧最大修改数量")]
        public int maxModificationsPerFrame = 10;
        
        [Tooltip("修改队列最大大小")]
        public int maxModificationQueueSize = 1000;
        
        [Tooltip("历史记录最大数量")]
        public int maxHistoryEntries = 100;

        [Header("调试设置")]
        [Tooltip("启用调试日志")]
        public bool enableDebugLog = false;
        
        [Tooltip("显示分块边界")]
        public bool showChunkBounds = false;
        
        [Tooltip("显示地形网格")]
        public bool showTerrainGrid = false;

        protected virtual void OnValidate()
        {
            
            // 确保值在合理范围内
            chunkSize = Mathf.Max(1, chunkSize);
            tileSize = Mathf.Max(0.1f, tileSize);
            loadRadius = Mathf.Max(1.0f, loadRadius);
            unloadRadius = Mathf.Max(Mathf.RoundToInt(loadRadius) + 1, unloadRadius);
            noiseScale = Mathf.Max(0.001f, noiseScale);
            heightScale = Mathf.Max(0.1f, heightScale);
            octaves = Mathf.Clamp(octaves, 1, 8);
            persistence = Mathf.Clamp01(persistence);
            lacunarity = Mathf.Max(1.0f, lacunarity);
            
            // 确保地形类型高度递增
            beachLevel = Mathf.Max(waterLevel, beachLevel);
            grassLevel = Mathf.Max(beachLevel, grassLevel);
            stoneLevel = Mathf.Max(grassLevel, stoneLevel);
            snowLevel = Mathf.Max(stoneLevel, snowLevel);
            
            maxModificationsPerFrame = Mathf.Max(1, maxModificationsPerFrame);
            maxModificationQueueSize = Mathf.Max(10, maxModificationQueueSize);
            maxHistoryEntries = Mathf.Max(10, maxHistoryEntries);
        }
    }
}