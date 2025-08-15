using UnityEngine;
using DeepAbyssHive.Units.Enums;
using DeepAbyssHive.Units.Data;
using DeepAbyssHive.SpatialIndex.Data;

namespace DeepAbyssHive.Units.Managers
{
    /// <summary>
    /// 单位管理器视图部分 - 游戏对象实例化、更新和外观管理
    /// </summary>
    public partial class UnitManager
    {
        /// <summary>
        /// 销毁单位视图 - 处理游戏对象和视觉效果的销毁
        /// </summary>
        /// <param name="unitId">单位ID</param>
        public void DestroyUnitView(int unitId)
        {
            // 销毁游戏对象
            if (_unitGameObjects.TryGetValue(unitId, out GameObject unitObject) && unitObject != null)
            {
                GameObject.Destroy(unitObject);
                _unitGameObjects.Remove(unitId);
            }
            
            Debug.Log($"[{_managerName}] 销毁单位视图: ID={unitId}");
        }

        /// <summary>
        /// 创建单位
        /// </summary>
        /// <param name="type">单位类型</param>
        /// <param name="position">位置</param>
        /// <param name="ownerId">所有者ID</param>
        /// <returns>单位ID</returns>
        public int CreateUnit(UnitType type, Vector3 position, int ownerId)
        {
            int unitId = _nextUnitId++;
            
            // 创建单位冷数据
            UnitColdData coldData = new UnitColdData
            {
                UnitId = unitId,
                Type = type,
                OwnerId = ownerId,
                BaseAttributes = GetBaseAttributesForType(type),
                Evolution = new EvolutionInfo
                {
                    Level = 0,
                    PathId = "",
                    UnlockedAbilities = new string[0]
                },
                AdaptiveTraits = new AdaptiveTrait[0],
                PrefabPath = GetPrefabPathForType(type)
            };
            
            // 创建单位热数据
            UnitHotData hotData = new UnitHotData
            {
                Position = position,
                Rotation = Quaternion.identity,
                Velocity = Vector3.zero,
                Health = coldData.BaseAttributes.MaxHealth,
                TargetId = -1,
                State = UnitState.Idle,
                StateTimer = 0f
            };
            
            // 存储数据
            _unitColdData[unitId] = coldData;
            _unitHotData[unitId] = hotData;
            
            // 实例化单位游戏对象
            GameObject unitObject = InstantiateUnitObject(coldData, hotData);
            if (unitObject != null)
            {
                _unitGameObjects[unitId] = unitObject;
            }
            
            // 创建空间节点并添加到空间索引
            if (_spatialIndex != null)
            {
                SpatialNode spatialNode = new SpatialNode(
                    unitId, 
                    unitObject, 
                    position, 
                    new Bounds(position, Vector3.one * coldData.BaseAttributes.SightRange),
                    "Unit",
                    0,
                    false
                );
                _unitSpatialNodes[unitId] = spatialNode;
                _spatialIndex.Insert(spatialNode, position, Vector3.one * coldData.BaseAttributes.SightRange);
            }
            
            Debug.Log($"[{_managerName}] 创建单位: ID={unitId}, 类型={type}, 所有者={ownerId}, 位置={position}");
            
            return unitId;
        }


        /// <summary>
        /// 实例化单位游戏对象
        /// </summary>
        /// <param name="coldData">单位冷数据</param>
        /// <param name="hotData">单位热数据</param>
        /// <returns>游戏对象</returns>
        private GameObject InstantiateUnitObject(UnitColdData coldData, UnitHotData hotData)
        {
            // 加载预制体
            GameObject prefab = Resources.Load<GameObject>(coldData.PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[{_managerName}] 无法加载单位预制体: {coldData.PrefabPath}");
                return null;
            }
            
            // 实例化游戏对象
            GameObject unitObject = GameObject.Instantiate(prefab, hotData.Position, hotData.Rotation);
            
            // 设置名称
            unitObject.name = $"{coldData.Type}_{coldData.UnitId}";
            
            // 设置标签和层
            unitObject.tag = "Unit";
            unitObject.layer = LayerMask.NameToLayer("Units");
            
            // 添加单位组件
            // 在实际实现中，应该添加一个UnitComponent组件来管理单位的游戏对象
            // 这里简化处理，不添加额外组件
            
            Debug.Log($"[{_managerName}] 实例化单位游戏对象: {unitObject.name}");
            
            return unitObject;
        }

        /// <summary>
        /// 更新单位游戏对象
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="hotData">单位热数据</param>
        private void UpdateUnitGameObject(int unitId, UnitHotData hotData)
        {
            if (!_unitGameObjects.TryGetValue(unitId, out GameObject unitObject) || unitObject == null)
                return;
                
            // 更新位置和旋转
            unitObject.transform.position = hotData.Position;
            unitObject.transform.rotation = hotData.Rotation;
            
            // 在实际实现中，应该更新UnitComponent组件的状态
            // 这里简化处理，不更新额外组件
        }

        /// <summary>
        /// 更新单位外观
        /// </summary>
        /// <param name="unitId">单位ID</param>
        private void UpdateUnitAppearance(int unitId)
        {
            if (!_unitGameObjects.TryGetValue(unitId, out GameObject unitObject) || unitObject == null)
                return;
                
            if (!_unitColdData.TryGetValue(unitId, out UnitColdData coldData))
                return;
                
            // 在实际实现中，应该根据进化等级和适应性特征更新单位的外观
            // 这里简化处理，不更新外观
            
            Debug.Log($"[{_managerName}] 更新单位外观: ID={unitId}, 进化等级={coldData.Evolution.Level}, 适应性特征数量={coldData.AdaptiveTraits.Length}");
        }
    }
}