using UnityEngine;

namespace DeepAbyssHive.Units.Data
{
    /// <summary>
    /// 单位数据结构（值类型，用于NativeArray）
    /// </summary>
    [System.Serializable]
    public partial struct UnitData
    {
        public int UnitId;
        public Vector3 Position;
        public Quaternion Rotation;
        public float Health;
        public float MaxHealth;
        public float Speed;
        public float AttackDamage;
        public float AttackRange;
        public int UnitType;
        public int Level;
        public bool IsAlive;
        public float LastUpdateTime;
        
        // 相容欄位（供舊程式碼引用）
        public float MaxEnergy;
        public object EvolutionOptions;
        
        // 注意：以下屬性已移至其他資料結構：
        // - AttackSound, DeathSound: 請使用 UnitTemplate
        // - UnitName: 請使用 UnitColdData.UnitName 或 UnitTemplate.UnitName
        // - MoveSpeed: 請使用 UnitHotData.MoveSpeed 或 UnitAttributes.MoveSpeed
        // - AttackCooldown: 請使用 UnitHotData.AttackCooldown
        // - DetectionRange: 請使用 UnitTemplate.DetectionRange
    }
}
