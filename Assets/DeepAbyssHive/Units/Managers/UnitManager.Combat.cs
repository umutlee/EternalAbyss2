using UnityEngine;
using DeepAbyssHive.Units.Enums;
using DeepAbyssHive.Units.Data;

namespace DeepAbyssHive.Units.Managers
{
    /// <summary>
    /// 单位管理器战斗部分 - AttackTarget和攻击逻辑
    /// </summary>
    public partial class UnitManager
    {
        /// <summary>
        /// 攻击目标
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="targetId">目标ID</param>
        public void AttackTarget(int unitId, int targetId)
        {
            if (!_unitHotData.TryGetValue(unitId, out UnitHotData hotData))
            {
                Debug.LogWarning($"[{_managerName}] 尝试攻击不存在的单位: {unitId}");
                return;
            }
            
            if (!_unitColdData.TryGetValue(unitId, out UnitColdData coldData))
            {
                return;
            }
            
            if (!_unitHotData.TryGetValue(targetId, out UnitHotData targetHotData))
            {
                Debug.LogWarning($"[{_managerName}] 尝试攻击不存在的目标: {targetId}");
                return;
            }
            
            // 更新单位状态
            hotData.State = UnitState.Attacking;
            hotData.TargetId = targetId;
            hotData.StateTimer = 0f;
            
            // 计算方向
            Vector3 direction = (targetHotData.Position - hotData.Position).normalized;
            
            // 更新旋转
            if (direction != Vector3.zero)
            {
                hotData.Rotation = Quaternion.LookRotation(direction);
            }
            
            // 停止移动
            hotData.Velocity = Vector3.zero;
            
            // 更新数据
            _unitHotData[unitId] = hotData;
            
            // 更新游戏对象
            UpdateUnitGameObject(unitId, hotData);
            
            Debug.Log($"[{_managerName}] 攻击目标: ID={unitId}, 目标ID={targetId}");
        }

        /// <summary>
        /// 执行攻击
        /// </summary>
        /// <param name="attackerId">攻击者ID</param>
        /// <param name="targetId">目标ID</param>
        private void PerformAttack(int attackerId, int targetId)
        {
            if (!_unitColdData.TryGetValue(attackerId, out UnitColdData attackerData))
                return;
                
            if (!_unitHotData.TryGetValue(targetId, out UnitHotData targetHotData))
                return;
                
            // 计算伤害
            float damage = attackerData.BaseAttributes.AttackDamage;
            
            // 应用伤害
            targetHotData.Health -= damage;
            
            // 检查目标是否死亡
            if (targetHotData.Health <= 0)
            {
                targetHotData.Health = 0;
                targetHotData.State = UnitState.Dead;
                
                // 更新目标热数据
                _unitHotData[targetId] = targetHotData;
                
                // 更新目标游戏对象
                UpdateUnitGameObject(targetId, targetHotData);
                
                // 延迟销毁目标
                // 在实际实现中，可能需要使用协程或定时器
                // 这里简化处理，直接销毁
                DestroyUnit(targetId);
            }
            else
            {
                // 更新目标热数据
                _unitHotData[targetId] = targetHotData;
                
                // 更新目标游戏对象
                UpdateUnitGameObject(targetId, targetHotData);
            }
            
            Debug.Log($"[{_managerName}] 单位攻击: 攻击者={attackerId}, 目标={targetId}, 伤害={damage}, 目标剩余生命={targetHotData.Health}");
        }

        /// <summary>
        /// 更新攻击状态
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="hotData">热数据</param>
        /// <param name="coldData">冷数据</param>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateAttackingState(int unitId, ref UnitHotData hotData, UnitColdData coldData, float deltaTime)
        {
            // 检查目标是否存在
            if (!_unitHotData.TryGetValue(hotData.TargetId, out UnitHotData targetHotData))
            {
                // 目标不存在，回到空闲状态
                hotData.State = UnitState.Idle;
                hotData.TargetId = -1;
                return;
            }
            
            // 使用移动部分的攻击移动逻辑
            UpdateAttackingMovement(unitId, ref hotData, coldData, targetHotData, deltaTime);
            
            // 检查攻击冷却
            if (hotData.StateTimer <= 0 && hotData.Velocity == Vector3.zero)
            {
                // 执行攻击
                PerformAttack(unitId, hotData.TargetId);
                
                // 重置攻击冷却
                hotData.StateTimer = 1f / coldData.BaseAttributes.AttackSpeed;
            }
        }
    }
}
