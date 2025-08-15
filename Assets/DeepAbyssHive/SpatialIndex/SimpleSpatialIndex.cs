using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Creep.Data;

namespace DeepAbyssHive.SpatialIndex
{
    /// <summary>
    /// 简单空间索引实现
    /// </summary>
    public class SimpleSpatialIndex
    {
        private Dictionary<Vector3, List<CreepData>> _spatialGrid = new Dictionary<Vector3, List<CreepData>>();
        private float _cellSize = 10f;
        
        /// <summary>
        /// 插入对象到空间索引
        /// </summary>
        public void Insert(CreepData data, Vector3 position, Vector3 size)
        {
            var cellKey = GetCellKey(position);
            
            if (!_spatialGrid.ContainsKey(cellKey))
            {
                _spatialGrid[cellKey] = new List<CreepData>();
            }
            
            _spatialGrid[cellKey].Add(data);
        }
        
        /// <summary>
        /// 从空间索引中移除对象
        /// </summary>
        public void Remove(CreepData data, Vector3 position, Vector3 size)
        {
            var cellKey = GetCellKey(position);
            
            if (_spatialGrid.TryGetValue(cellKey, out var list))
            {
                list.Remove(data);
                
                if (list.Count == 0)
                {
                    _spatialGrid.Remove(cellKey);
                }
            }
        }
        
        /// <summary>
        /// 查询指定区域内的对象
        /// </summary>
        public List<CreepData> Query(Vector3 center, float radius)
        {
            var result = new List<CreepData>();
            var cellKey = GetCellKey(center);
            
            if (_spatialGrid.TryGetValue(cellKey, out var list))
            {
                foreach (var data in list)
                {
                    if (Vector3.Distance(data.Position, center) <= radius)
                    {
                        result.Add(data);
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 获取网格键
        /// </summary>
        private Vector3 GetCellKey(Vector3 position)
        {
            return new Vector3(
                Mathf.Floor(position.x / _cellSize) * _cellSize,
                0f,
                Mathf.Floor(position.z / _cellSize) * _cellSize
            );
        }
    }
}