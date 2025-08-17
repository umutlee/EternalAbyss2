using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using DeepAbyssHive.SpatialIndex.Data;
using DeepAbyssHive.SpatialIndex.Enums;

namespace DeepAbyssHive.SpatialIndex.Managers
{
    /// <summary>
    /// 空间索引管理器API部分 - 对外接口、查询操作、性能统计
    /// </summary>
    public partial class SpatialIndexManager
    {
        /// <summary>
        /// 添加节点到空间索引
        /// </summary>
        /// <param name="obj">游戏对象</param>
        /// <param name="category">分类</param>
        /// <param name="bounds">边界</param>
        /// <returns>节点ID</returns>
        public int AddNode(GameObject obj, string category = "default", Bounds? bounds = null)
        {
            if (!IsInitialized || obj == null) return -1;

            var actualBounds = bounds ?? new Bounds(obj.transform.position, Vector3.one);
            var objectId = obj.GetInstanceID();
            
            // 委託給服務
            var objectType = GetSpatialObjectType(category);
            bool success = _spatialIndexService?.AddObject(objectId, obj.transform.position, actualBounds, objectType) ?? false;
            
            if (success)
            {
                // 兼容性保持 - 維護原有數據結構
                var node = new SpatialNode(objectId, obj, obj.transform.position, actualBounds, category, 0, false);
                _allNodes[objectId] = node;
                OnNodeAdded?.Invoke(node);
                return objectId;
            }

            return -1;
        }

        /// <summary>
        /// 立即插入节点
        /// </summary>
        /// <param name="node">节点</param>
        private void InsertNodeImmediate(SpatialNode node)
        {
            _allNodes[node.Id] = node;
            _spatialIndex.Insert(node, node.Position, node.Bounds.size);

            // 添加到分类索引
            if (!_categoryIndices.ContainsKey(node.Category))
            {
                if (_useOctree)
                {
                    // TODO: 需要实现OctreeSpatialIndex类或使用现有的空间索引实现
                    Debug.LogWarning("[SpatialIndexManager] OctreeSpatialIndex类型未找到，使用默认索引");
                }
                else
                {
                    // TODO: 需要实现QuadTreeSpatialIndex类或使用现有的空间索引实现
                    Debug.LogWarning("[SpatialIndexManager] QuadTreeSpatialIndex类型未找到，使用默认索引");
                }
            }
            _categoryIndices[node.Category].Insert(node, node.Position, node.Bounds.size);

            OnNodeAdded?.Invoke(node);
        }

        /// <summary>
        /// 更新节点位置
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <param name="newPosition">新位置</param>
        /// <param name="newBounds">新边界</param>
        public void UpdateNode(int nodeId, Vector3 newPosition, Bounds? newBounds = null)
        {
            if (!IsInitialized || !_allNodes.ContainsKey(nodeId)) return;

            var node = _allNodes[nodeId];
            // 注意：Position和Bounds可能是只读属性，需要通过方法更新
            // node.Position = newPosition;
            // if (newBounds.HasValue)
            // {
            //     node.Bounds = newBounds.Value;
            // }
            
            // 使用更新方法替代直接赋值
            node.UpdatePosition(newPosition);
            if (newBounds.HasValue)
            {
                node.UpdateBounds(newBounds.Value);
            }

            if (_enableBatching)
            {
                _pendingUpdates.Enqueue(node);
            }
            else
            {
                UpdateNodeImmediate(node, newPosition);
            }
        }

        /// <summary>
        /// 立即更新节点
        /// </summary>
        /// <param name="node">节点</param>
        /// <param name="newPosition">新位置</param>
        private void UpdateNodeImmediate(SpatialNode node, Vector3 newPosition)
        {
            Vector3 oldPosition = node.Position;
            Vector3 size = node.Bounds.size;
            
            // 从索引中移除
            _spatialIndex.Remove(node, oldPosition, size);
            if (_categoryIndices.ContainsKey(node.Category))
            {
                _categoryIndices[node.Category].Remove(node, oldPosition, size);
            }

            // 更新位置
            node.UpdatePosition(newPosition);
            if (node.GameObject != null)
            {
                node.UpdateBounds(new Bounds(newPosition, size));
            }

            // 重新插入
            _spatialIndex.Insert(node, newPosition, size);
            if (_categoryIndices.ContainsKey(node.Category))
            {
                _categoryIndices[node.Category].Insert(node, newPosition, size);
            }

            OnNodeUpdated?.Invoke(node);
        }

        /// <summary>
        /// 移除节点
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        public void RemoveNode(int nodeId)
        {
            if (!IsInitialized || !_allNodes.ContainsKey(nodeId)) return;

            var node = _allNodes[nodeId];

            if (_enableBatching)
            {
                _pendingRemovals.Enqueue(node);
            }
            else
            {
                RemoveNodeImmediate(node);
            }
        }

        /// <summary>
        /// 立即移除节点
        /// </summary>
        /// <param name="node">节点</param>
        private void RemoveNodeImmediate(SpatialNode node)
        {
            _spatialIndex.Remove(node, node.Position, node.Bounds.size);
            if (_categoryIndices.ContainsKey(node.Category))
            {
                _categoryIndices[node.Category].Remove(node, node.Position, node.Bounds.size);
            }

            _allNodes.Remove(node.Id);
            OnNodeRemoved?.Invoke(node);
        }

        /// <summary>
        /// 范围查询
        /// </summary>
        /// <param name="center">中心点</param>
        /// <param name="radius">半径</param>
        /// <param name="category">分类过滤</param>
        /// <param name="includeInactive">是否包含非活跃对象</param>
        /// <returns>查询结果</returns>
        public List<SpatialNode> QueryRange(Vector3 center, float radius, string category = null, bool includeInactive = false)
        {
            if (!IsInitialized)
            {
                return new List<SpatialNode>();
            }

            var startTime = Time.realtimeSinceStartup;
            var bounds = new Bounds(center, Vector3.one * radius * 2);
            
            List<SpatialNode> results;
            
            if (!string.IsNullOrEmpty(category) && _categoryIndices.ContainsKey(category))
            {
                results = _categoryIndices[category].QueryRange(bounds.center, bounds.size);
            }
            else
            {
                results = _spatialIndex.QueryRange(bounds.center, bounds.size);
            }

            // 过滤结果
            var filteredResults = results.Where(node => 
            {
                if (!includeInactive && node.HasTag("inactive")) return false;
                if (!string.IsNullOrEmpty(category) && node.Category != category) return false;
                
                var distance = Vector3.Distance(node.Position, center);
                return distance <= radius;
            }).ToList();

            var queryTime = Time.realtimeSinceStartup - startTime;
            UpdateQueryStats(queryTime);

            return filteredResults;
        }

        /// <summary>
        /// 边界查询
        /// </summary>
        /// <param name="bounds">查询边界</param>
        /// <param name="category">分类过滤</param>
        /// <param name="includeInactive">是否包含非活跃对象</param>
        /// <returns>查询结果</returns>
        public List<SpatialNode> QueryBounds(Bounds bounds, string category = null, bool includeInactive = false)
        {
            if (!IsInitialized)
            {
                return new List<SpatialNode>();
            }

            var startTime = Time.realtimeSinceStartup;
            
            List<SpatialNode> results;
            
            if (!string.IsNullOrEmpty(category) && _categoryIndices.ContainsKey(category))
            {
                results = _categoryIndices[category].QueryRange(bounds.center, bounds.size);
            }
            else
            {
                results = _spatialIndex.QueryRange(bounds.center, bounds.size);
            }

            // 过滤结果
            var filteredResults = results.Where(node => 
            {
                if (!includeInactive && node.HasTag("inactive")) return false;
                if (!string.IsNullOrEmpty(category) && node.Category != category) return false;
                return bounds.Intersects(node.Bounds);
            }).ToList();

            var queryTime = Time.realtimeSinceStartup - startTime;
            UpdateQueryStats(queryTime);

            return filteredResults;
        }

        /// <summary>
        /// 最近邻查询
        /// </summary>
        /// <param name="position">查询位置</param>
        /// <param name="count">返回数量</param>
        /// <param name="category">分类过滤</param>
        /// <param name="includeInactive">是否包含非活跃对象</param>
        /// <returns>最近的节点</returns>
        public List<SpatialNode> QueryNearest(Vector3 position, int count = 1, string category = null, bool includeInactive = false)
        {
            if (!IsInitialized || count <= 0)
            {
                return new List<SpatialNode>();
            }

            var startTime = Time.realtimeSinceStartup;
            
            // 使用较大的查询范围
            var searchRadius = 100f;
            var candidates = QueryRange(position, searchRadius, category, includeInactive);
            
            if (candidates.Count == 0)
            {
                return new List<SpatialNode>();
            }

            // 按距离排序
            var sortedCandidates = candidates.OrderBy(node => 
                Vector3.Distance(node.Position, position)).Take(count).ToList();

            var queryTime = Time.realtimeSinceStartup - startTime;
            UpdateQueryStats(queryTime);

            return sortedCandidates;
        }

        /// <summary>
        /// 优化空间索引
        /// </summary>
        public void Optimize()
        {
            if (!IsInitialized) return;

            // TODO: 需要检查ISpatialIndex接口是否包含Optimize方法
            // _spatialIndex.Optimize();
            // foreach (var index in _categoryIndices.Values)
            // {
            //     index.Optimize();
            // }
            
            Debug.Log($"[{ManagerName}] 空间索引优化方法暂时禁用，等待接口实现");

            Debug.Log($"[{ManagerName}] 空间索引优化完成");
        }

        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        /// <returns>性能统计字符串</returns>
        public string GetPerformanceStats()
        {
            if (!IsInitialized) return "未初始化";

            var avgQueryTime = _totalQueries > 0 ? _totalQueryTime / _totalQueries * 1000f : 0f;
            
            return $"空间索引统计:\n" +
                   $"总对象数: {_allNodes.Count}\n" +
                   $"总查询数: {_totalQueries}\n" +
                   $"平均查询时间: {avgQueryTime:F2}ms\n" +
                   $"本帧查询数: {_frameQueries}\n" +
                   $"待处理插入: {_pendingInserts.Count}\n" +
                   $"待处理更新: {_pendingUpdates.Count}\n" +
                   $"待处理移除: {_pendingRemovals.Count}";
        }

        /// <summary>
        /// 获取节点信息
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>节点信息</returns>
        public SpatialNode GetNode(int nodeId)
        {
            if (!IsInitialized || !_allNodes.ContainsKey(nodeId))
                return null;

            return _allNodes[nodeId];
        }

        /// <summary>
        /// 获取所有分类
        /// </summary>
        /// <returns>分类列表</returns>
        public string[] GetCategories()
        {
            if (!IsInitialized) return new string[0];
            return _categoryIndices.Keys.ToArray();
        }

        /// <summary>
        /// 获取分类中的对象数量
        /// </summary>
        /// <param name="category">分类</param>
        /// <returns>对象数量</returns>
        public int GetCategoryCount(string category)
        {
            if (!IsInitialized || !_categoryIndices.ContainsKey(category))
                return 0;

            return _allNodes.Values.Count(node => node.Category == category);
        }

        /// <summary>
        /// 清空指定分类
        /// </summary>
        /// <param name="category">分类</param>
        public void ClearCategory(string category)
        {
            if (!IsInitialized) return;

            // 委託給服務
            var objectType = GetSpatialObjectType(category);
            _spatialIndexService?.Clear(objectType);

            // 兼容性保持 - 清理原有數據結構
            if (_categoryIndices.ContainsKey(category))
            {
                var nodesToRemove = _allNodes.Values.Where(node => node.Category == category).ToList();
                foreach (var node in nodesToRemove)
                {
                    _allNodes.Remove(node.Id);
                    OnNodeRemoved?.Invoke(node);
                }
                Debug.Log($"[{ManagerName}] 已清空分类: {category}, 移除了 {nodesToRemove.Count} 个对象");
            }
        }

        /// <summary>
        /// 輔助方法：將分類字符串轉換為SpatialObjectType
        /// </summary>
        /// <param name="category">分類字符串</param>
        /// <returns>對應的SpatialObjectType</returns>
        private SpatialObjectType GetSpatialObjectType(string category)
        {
            if (string.IsNullOrEmpty(category) || category == "default")
                return SpatialObjectType.All;
                
            // 根據分類名稱映射到對應的枚舉值
            switch (category.ToLower())
            {
                case "unit":
                case "units":
                    return SpatialObjectType.Unit;
                case "building":
                case "buildings":
                    return SpatialObjectType.Building;
                case "resource":
                case "resources":
                    return SpatialObjectType.Resource;
                case "terrain":
                    return SpatialObjectType.Terrain;
                case "effect":
                case "effects":
                    return SpatialObjectType.Effect;
                default:
                    return SpatialObjectType.All;
            }
        }
    }
}