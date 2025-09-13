using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Units.Enums;
using DeepAbyssHive.Units.Data;
using DeepAbyssHive.SpatialIndex.Data;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Units.Managers
{
    /// <summary>
    /// 单位管理器移动部分 - MoveUnit和移动状态更新
    /// </summary>
    public partial class UnitManager
    {
        /// <summary>
        /// 移动单位
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="targetPosition">目标位置</param>
        public void MoveUnit(int unitId, Vector3 targetPosition)
        {
            if (!_unitHotData.TryGetValue(unitId, out UnitHotData hotData))
            {
                DAHLog.Warning(LogCategory.UNITS, $"[{_managerName}] 尝试移动不存在的单位: {unitId}");
                return;
            }
            
            if (!_unitColdData.TryGetValue(unitId, out UnitColdData coldData))
            {
                return;
            }
            
            // 更新单位状态
            hotData.CurrentState = UnitState.Moving;
            hotData.TargetUnitId = -1;
            hotData.ActionTimer = 0f;
            
            // 计算移动方向
            Vector3 direction = (targetPosition - hotData.Position).normalized;
            
            // 设置速度
            hotData.Velocity = direction * coldData.BaseAttributes.MoveSpeed;
            
            // 更新旋转
            if (direction != Vector3.zero)
            {
                hotData.Rotation = Quaternion.LookRotation(direction);
            }
            
            // 更新数据
            _unitHotData[unitId] = hotData;
            
            // 更新游戏对象
            UpdateUnitGameObject(unitId, hotData);
            
            DAHLog.Info(LogCategory.UNITS, $"[{_managerName}] 移动单位: ID={unitId}, 目标位置={targetPosition}");
        }

        /// <summary>
        /// 更新移动状态的单位
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="hotData">热数据</param>
        /// <param name="coldData">冷数据</param>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateMovingState(int unitId, ref UnitHotData hotData, UnitColdData coldData, float deltaTime)
        {
            // 更新位置
            hotData.Position += hotData.Velocity * deltaTime;
            
            // 更新空间索引
            if (_spatialIndex != null && _unitSpatialNodes.TryGetValue(unitId, out SpatialNode spatialNode))
            {
                Vector3 oldPosition = hotData.Position - hotData.Velocity * deltaTime;
                _spatialIndex.Update(spatialNode, oldPosition, hotData.Position, new Vector3(coldData.BaseAttributes.SightRange, coldData.BaseAttributes.SightRange, coldData.BaseAttributes.SightRange));
            }
        }

        /// <summary>
        /// 更新攻击状态中的移动逻辑
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="hotData">热数据</param>
        /// <param name="coldData">冷数据</param>
        /// <param name="targetHotData">目标热数据</param>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateAttackingMovement(int unitId, ref UnitHotData hotData, UnitColdData coldData, UnitHotData targetHotData, float deltaTime)
        {
            // 检查是否在攻击范围内
            float distanceToTarget = Vector3.Distance(hotData.Position, targetHotData.Position);
            if (distanceToTarget > coldData.BaseAttributes.AttackRange)
            {
                // 目标不在攻击范围内，移动向目标
                Vector3 direction = (targetHotData.Position - hotData.Position).normalized;
                hotData.Velocity = direction * coldData.BaseAttributes.MoveSpeed;
                hotData.Position += hotData.Velocity * deltaTime;
                
                // 更新旋转
                if (direction != Vector3.zero)
                {
                    hotData.Rotation = Quaternion.LookRotation(direction);
                }
                
                // 更新空间索引
                if (_spatialIndex != null && _unitSpatialNodes.TryGetValue(unitId, out SpatialNode spatialNode))
                {
                    Vector3 oldPosition = hotData.Position - hotData.Velocity * deltaTime;
                    _spatialIndex.Update(spatialNode, oldPosition, hotData.Position, new Vector3(coldData.BaseAttributes.SightRange, coldData.BaseAttributes.SightRange, coldData.BaseAttributes.SightRange));
                }
            }
            else
            {
                // 目标在攻击范围内，停止移动
                hotData.Velocity = Vector3.zero;
                
                // 更新旋转，面向目标
                Vector3 direction = (targetHotData.Position - hotData.Position).normalized;
                if (direction != Vector3.zero)
                {
                    hotData.Rotation = Quaternion.LookRotation(direction);
                }
            }
        }

        /// <summary>
        /// 更新资源收集状态
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="hotData">热数据</param>
        /// <param name="coldData">冷数据</param>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateGatheringState(int unitId, UnitHotData hotData, UnitColdData coldData, float deltaTime)
        {
            // 检查是否有目标资源点
            if (hotData.TargetUnitId == -1)
            {
                // 没有目标，回到空闲状态
                hotData.CurrentState = UnitState.Idle;
                return;
            }
            
            // 简化的资源收集逻辑
            if (hotData.ActionTimer <= 0)
            {
                // 收集资源
                float gatherAmount = coldData.BaseAttributes.ResourceGatherRate * deltaTime;
                
                // 重置收集计时器
                hotData.ActionTimer = 1f / coldData.BaseAttributes.ResourceGatherRate;
                
                DAHLog.Info(LogCategory.UNITS, $"[{_managerName}] 单位收集资源: ID={unitId}, 数量={gatherAmount}");
            }
        }

        /// <summary>
        /// 更新建造状态
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="hotData">热数据</param>
        /// <param name="coldData">冷数据</param>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateBuildingState(int unitId, UnitHotData hotData, UnitColdData coldData, float deltaTime)
        {
            // 检查是否有目标建筑位置
            if (hotData.TargetUnitId == -1)
            {
                // 没有目标，回到空闲状态
                hotData.CurrentState = UnitState.Idle;
                return;
            }
            
            // 简化的建造逻辑
            if (hotData.ActionTimer <= 0)
            {
                // 建造进度
                float buildProgress = coldData.BaseAttributes.BuildSpeed * deltaTime;
                
                // 重置建造计时器
                hotData.ActionTimer = 1f / coldData.BaseAttributes.BuildSpeed;
                
                DAHLog.Info(LogCategory.UNITS, $"[{_managerName}] 单位建造进度: ID={unitId}, 进度={buildProgress}");
            }
        }
    }
}