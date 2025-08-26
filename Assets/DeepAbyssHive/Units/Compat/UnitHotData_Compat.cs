using System.Collections.Generic;
using DeepAbyssHive.Units.Enums;
using UnityEngine;

namespace DeepAbyssHive.Units.Data
{
    public partial struct UnitHotData
    {
        public int Id { get => _compatId; set => _compatId = value; } // 若已有 UnitId，改為直通
        public UnitState State { get => _compatState; set => _compatState = value; }
        public float StateTimer { get => _compatStateTimer; set => _compatStateTimer = value; }
        public int TargetId { get => _compatTargetId; set => _compatTargetId = value; }

        // 舊 ActionTimer → 先提供後備欄位
        public float ActionTimer { get => _compatActionTimer; set => _compatActionTimer = value; }

        // 舊 MovementPath → 先提供後備欄位（若現用有 Path/Waypoints，可改為直通）
        public List<Vector3> MovementPath { get => _compatMovementPath ??= new List<Vector3>(); set => _compatMovementPath = value; }

        private int _compatId = 0;
        private UnitState _compatState = UnitState.Idle;
        private float _compatStateTimer = 0f;
        private int _compatTargetId = -1;
        private float _compatActionTimer = 0f;
        private List<Vector3> _compatMovementPath = null;
    }
}