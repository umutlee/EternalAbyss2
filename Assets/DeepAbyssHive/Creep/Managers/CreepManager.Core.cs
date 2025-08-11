using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Creep.Data;

namespace DeepAbyssHive.Creep.Managers
{
    /// <summary>
    /// CreepManager 核心方法
    /// 包含坐标转换、基础操作等核心功能
    /// </summary>
    public partial class CreepManager
    {
        /// <summary>
        /// 世界坐标转网格坐标
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>网格坐标</returns>
        private Vector2Int WorldToGridPosition(Vector3 worldPosition)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPosition.x / _gridSize),
                Mathf.FloorToInt(worldPosition.z / _gridSize)
            );
        }

        /// <summary>
        /// 网格坐标转世界坐标
        /// </summary>
        /// <param name="gridPosition">网格坐标</param>
        /// <returns>世界坐标</returns>
        private Vector3 GridToWorldPosition(Vector2Int gridPosition)
        {
            return new Vector3(
                gridPosition.x * _gridSize + _gridSize * 0.5f,
                0f,
                gridPosition.y * _gridSize + _gridSize * 0.5f
            );
        }

        /// <summary>
        /// 在指定位置扩张菌毯
        /// </summary>
        /// <param name="gridPos">网格位置</param>
        /// <param name="worldPos">世界位置</param>
        /// <param name="expansionStrength">扩张强度</param>
        /// <param name="ownerId">所有者ID</param>
        private void ExpandCreepAtPosition(Vector2Int gridPos, Vector3 worldPos, float expansionStrength, int ownerId)
        {
            if (_creepGrid.TryGetValue(gridPos, out CreepData existingCreep))
            {
                // 如果是同一所有者，增强密度
                if (existingCreep.OwnerId == ownerId)
                {
                    existingCreep.Density = Mathf.Min(_maxDensity, existingCreep.Density + expansionStrength);
                    existingCreep.LastUpdateTime = Time.time;
                    _creepGrid[gridPos] = existingCreep;
                }
                // 如果是敌方菌毯，进行竞争
                else
                {
                    float competitionResult = expansionStrength - existingCreep.Density * 0.5f;
                    if (competitionResult > 0)
                    {
                        // 覆盖敌方菌毯
                        existingCreep.OwnerId = ownerId;
                        existingCreep.Density = competitionResult;
                        existingCreep.LastUpdateTime = Time.time;
                        _creepGrid[gridPos] = existingCreep;
                    }
                }
            }
            else
            {
                // 创建新的菌毯
                CreepData newCreep = new CreepData
                {
                    Position = worldPos,
                    Density = expansionStrength,
                    OwnerId = ownerId,
                    IsSource = false,
                    SourceRadius = 0f,
                    LastUpdateTime = Time.time,
                    CreationTime = Time.time
                };
                
                _creepGrid[gridPos] = newCreep;
                _activeCreepCells.Add(gridPos);
                
                // 添加到空间索引
                if (_spatialIndex != null)
                {
                    _spatialIndex.Insert(newCreep, worldPos, Vector3.one * _gridSize);
                }
            }
        }

        /// <summary>
        /// 移除指定位置的菌毯
        /// </summary>
        /// <param name="gridPos">网格位置</param>
        private void RemoveCreepAtPosition(Vector2Int gridPos)
        {
            if (_creepGrid.TryGetValue(gridPos, out CreepData creepData))
            {
                // 从空间索引中移除
                if (_spatialIndex != null)
                {
                    _spatialIndex.Remove(creepData, creepData.Position, Vector3.one * _gridSize);
                }
                
                // 从网格中移除
                _creepGrid.Remove(gridPos);
                _activeCreepCells.Remove(gridPos);
            }
        }
    }
}