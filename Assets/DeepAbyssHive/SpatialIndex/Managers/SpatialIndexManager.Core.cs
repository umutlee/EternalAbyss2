using UnityEngine;
using System.Collections.Generic;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.SpatialIndex.Interfaces;
using DeepAbyssHive.SpatialIndex.Data;
using DeepAbyssHive.SpatialIndex.Implementations;
using DeepAbyssHive.SpatialIndex.Config;
using DeepAbyssHive.SpatialIndex.Services;
using DeepAbyssHive.Core.Config;

namespace DeepAbyssHive.SpatialIndex.Managers
{
    /// <summary>
    /// 空间索引管理器核心部分 - 数据结构、初始化/清理、生命周期管理
    /// </summary>
    public partial class SpatialIndexManager : MonoBehaviour, IManager, IUpdatable, IFixedUpdatable, ILateUpdatable
    {
        [Header("空间索引配置")]
        [SerializeField] private bool _useOctree = true;
        [SerializeField] private Bounds _worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
        [SerializeField] private int _maxDepth = 8;
        [SerializeField] private int _maxObjectsPerNode = 10;
        [SerializeField] private bool _autoResize = true;

        [Header("性能优化")]
        [SerializeField] private bool _enableBatching = true;
        [SerializeField] private int _batchSize = 100;
        [SerializeField] private float _updateInterval = 0.1f;
        [SerializeField] private bool _enableAsyncQueries = true;

        [Header("调试信息")]
        [SerializeField] private bool _showDebugInfo = false;
        [SerializeField] private bool _showBounds = false;
        [SerializeField] private Color _boundsColor = Color.green;

        // 服务委託
        private ISpatialIndexService _spatialIndexService;
        
        // 配置
        private SpatialIndexConfigSO _config;
        
        // 兼容性保持 - 保留原有字段用于向后兼容
        private ISpatialIndex<SpatialNode> _spatialIndex;
        private Dictionary<string, ISpatialIndex<SpatialNode>> _categoryIndices;
        private Dictionary<int, SpatialNode> _allNodes;
        private Queue<SpatialNode> _pendingInserts;
        private Queue<SpatialNode> _pendingUpdates;
        private Queue<SpatialNode> _pendingRemovals;
        private int _totalQueries = 0;
        private float _totalQueryTime = 0f;
        private int _frameQueries = 0;
        
        // 事件
        public event System.Action<SpatialNode> OnNodeAdded;
        public event System.Action<SpatialNode> OnNodeRemoved;
        public event System.Action<SpatialNode> OnNodeUpdated;

        // IManager接口实现
        public bool IsInitialized { get; private set; }
        public string ManagerName => "SpatialIndexManager";

        // 公共屬性（用於兼容性）
        public Bounds WorldBounds => _worldBounds;
        public int BatchSize => _batchSize;
        public float UpdateInterval => _updateInterval;
        public bool ShowBounds => _showBounds;
        public Color BoundsColor => _boundsColor;
        public bool ShowDebugInfo => _showDebugInfo;

        /// <summary>
        /// 初始化管理器
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized) return;

            // 加载配置
            LoadConfiguration();

            // 创建并初始化空间索引服务
            _spatialIndexService = new SpatialIndexService();
            _spatialIndexService.Initialize(
                worldBounds: _worldBounds,
                maxDepth: _maxDepth,
                maxObjectsPerNode: _maxObjectsPerNode,
                useOctree: _useOctree,
                batchSize: _batchSize
            );

            // 兼容性保持 - 初始化原有数据结构
            _categoryIndices = new Dictionary<string, ISpatialIndex<SpatialNode>>();
            _allNodes = new Dictionary<int, SpatialNode>();
            _pendingInserts = new Queue<SpatialNode>();
            _pendingUpdates = new Queue<SpatialNode>();
            _pendingRemovals = new Queue<SpatialNode>();

            // 启动更新协程
            if (_enableBatching)
            {
                StartCoroutine(BatchUpdateCoroutine());
            }

            IsInitialized = true;
            Debug.Log($"[{ManagerName}] 初始化完成 - 使用服务委託模式，{(_useOctree ? "八叉树" : "四叉树")}索引");
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        private void LoadConfiguration()
        {
            _config = ConfigManager.Instance.GetConfig<SpatialIndexConfigSO>("SpatialIndexConfig");
            
            if (_config == null)
            {
                Debug.LogWarning($"[{ManagerName}] 未找到SpatialIndexConfig配置文件，使用默认值");
            }
            else
            {
                Debug.Log($"[{ManagerName}] 成功加载空间索引配置: {_config.name}");
            }
        }

        /// <summary>
        /// 更新管理器
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        private void Update()
        {
            UpdateManager();
        }

        /// <summary>
        /// 更新管理器
        /// </summary>
        public void UpdateManager()
        {
            if (!IsInitialized) return;

            // 委託給服務更新
            _spatialIndexService?.UpdateService(Time.deltaTime);

            // 兼容性保持 - 處理原有待處理操作
            if (!_enableBatching)
            {
                ProcessPendingOperations();
            }

            // 重置帧查询计数
            _frameQueries = 0;
        }

        /// <summary>
        /// 固定更新管理器
        /// </summary>
        /// <param name="fixedDeltaTime">固定时间增量</param>
        private void FixedUpdate()
        {
            // 固定更新逻辑
        }

        // IUpdatable接口实现
        void IUpdatable.Update(float deltaTime)
        {
            UpdateManager();
        }

        // IFixedUpdatable接口实现
        void IFixedUpdatable.FixedUpdate(float fixedDeltaTime)
        {
            // 需要時加入固定更新邏輯
        }

        // ILateUpdatable接口实现
        void ILateUpdatable.LateUpdate(float deltaTime)
        {
            // 需要時加入後更新邏輯
        }

        // IManager接口实现
        void IManager.Update(float deltaTime)
        {
            UpdateManager();
        }

        void IManager.FixedUpdate(float fixedDeltaTime)
        {
            // 需要時加入固定更新邏輯
        }

        void IManager.LateUpdate(float deltaTime)
        {
            // 需要時加入後更新邏輯
        }

        public bool IsPaused { get; private set; }

        /// <summary>
        /// 暂停管理器
        /// </summary>
        public void Pause()
        {
            // 暂停逻辑
        }

        /// <summary>
        /// 恢复管理器
        /// </summary>
        public void Resume()
        {
            // 恢复逻辑
        }

        /// <summary>
        /// 获取管理器名称
        /// </summary>
        /// <returns>管理器名称</returns>
        public string GetManagerName()
        {
            return ManagerName;
        }

        /// <summary>
        /// 获取服务实例
        /// </summary>
        /// <typeparam name="T">服务接口类型</typeparam>
        /// <returns>服务实例，如果不存在则返回null</returns>
        public T GetService<T>() where T : class
        {
            if (typeof(T) == typeof(ISpatialIndexService))
                return _spatialIndexService as T;
            
            return null;
        }

        /// <summary>
        /// 清理管理器
        /// </summary>
        public void Cleanup()
        {
            if (!IsInitialized) return;

            // 委託給服務清理
            _spatialIndexService?.Cleanup();

            // 兼容性保持 - 清理原有數據結構
            _spatialIndex?.Clear();
            foreach (var index in _categoryIndices.Values)
            {
                index?.Clear();
            }

            _categoryIndices.Clear();
            _allNodes.Clear();
            _pendingInserts.Clear();
            _pendingUpdates.Clear();
            _pendingRemovals.Clear();

            IsInitialized = false;
            Debug.Log($"[{ManagerName}] 清理完成 - 服務委託模式");
        }

        /// <summary>
        /// <summary>
        /// 处理待处理的操作
        /// </summary>
        private void ProcessPendingOperations()
        {
            // 处理插入
            int insertCount = 0;
            while (_pendingInserts.Count > 0 && insertCount < BatchSize)
            {
                var node = _pendingInserts.Dequeue();
                InsertNodeImmediate(node);
                insertCount++;
            }

            // 处理更新
            int updateCount = 0;
            while (_pendingUpdates.Count > 0 && updateCount < BatchSize)
            {
                var node = _pendingUpdates.Dequeue();
                UpdateNodeImmediate(node, node.Position); // 简化处理
                updateCount++;
            }

            // 处理移除
            int removeCount = 0;
            while (_pendingRemovals.Count > 0 && removeCount < BatchSize)
            {
                var node = _pendingRemovals.Dequeue();
                RemoveNodeImmediate(node);
                removeCount++;
            }
        }

        /// <summary>
        /// 批量更新协程
        /// </summary>
        private System.Collections.IEnumerator BatchUpdateCoroutine()
        {
            while (IsInitialized)
            {
                ProcessPendingOperations();
                yield return new WaitForSeconds(UpdateInterval);
            }
        }

        /// <summary>
        /// 更新查询统计
        /// </summary>
        /// <param name="queryTime">查询时间</param>
        private void UpdateQueryStats(float queryTime)
        {
            _totalQueries++;
            _totalQueryTime += queryTime;
            _frameQueries++;
        }

        // Unity生命周期
        // Unity生命周期
        private void OnDrawGizmos()
        {
            if (!ShowBounds || !IsInitialized) return;

            Gizmos.color = BoundsColor;
            Gizmos.DrawWireCube(WorldBounds.center, WorldBounds.size);
        }

        private void OnGUI()
        {
            if (!ShowDebugInfo || !IsInitialized) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label(GetPerformanceStats());
            GUILayout.EndArea();
        }

    }
}