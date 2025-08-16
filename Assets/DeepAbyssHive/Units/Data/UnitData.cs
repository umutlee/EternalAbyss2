using UnityEngine;

namespace DeepAbyssHive.Units.Data
{
    /// <summary>
    /// 单位数据结构（值类型，用于NativeArray）
    /// </summary>
    [System.Serializable]
    public struct UnitData
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
    }
}