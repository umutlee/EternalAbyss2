using UnityEngine;
using DeepAbyssHive.Creep.Data;
using DeepAbyssHive.Core.Interfaces;

namespace DeepAbyssHive.Creep.Interfaces
{
    /// <summary>
    /// 菌毯管理器接口
    /// </summary>
    public interface ICreepManager : IManager
    {
        /// <summary>
        /// 创建菌毯节点
        /// </summary>
        /// <param name="creepData">菌毯数据</param>
        /// <returns>菌毯ID</returns>
        int CreateCreepNode(CreepData creepData);
        
        /// <summary>
        /// 获取菌毯数据
        /// </summary>
        /// <param name="creepId">菌毯ID</param>
        /// <returns>菌毯数据</returns>
        CreepData GetCreepData(int creepId);
        
        /// <summary>
        /// 更新菌毯数据
        /// </summary>
        /// <param name="creepData">菌毯数据</param>
        void UpdateCreep(CreepData creepData);
        
        /// <summary>
        /// 删除菌毯节点
        /// </summary>
        /// <param name="creepId">菌毯ID</param>
        void RemoveCreepNode(int creepId);
        
        /// <summary>
        /// 检查位置是否有菌毯覆盖
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="ownerId">所有者ID（可选）</param>
        /// <returns>是否有菌毯覆盖</returns>
        bool HasCreepCoverage(Vector3 position, int ownerId = -1);
        
        /// <summary>
        /// 获取位置处的菌毯强度
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="ownerId">所有者ID（可选）</param>
        /// <returns>菌毯强度（0-1）</returns>
        float GetCreepStrength(Vector3 position, int ownerId = -1);
        
        /// <summary>
        /// 扩张菌毯
        /// </summary>
        /// <param name="creepId">菌毯ID</param>
        /// <param name="expansionAmount">扩张量</param>
        void ExpandCreep(int creepId, float expansionAmount);
        
        /// <summary>
        /// 收缩菌毯
        /// </summary>
        /// <param name="creepId">菌毯ID</param>
        /// <param name="shrinkAmount">收缩量</param>
        void ShrinkCreep(int creepId, float shrinkAmount);
        
        /// <summary>
        /// 损坏菌毯
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="radius">半径</param>
        /// <param name="damageAmount">损坏量</param>
        void DamageCreep(Vector3 position, float radius, float damageAmount);
        
        /// <summary>
        /// 修复菌毯
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="radius">半径</param>
        /// <param name="healAmount">修复量</param>
        /// <param name="ownerId">所有者ID</param>
        void HealCreep(Vector3 position, float radius, float healAmount, int ownerId);
        
        /// <summary>
        /// 获取菌毯网络数据
        /// </summary>
        /// <param name="networkId">网络ID</param>
        /// <returns>菌毯网络数据</returns>
        CreepNetworkData GetCreepNetworkData(int networkId);
        
        /// <summary>
        /// 合并菌毯网络
        /// </summary>
        /// <param name="networkId1">网络ID1</param>
        /// <param name="networkId2">网络ID2</param>
        /// <returns>合并后的网络ID</returns>
        int MergeCreepNetworks(int networkId1, int networkId2);
        
        /// <summary>
        /// 分割菌毯网络
        /// </summary>
        /// <param name="networkId">网络ID</param>
        /// <param name="position">分割位置</param>
        /// <param name="radius">分割半径</param>
        /// <returns>分割后的网络ID数组</returns>
        int[] SplitCreepNetwork(int networkId, Vector3 position, float radius);
    }
}