using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Core.Managers
{
    /// <summary>
    /// 游戏主管理器 - 性能模块
    /// 负责FPS监控、查询计时、统计数据收集
    /// </summary>
    public partial class GameManager
    {
        #region 性能监控配置
        
        [Header("性能监控")]
        [SerializeField] private bool _enablePerformanceMonitoring = true;
        [SerializeField] private float _performanceUpdateInterval = 1.0f;
        [SerializeField] private int _performanceHistorySize = 60;
        [SerializeField] private bool _enableDetailedProfiling = false;
        
        #endregion
        
        #region 性能数据
        
        // FPS监控
        private float _frameTime = 0f;
        private float _updateTime = 0f;
        private float _fixedUpdateTime = 0f;
        private float _lateUpdateTime = 0f;
        private float _renderTime = 0f;
        
        private float _currentFPS = 0f;
        private float _averageFPS = 0f;
        private float _minFPS = float.MaxValue;
        private float _maxFPS = 0f;
        
        // 性能历史记录
        private Queue<float> _fpsHistory = new Queue<float>();
        private Queue<float> _frameTimeHistory = new Queue<float>();
        private Queue<float> _updateTimeHistory = new Queue<float>();
        
        // 计时器
        private float _performanceTimer = 0f;
        private System.Diagnostics.Stopwatch _updateStopwatch = new System.Diagnostics.Stopwatch();
        private System.Diagnostics.Stopwatch _fixedUpdateStopwatch = new System.Diagnostics.Stopwatch();
        private System.Diagnostics.Stopwatch _lateUpdateStopwatch = new System.Diagnostics.Stopwatch();
        
        // 内存监控
        private long _totalMemory = 0;
        private long _usedMemory = 0;
        private long _gcMemory = 0;
        private int _gcCollectionCount = 0;
        
        // 查询统计
        private Dictionary<string, QueryStats> _queryStats = new Dictionary<string, QueryStats>();
        
        #endregion
        
        #region 性能属性
        
        public bool EnablePerformanceMonitoring => _enablePerformanceMonitoring;
        public float CurrentFPS => _currentFPS;
        public float AverageFPS => _averageFPS;
        public float MinFPS => _minFPS;
        public float MaxFPS => _maxFPS;
        public float FrameTime => _frameTime;
        public float UpdateTime => _updateTime;
        public float FixedUpdateTime => _fixedUpdateTime;
        public float LateUpdateTime => _lateUpdateTime;
        public float RenderTime => _renderTime;
        public long TotalMemory => _totalMemory;
        public long UsedMemory => _usedMemory;
        public long GCMemory => _gcMemory;
        public int GCCollectionCount => _gcCollectionCount;
        
        #endregion
        
        #region 性能监控初始化
        
        /// <summary>
        /// 初始化性能监控系统
        /// </summary>
        private void InitializePerformanceMonitoring()
        {
            if (!_enablePerformanceMonitoring) return;
            
            DAHLog.Info(LogCategory.MANAGER, "[GameManager] 初始化性能监控系统...");
            
            try
            {
                // 初始化计时器
                _updateStopwatch = new System.Diagnostics.Stopwatch();
                _fixedUpdateStopwatch = new System.Diagnostics.Stopwatch();
                _lateUpdateStopwatch = new System.Diagnostics.Stopwatch();
                
                // 初始化历史记录队列
                _fpsHistory.Clear();
                _frameTimeHistory.Clear();
                _updateTimeHistory.Clear();
                
                // 重置统计数据
                ResetPerformanceStats();
                
                DAHLog.Info(LogCategory.MANAGER, "[GameManager] 性能监控系统初始化完成");
            }
            catch (Exception ex)
            {
                DAHLog.Error(LogCategory.MANAGER, $"[GameManager] 性能监控系统初始化失败: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 性能数据更新
        
        /// <summary>
        /// 更新性能监控数据
        /// </summary>
        private void UpdatePerformanceMonitoring()
        {
            if (!_enablePerformanceMonitoring) return;
            
            _performanceTimer += Time.unscaledDeltaTime;
            
            // 每帧更新基础数据
            UpdateFramePerformance();
            
            // 定期更新详细统计
            if (_performanceTimer >= _performanceUpdateInterval)
            {
                UpdateDetailedPerformance();
                _performanceTimer = 0f;
            }
        }
        
        /// <summary>
        /// 更新帧性能数据
        /// </summary>
        private void UpdateFramePerformance()
        {
            // 计算FPS
            _frameTime = Time.unscaledDeltaTime;
            _currentFPS = 1f / _frameTime;
            
            // 更新FPS统计
            if (_currentFPS < _minFPS) _minFPS = _currentFPS;
            if (_currentFPS > _maxFPS) _maxFPS = _currentFPS;
            
            // 添加到历史记录
            _fpsHistory.Enqueue(_currentFPS);
            _frameTimeHistory.Enqueue(_frameTime);
            
            // 限制历史记录大小
            while (_fpsHistory.Count > _performanceHistorySize)
            {
                _fpsHistory.Dequeue();
            }
            while (_frameTimeHistory.Count > _performanceHistorySize)
            {
                _frameTimeHistory.Dequeue();
            }
            
            // 计算平均FPS
            float totalFPS = 0f;
            foreach (float fps in _fpsHistory)
            {
                totalFPS += fps;
            }
            _averageFPS = totalFPS / _fpsHistory.Count;
        }
        
        /// <summary>
        /// 更新详细性能数据
        /// </summary>
        private void UpdateDetailedPerformance()
        {
            // 更新内存使用情况
            UpdateMemoryStats();
            
            // 更新渲染统计
            UpdateRenderStats();
            
            // 如果启用详细分析，输出性能报告
            if (_enableDetailedProfiling)
            {
                LogPerformanceReport();
            }
        }
        
        /// <summary>
        /// 更新内存统计
        /// </summary>
        private void UpdateMemoryStats()
        {
            try
            {
                _totalMemory = Profiler.GetTotalAllocatedMemory();
                _usedMemory = Profiler.GetTotalReservedMemory();
                _gcMemory = GC.GetTotalMemory(false);
                _gcCollectionCount = GC.CollectionCount(0);
            }
            catch (Exception ex)
            {
                DAHLog.Warning(LogCategory.MANAGER, $"[GameManager] 更新内存统计失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 更新渲染统计
        /// </summary>
        private void UpdateRenderStats()
        {
            try
            {
                // 这里可以添加渲染相关的统计
                // 例如：DrawCall数量、三角形数量等
            }
            catch (Exception ex)
            {
                DAHLog.Warning(LogCategory.MANAGER, $"[GameManager] 更新渲染统计失败: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 查询性能统计
        
        /// <summary>
        /// 开始查询计时
        /// </summary>
        public void BeginQueryTiming(string queryName)
        {
            if (!_enablePerformanceMonitoring) return;
            
            if (!_queryStats.ContainsKey(queryName))
            {
                _queryStats[queryName] = new QueryStats();
            }
            
            _queryStats[queryName].StartTiming();
        }
        
        /// <summary>
        /// 结束查询计时
        /// </summary>
        public void EndQueryTiming(string queryName)
        {
            if (!_enablePerformanceMonitoring) return;
            
            if (_queryStats.TryGetValue(queryName, out var stats))
            {
                stats.EndTiming();
            }
        }
        
        /// <summary>
        /// 获取查询统计
        /// </summary>
        public QueryStats GetQueryStats(string queryName)
        {
            return _queryStats.TryGetValue(queryName, out var stats) ? stats : null;
        }
        
        /// <summary>
        /// 获取所有查询统计
        /// </summary>
        public Dictionary<string, QueryStats> GetAllQueryStats()
        {
            return new Dictionary<string, QueryStats>(_queryStats);
        }
        
        #endregion
        
        #region 性能报告
        
        /// <summary>
        /// 输出性能报告
        /// </summary>
        private void LogPerformanceReport()
        {
            var report = GeneratePerformanceReport();
            DAHLog.Info(LogCategory.MANAGER, $"[GameManager] 性能报告:\n{report}");
        }
        
        /// <summary>
        /// 生成性能报告
        /// </summary>
        public string GeneratePerformanceReport()
        {
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("=== 游戏性能报告 ===");
            report.AppendLine($"当前FPS: {_currentFPS:F1}");
            report.AppendLine($"平均FPS: {_averageFPS:F1}");
            report.AppendLine($"最低FPS: {_minFPS:F1}");
            report.AppendLine($"最高FPS: {_maxFPS:F1}");
            report.AppendLine($"帧时间: {_frameTime * 1000f:F2}ms");
            report.AppendLine($"更新时间: {_updateTime * 1000f:F2}ms");
            report.AppendLine($"固定更新时间: {_fixedUpdateTime * 1000f:F2}ms");
            report.AppendLine($"后更新时间: {_lateUpdateTime * 1000f:F2}ms");
            
            report.AppendLine("\n=== 内存使用 ===");
            report.AppendLine($"总内存: {_totalMemory / 1024 / 1024:F1}MB");
            report.AppendLine($"已用内存: {_usedMemory / 1024 / 1024:F1}MB");
            report.AppendLine($"GC内存: {_gcMemory / 1024 / 1024:F1}MB");
            report.AppendLine($"GC回收次数: {_gcCollectionCount}");
            
            if (_queryStats.Count > 0)
            {
                report.AppendLine("\n=== 查询统计 ===");
                foreach (var kvp in _queryStats)
                {
                    var stats = kvp.Value;
                    report.AppendLine($"{kvp.Key}: 调用{stats.CallCount}次, 平均{stats.AverageTime:F2}ms, 总计{stats.TotalTime:F2}ms");
                }
            }
            
            return report.ToString();
        }
        
        /// <summary>
        /// 重置性能统计
        /// </summary>
        public void ResetPerformanceStats()
        {
            _minFPS = float.MaxValue;
            _maxFPS = 0f;
            _fpsHistory.Clear();
            _frameTimeHistory.Clear();
            _updateTimeHistory.Clear();
            _queryStats.Clear();
            _gcCollectionCount = GC.CollectionCount(0);
            
            DAHLog.Info(LogCategory.MANAGER, "[GameManager] 性能统计已重置");
        }
        
        /// <summary>
        /// 获取性能统计摘要
        /// </summary>
        public string GetPerformanceSummary()
        {
            return $"FPS: {_currentFPS:F1} | 帧时间: {_frameTime * 1000f:F1}ms | 内存: {_usedMemory / 1024 / 1024:F0}MB | Tick: {_currentTick}";
        }
        
        #endregion
        
        #region 性能监控清理
        
        /// <summary>
        /// 清理性能监控系统
        /// </summary>
        private void CleanupPerformanceMonitoring()
        {
            if (!_enablePerformanceMonitoring) return;
            
            DAHLog.Info(LogCategory.MANAGER, "[GameManager] 清理性能监控系统...");
            
            try
            {
                _updateStopwatch?.Stop();
                _fixedUpdateStopwatch?.Stop();
                _lateUpdateStopwatch?.Stop();
                
                _fpsHistory.Clear();
                _frameTimeHistory.Clear();
                _updateTimeHistory.Clear();
                _queryStats.Clear();
                
                DAHLog.Info(LogCategory.MANAGER, "[GameManager] 性能监控系统清理完成");
            }
            catch (Exception ex)
            {
                DAHLog.Error(LogCategory.MANAGER, $"[GameManager] 性能监控系统清理失败: {ex.Message}");
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// 查询统计数据
    /// </summary>
    public class QueryStats
    {
        private System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();
        private float _totalTime = 0f;
        private int _callCount = 0;
        
        public float TotalTime => _totalTime;
        public int CallCount => _callCount;
        public float AverageTime => _callCount > 0 ? _totalTime / _callCount : 0f;
        
        public void StartTiming()
        {
            _stopwatch.Restart();
        }
        
        public void EndTiming()
        {
            _stopwatch.Stop();
            _totalTime += (float)_stopwatch.Elapsed.TotalMilliseconds;
            _callCount++;
        }
        
        public void Reset()
        {
            _totalTime = 0f;
            _callCount = 0;
            _stopwatch.Reset();
        }
    }
}
