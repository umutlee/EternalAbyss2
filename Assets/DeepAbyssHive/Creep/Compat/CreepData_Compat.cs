using UnityEngine;

namespace DeepAbyssHive.Creep.Data
{
    public partial class CreepData
    {
        // 舊版常用的三個字段：先補後備欄位（若未來有對應，改為直通）
        public int NetworkId { get => _compatNetworkId; set => _compatNetworkId = value; }
        public bool IsActive { get => _compatIsActive; set => _compatIsActive = value; }
        public float Strength { get => _compatStrength; set => _compatStrength = value; }

        // 舊名 Timestamp → 新結構常用 LastUpdateTime（直通到原有屬性）
        public float Timestamp { get => LastUpdateTime; set => LastUpdateTime = value; }

        private int _compatNetworkId;
        private bool _compatIsActive;
        private float _compatStrength;
        
        // LastUpdateTime 屬性（如果原本沒有的話）
        public float LastUpdateTime { get => _compatLastUpdateTime; set => _compatLastUpdateTime = value; }
        private float _compatLastUpdateTime;
    }
}