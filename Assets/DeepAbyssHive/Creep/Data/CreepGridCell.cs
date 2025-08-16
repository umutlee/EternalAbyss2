using UnityEngine;
using DeepAbyssHive.Creep.Enums;

namespace DeepAbyssHive.Creep.Data
{
    /// <summary>
    /// 菌毯网格单元
    /// </summary>
    [System.Serializable]
    public struct CreepGridCell
    {
        public Vector2Int GridCoord;
        public Vector3 WorldPosition;
        public float Density;
        public CreepType Type;
        public CreepNetworkState NetworkState;
        public int NetworkId;
        public float LastUpdateTime;
        public bool IsActive;
    }
}