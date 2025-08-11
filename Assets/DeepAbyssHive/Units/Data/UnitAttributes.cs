using UnityEngine;

namespace DeepAbyssHive.Units.Data
{
    /// <summary>
    /// 单位属性数据结构
    /// </summary>
    [System.Serializable]
    public struct UnitAttributes
    {
        /// <summary>
        /// 最大生命值
        /// </summary>
        public float MaxHealth;
        
        /// <summary>
        /// 移动速度
        /// </summary>
        public float MoveSpeed;
        
        /// <summary>
        /// 攻击伤害
        /// </summary>
        public float AttackDamage;
        
        /// <summary>
        /// 攻击速度（每秒攻击次数）
        /// </summary>
        public float AttackSpeed;
        
        /// <summary>
        /// 攻击范围
        /// </summary>
        public float AttackRange;
        
        /// <summary>
        /// 视野范围
        /// </summary>
        public float SightRange;
        
        /// <summary>
        /// 资源收集速率
        /// </summary>
        public float ResourceGatherRate;
        
        /// <summary>
        /// 建造速度
        /// </summary>
        public float BuildSpeed;
        
        /// <summary>
        /// 创建默认属性
        /// </summary>
        /// <returns>默认属性</returns>
        public static UnitAttributes CreateDefault()
        {
            return new UnitAttributes
            {
                MaxHealth = 100f,
                MoveSpeed = 3f,
                AttackDamage = 10f,
                AttackSpeed = 1f,
                AttackRange = 1.5f,
                SightRange = 10f,
                ResourceGatherRate = 1f,
                BuildSpeed = 1f
            };
        }
    }
}