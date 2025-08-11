using UnityEngine;

namespace DeepAbyssHive.Creep.Data
{
    /// <summary>
    /// 菌毯数据结构
    /// </summary>
    public class CreepData
    {
        /// <summary>
        /// 菌毯ID
        /// </summary>
        public int CreepId;
        
        /// <summary>
        /// 所有者ID
        /// </summary>
        public int OwnerId;
        
        /// <summary>
        /// 位置
        /// </summary>
        public Vector3 Position;
        
        /// <summary>
        /// 源点位置
        /// </summary>
        public Vector3 SourcePosition;
        
        /// <summary>
        /// 菌毯密度（0-1）
        /// </summary>
        public float Density;
        
        /// <summary>
        /// 是否为源点
        /// </summary>
        public bool IsSource;
        
        /// <summary>
        /// 源点半径
        /// </summary>
        public float SourceRadius;
        
        /// <summary>
        /// 最后更新时间
        /// </summary>
        public float LastUpdateTime;
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public float CreationTime;
        
        /// <summary>
        /// 当前扩张半径
        /// </summary>
        public float CurrentRadius;
        
        /// <summary>
        /// 最大扩张半径
        /// </summary>
        public float MaxRadius;
        
        /// <summary>
        /// 扩张速度
        /// </summary>
        public float ExpansionSpeed;
        
        /// <summary>
        /// 菌毯强度（0-1）
        /// </summary>
        public float Strength;
        
        /// <summary>
        /// 菌毯健康度（0-1）
        /// </summary>
        public float Health;
        
        /// <summary>
        /// 是否连接到主菌毯网络
        /// </summary>
        public bool IsConnectedToMainNetwork;
        
        /// <summary>
        /// 源建筑ID（如果有）
        /// </summary>
        public int SourceBuildingId;
        
        /// <summary>
        /// 菌毯网络ID
        /// </summary>
        public int NetworkId;
    }
    
    /// <summary>
    /// 菌毯网络数据结构
    /// </summary>
    public class CreepNetworkData
    {
        /// <summary>
        /// 网络ID
        /// </summary>
        public int NetworkId;
        
        /// <summary>
        /// 所有者ID
        /// </summary>
        public int OwnerId;
        
        /// <summary>
        /// 网络中的菌毯节点ID列表
        /// </summary>
        public int[] CreepNodeIds;
        
        /// <summary>
        /// 网络中的建筑ID列表
        /// </summary>
        public int[] BuildingIds;
        
        /// <summary>
        /// 网络总面积
        /// </summary>
        public float TotalArea;
        
        /// <summary>
        /// 网络平均强度
        /// </summary>
        public float AverageStrength;
        
        /// <summary>
        /// 网络平均健康度
        /// </summary>
        public float AverageHealth;
        
        /// <summary>
        /// 连接的源点列表
        /// </summary>
        public System.Collections.Generic.List<Vector3> ConnectedSources;
        
        /// <summary>
        /// 网络效率
        /// </summary>
        public float NetworkEfficiency;
    }
}