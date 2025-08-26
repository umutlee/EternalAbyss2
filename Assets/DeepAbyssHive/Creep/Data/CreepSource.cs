using UnityEngine;
using DeepAbyssHive.Creep.Enums;

namespace DeepAbyssHive.Creep.Data
{
    /// <summary>
    /// 菌毯源点
    /// </summary>
    [System.Serializable]
    public partial struct CreepSource
    {
        public int SourceId;
        public Vector3 Position;
        public float Radius;
        public float Strength;
        public CreepSourceType Type;
        public bool IsActive;
        public float CreationTime;
        public int NetworkId;
    }
}