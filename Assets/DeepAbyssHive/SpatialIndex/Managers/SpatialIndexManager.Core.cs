using System.Collections.Generic;
using System.Text;
using UnityEngine;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.SpatialIndex.Config;
using DeepAbyssHive.SpatialIndex.Data;
using DeepAbyssHive.SpatialIndex.Implementations;
using DeepAbyssHive.Core.Logging;
using ISpatialIndex = DeepAbyssHive.SpatialIndex.Interfaces.ISpatialIndex;

namespace DeepAbyssHive.SpatialIndex.Managers
{
    /// <summary>
    /// 空间索引管理器核心部分 - 数据结构、初始化/清理、生命周期管理
    /// </summary>
    public partial class SpatialIndexManager : MonoBehaviour, IManager, IUpdatable, IFixedUpdatable, ILateUpdatable
    {
        [Header("空间索引配置")]
        [SerializeField] private bool _useOctree = true;
        [SerializeField] private Bounds _worldBounds = new Bounds(Vector3.zero, new Vector3(1000f, 1000f, 1000f));
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

        // 空间索引实例
        private DeepAbyssHive.SpatialIndex.Interfaces.ISpatialIndex _spatialIndex;
        private Dictionary<string, DeepAbyssHive.SpatialIndex.Interfaces.ISpatialIndex> _categoryIndices;
        
        // 对象管理
        private Dictionary<int, SpatialNode> _allNodes;
        private Queue<SpatialNode> _pendingInserts;
        private Queue<SpatialNode> _pendingUpdates;
        private Queue<SpatialNode> _pendingRemovals;
        
        // 性能统计
        private int _totalQueries = 0;
        private float _totalQueryTime = 0f;
        private int _frameQueries = 0;
        
        // 配置
        private SpatialIndexConfigSO _config;
        
        // 事件
        public event System.Action<SpatialNode> OnNodeAdded;
        public event System.Action<SpatialNode> OnNodeRemoved;
        public event System.Action<SpatialNode> OnNodeUpdated;

        // IManager接口实现
        public bool IsInitialized { get; private set; }
        public string ManagerName => "SpatialIndexManager";
    
        // === IManager / IUpdatable / IFixedUpdatable / ILateUpdatable 实现 ===
        private bool _isPaused = false;

        // IManager 介面實作
        // 內部實作方法
        public void TickUpdate(float dt)
        {
            if (!IsInitialized || _isPaused) return;
            ProcessPendingOperations();
            // 目前 SpatialIndex 不需要逐帧更新；必要時可在此轉發到 _activeIndex
        }

        public void TickFixedUpdate(float fixedDt)
        {
            if (!IsInitialized || _isPaused) return;
            // 目前 SpatialIndex 無固定步進需求
        }

        public void TickLateUpdate(float dt)
        {
            if (!IsInitialized || _isPaused) return;
            // 目前 SpatialIndex 無 LateUpdate 需求
        }

        // --- Explicit interface forwarding ---
        void DeepAbyssHive.Core.Interfaces.IUpdatable.Update(float dt) => TickUpdate(dt);
        void DeepAbyssHive.Core.Interfaces.IFixedUpdatable.FixedUpdate(float dt) => TickFixedUpdate(dt);
        void DeepAbyssHive.Core.Interfaces.ILateUpdatable.LateUpdate(float dt) => TickLateUpdate(dt);

        void DeepAbyssHive.Core.Interfaces.IManager.Update(float dt) => TickUpdate(dt);
        void DeepAbyssHive.Core.Interfaces.IManager.FixedUpdate(float dt) => TickFixedUpdate(dt);
        void DeepAbyssHive.Core.Interfaces.IManager.LateUpdate(float dt) => TickLateUpdate(dt);

        public void Pause()  { _isPaused = true;  }
        public void Resume() { _isPaused = false; }

        public string GetManagerName() => ManagerName;

                
        // 属性
        public bool UseOctree => _useOctree;

        /// <summary>
        /// <summary>
        /// 初始化管理器
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized) return;

            // 加载配置
            LoadConfiguration();

            // 创建主空间索引
            if (UseOctree)
            {
                _spatialIndex = new OctreeSpatialIndex(_worldBounds, _maxDepth, _maxObjectsPerNode);
            }
            else
            {
                _spatialIndex = new QuadTreeSpatialIndex(_worldBounds.size.x, _maxDepth, _maxObjectsPerNode);
            }

            // 初始化数据结构
            _categoryIndices = new Dictionary<string, ISpatialIndex>();
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
            DAHLog.Info(LogCategory.SYSTEM, $"[{ManagerName}] 初始化完成 - 使用{(_useOctree ? "八叉树" : "四叉树")}索引");
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        private void LoadConfiguration()
        {
            _config = ConfigManager.Instance.GetConfig<SpatialIndexConfigSO>();
            
            if (_config == null)
            {
                DAHLog.Warning(LogCategory.SYSTEM, $"[{ManagerName}] 未找到SpatialIndexConfig配置文件，使用默认值");
            }
            else
            {
                DAHLog.Info(LogCategory.SYSTEM, $"[{ManagerName}] 成功加载空间索引配置: {_config.name}");
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            if (!IsInitialized) return;

            _categoryIndices.Clear();
            _allNodes.Clear();
            _pendingInserts.Clear();
            _pendingUpdates.Clear();
            _pendingRemovals.Clear();

            IsInitialized = false;
            DAHLog.Info(LogCategory.SYSTEM, $"[{ManagerName}] 清理完成");
        }
    

        /// <summary>
        /// <summary>
        /// 处理待处理的操作
        /// </summary>
        private void ProcessPendingOperations()
        {
            // 处理插入
            int insertCount = 0;
            while (_pendingInserts.Count > 0 && insertCount < _batchSize)
            {
                var node = _pendingInserts.Dequeue();
                InsertNodeImmediate(node);
                insertCount++;
            }

            // 处理更新
            int updateCount = 0;
            while (_pendingUpdates.Count > 0 && updateCount < _batchSize)
            {
                var node = _pendingUpdates.Dequeue();
                UpdateNodeImmediate(node, node.Position); // 简化处理
                updateCount++;
            }

            // 处理移除
            int removeCount = 0;
            while (_pendingRemovals.Count > 0 && removeCount < _batchSize)
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
                yield return new WaitForSeconds(_updateInterval);
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
            if (!_showBounds || !IsInitialized) return;

            Gizmos.color = _boundsColor;
            Gizmos.DrawWireCube(_worldBounds.center, _worldBounds.size);
        }

        private void OnGUI()
        {
            if (!_showDebugInfo || !IsInitialized) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label(GetPerformanceStats());
            GUILayout.EndArea();
        }
        
    }
}