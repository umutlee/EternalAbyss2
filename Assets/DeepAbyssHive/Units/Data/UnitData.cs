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
        // 以下欄位已移至 UnitData_Compat.cs 作為屬性實現
        // public string AttackSound;
        // public string DeathSound;
        // public string UnitName;
        // public float MoveSpeed;
        // public float AttackCooldown;
        // public float DetectionRange;
    }
}
