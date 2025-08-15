using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Units.Enums;
using DeepAbyssHive.Units.Data;
using DeepAbyssHive.SpatialIndex.Data;

namespace DeepAbyssHive.Units.Managers
{
    /// <summary>
    /// 单位管理器查询部分 - GetUnitsInRange、GetUnitsOfType等查询方法
    /// </summary>
    public partial class UnitManager
    {
        /// <summary>
        /// 获取单位热数据
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <returns>单位热数据</returns>
        public UnitHotData GetUnitHotData(int unitId)
        {
            if (_unitHotData.TryGetValue(unitId, out UnitHotData hotData))
            {
                return hotData;
            }
            
            Debug.LogWarning($"[{_managerName}] 尝试获取不存在的单位热数据: {unitId}");
            return new UnitHotData();
        }

        /// <summary>
        /// 获取单位冷数据
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <returns>单位冷数据</returns>
        public UnitColdData? GetUnitColdData(int unitId)
        {
            if (_unitColdData.TryGetValue(unitId, out UnitColdData coldData))
            {
                return coldData;
            }
            
            Debug.LogWarning($"[{_managerName}] 尝试获取不存在的单位冷数据: {unitId}");
            return null;
        }

        /// <summary>
        /// 获取范围内的单位
        /// </summary>
        /// <param name="position">中心位置</param>
        /// <param name="radius">半径</param>
        /// <returns>单位ID数组</returns>
        public NativeArray<int> GetUnitsInRange(Vector3 position, float radius)
        {
            if (_spatialIndex != null)
            {
                // 使用空间索引查询
                List<SpatialNode> spatialResults = _spatialIndex.QueryRange(position, Vector3.one * radius * 2);
                
                // 过滤距离并转换为单位ID
                List<int> unitsInRange = new List<int>();
                foreach (var spatialNode in spatialResults)
                {
                    if (Vector3.Distance(spatialNode.Position, position) <= radius)
                    {
                        unitsInRange.Add(spatialNode.Id);
                    }
                }
                
                // 转换为NativeArray
                NativeArray<int> result = new NativeArray<int>(unitsInRange.Count, Allocator.Temp);
                for (int i = 0; i < unitsInRange.Count; i++)
                {
                    result[i] = unitsInRange[i];
                }
                
                return result;
            }
            
            // 如果没有空间索引，使用暴力搜索
            List<int> unitsInRangeFallback = new List<int>();
            foreach (var pair in _unitHotData)
            {
                int unitId = pair.Key;
                UnitHotData hotData = pair.Value;
                
                if (Vector3.Distance(hotData.Position, position) <= radius)
                {
                    unitsInRangeFallback.Add(unitId);
                }
            }
            
            // 转换为NativeArray
            NativeArray<int> resultFallback = new NativeArray<int>(unitsInRangeFallback.Count, Allocator.Temp);
            for (int i = 0; i < unitsInRangeFallback.Count; i++)
            {
                resultFallback[i] = unitsInRangeFallback[i];
            }
            
            return resultFallback;
        }

        /// <summary>
        /// 获取指定类型和所有者的单位
        /// </summary>
        /// <param name="type">单位类型</param>
        /// <param name="ownerId">所有者ID</param>
        /// <returns>单位ID数组</returns>
        public NativeArray<int> GetUnitsOfType(UnitType type, int ownerId)
        {
            List<int> units = new List<int>();
            
            foreach (var pair in _unitColdData)
            {
                int unitId = pair.Key;
                UnitColdData coldData = pair.Value;
                
                if (coldData.Type == type && coldData.OwnerId == ownerId)
                {
                    units.Add(unitId);
                }
            }
            
            // 转换为NativeArray
            NativeArray<int> result = new NativeArray<int>(units.Count, Allocator.Temp);
            for (int i = 0; i < units.Count; i++)
            {
                result[i] = units[i];
            }
            
            return result;
        }

        /// <summary>
        /// 获取单位类型的基础属性
        /// </summary>
        /// <param name="type">单位类型</param>
        /// <returns>单位属性</returns>
        private UnitAttributes GetBaseAttributesForType(UnitType type)
        {
            // 在实际实现中，应该从配置文件或数据库中加载单位属性
            // 这里简化处理，直接返回硬编码的属性
            
            UnitAttributes attributes = new UnitAttributes();
            
            switch (type)
            {
                case UnitType.Worker:
                    attributes.MaxHealth = 50f;
                    attributes.MoveSpeed = 3f;
                    attributes.AttackDamage = 5f;
                    attributes.AttackSpeed = 1f;
                    attributes.AttackRange = 1f;
                    attributes.SightRange = 10f;
                    attributes.ResourceGatherRate = 1f;
                    attributes.BuildSpeed = 1f;
                    break;
                    
                case UnitType.Warrior:
                    attributes.MaxHealth = 100f;
                    attributes.MoveSpeed = 3.5f;
                    attributes.AttackDamage = 15f;
                    attributes.AttackSpeed = 1.2f;
                    attributes.AttackRange = 1.5f;
                    attributes.SightRange = 12f;
                    attributes.ResourceGatherRate = 0f;
                    attributes.BuildSpeed = 0f;
                    break;
                    
                case UnitType.AcidSprayer:
                    attributes.MaxHealth = 60f;
                    attributes.MoveSpeed = 3f;
                    attributes.AttackDamage = 12f;
                    attributes.AttackSpeed = 1f;
                    attributes.AttackRange = 8f;
                    attributes.SightRange = 15f;
                    attributes.ResourceGatherRate = 0f;
                    attributes.BuildSpeed = 0f;
                    break;
                    
                case UnitType.Tank:
                    attributes.MaxHealth = 200f;
                    attributes.MoveSpeed = 2f;
                    attributes.AttackDamage = 20f;
                    attributes.AttackSpeed = 0.8f;
                    attributes.AttackRange = 2f;
                    attributes.SightRange = 10f;
                    attributes.ResourceGatherRate = 0f;
                    attributes.BuildSpeed = 0f;
                    break;
                    
                case UnitType.Scout:
                    attributes.MaxHealth = 40f;
                    attributes.MoveSpeed = 5f;
                    attributes.AttackDamage = 8f;
                    attributes.AttackSpeed = 1.5f;
                    attributes.AttackRange = 1f;
                    attributes.SightRange = 20f;
                    attributes.ResourceGatherRate = 0f;
                    attributes.BuildSpeed = 0f;
                    break;
                    
                case UnitType.Flyer:
                    attributes.MaxHealth = 70f;
                    attributes.MoveSpeed = 4f;
                    attributes.AttackDamage = 10f;
                    attributes.AttackSpeed = 1.2f;
                    attributes.AttackRange = 1.5f;
                    attributes.SightRange = 18f;
                    attributes.ResourceGatherRate = 0f;
                    attributes.BuildSpeed = 0f;
                    break;
                    
                case UnitType.Queen:
                    attributes.MaxHealth = 500f;
                    attributes.MoveSpeed = 2.5f;
                    attributes.AttackDamage = 30f;
                    attributes.AttackSpeed = 1f;
                    attributes.AttackRange = 3f;
                    attributes.SightRange = 15f;
                    attributes.ResourceGatherRate = 0f;
                    attributes.BuildSpeed = 2f;
                    break;
            }
            
            return attributes;
        }

        /// <summary>
        /// 获取单位类型的预制体路径
        /// </summary>
        /// <param name="type">单位类型</param>
        /// <returns>预制体路径</returns>
        private string GetPrefabPathForType(UnitType type)
        {
            if (_unitPrefabPaths.TryGetValue(type, out string path))
            {
                return path;
            }
            
            // 默认路径
            return $"Prefabs/Units/{type}";
        }
    }
}