using UnityEngine;
using DeepAbyssHive.Buildings.Enums;

namespace DeepAbyssHive.Buildings.Data
{
    /// <summary>
    /// 建筑数据结构
    /// </summary>
    public class BuildingData
    {
        /// <summary>
        /// 建筑ID
        /// </summary>
        public int BuildingId;
        
        /// <summary>
        /// 建筑类型
        /// </summary>
        public BuildingType BuildingType;
        
        /// <summary>
        /// 建筑类型（兼容性别名）
        /// </summary>
        public BuildingType Type
        {
          get => BuildingType;
          set => BuildingType = value;
        }
        
        /// <summary>
        /// 所有者ID
        /// </summary>
        public int OwnerId;
        
        /// <summary>
        /// 位置
        /// </summary>
        public Vector3 Position;
        
        /// <summary>
        /// 旋转
        /// </summary>
        public Quaternion Rotation;
        
        /// <summary>
        /// 大小
        /// </summary>
        public Vector2Int Size;
        
        /// <summary>
        /// 当前状态
        /// </summary>
        public BuildingState State;
        
        /// <summary>
        /// 当前生命值
        /// </summary>
        public float Health;
        
        /// <summary>
        /// 最大生命值
        /// </summary>
        public float MaxHealth;
        
        /// <summary>
        /// 建造/升级进度（0-1）
        /// </summary>
        public float Progress;
        
        /// <summary>
        /// 建造进度（0-1）
        /// </summary>
        public float ConstructionProgress;
        
        /// <summary>
        /// 建造时间
        /// </summary>
        public float ConstructionTime;
        
        /// <summary>
        /// 当前等级
        /// </summary>
        public int Level;
        
        /// <summary>
        /// 经验值
        /// </summary>
        public float Experience;
        
        /// <summary>
        /// 生物能消耗
        /// </summary>
        public float BioEnergyConsumption;
        
        /// <summary>
        /// 生物能产出
        /// </summary>
        public float BioEnergyGeneration;
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public float CreationTime;
        
        /// <summary>
        /// 最后更新时间
        /// </summary>
        public float LastUpdateTime;
        
        /// <summary>
        /// 预制体路径
        /// </summary>
        public string PrefabPath;
        
        /// <summary>
        /// 升级路径ID
        /// </summary>
        public string UpgradePath;
        
        /// <summary>
        /// 功能数据
        /// </summary>
        public BuildingFunctionData FunctionData;
    }
    
    /// <summary>
    /// 建筑功能数据结构
    /// </summary>
    public class BuildingFunctionData
    {
        /// <summary>
        /// 生产队列
        /// </summary>
        public ProductionQueueItem[] ProductionQueue;
        
        /// <summary>
        /// 研究项目
        /// </summary>
        public ResearchItem CurrentResearch;
        
        /// <summary>
        /// 资源存储
        /// </summary>
        public ResourceStorage Resources;
        
        /// <summary>
        /// 特殊能力
        /// </summary>
        public string[] SpecialAbilities;
        
        /// <summary>
        /// 菌毯扩张范围
        /// </summary>
        public float CreepExpansionRadius;
    }
    
    /// <summary>
    /// 生产队列项结构
    /// </summary>
    public struct ProductionQueueItem
    {
        /// <summary>
        /// 生产类型
        /// </summary>
        public enum ProductionType
        {
            /// <summary>
            /// 单位生产
            /// </summary>
            Unit,
            
            /// <summary>
            /// 建筑生产
            /// </summary>
            Building,
            
            /// <summary>
            /// 升级生产
            /// </summary>
            Upgrade
        }
        
        /// <summary>
        /// 生产类型
        /// </summary>
        public ProductionType Type;
        
        /// <summary>
        /// 生产ID（单位类型、建筑类型或升级ID）
        /// </summary>
        public string ProductionId;
        
        /// <summary>
        /// 生产进度（0-1）
        /// </summary>
        public float Progress;
        
        /// <summary>
        /// 总生产时间
        /// </summary>
        public float TotalTime;
    }
    
    /// <summary>
    /// 研究项目结构
    /// </summary>
    public struct ResearchItem
    {
        /// <summary>
        /// 研究ID
        /// </summary>
        public string ResearchId;
        
        /// <summary>
        /// 研究进度（0-1）
        /// </summary>
        public float Progress;
        
        /// <summary>
        /// 总研究时间
        /// </summary>
        public float TotalTime;
    }
    
    /// <summary>
    /// 资源存储结构
    /// </summary>
    public struct ResourceStorage
    {
        /// <summary>
        /// 生物质
        /// </summary>
        public float Biomass;
        
        /// <summary>
        /// 矿物质
        /// </summary>
        public float Minerals;
        
        /// <summary>
        /// 基因点
        /// </summary>
        public float GenePoints;
        
        /// <summary>
        /// 最大存储量
        /// </summary>
        public float MaxStorage;
    }
}