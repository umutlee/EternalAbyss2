using UnityEngine;

namespace DeepAbyssHive.Creep.Data
{
    public partial struct CreepSource
    {
        // 舊名 PlayerId → 暫無對應，先給後備欄位
        public int PlayerId { get => _compatPlayerId; set => _compatPlayerId = value; }
        private int _compatPlayerId;

        // 舊名 InfluenceRadius → 新結構裡是 Radius
        public float InfluenceRadius { get => Radius; set => Radius = value; }

        // 舊名 SourceType → 新結構裡是 Type
        public int SourceType { get => (int)Type; set => Type = (DeepAbyssHive.Creep.Enums.CreepSourceType)value; }

        // 舊名 LastUpdateTime → 同名直通（若底層已存在同名欄位則直通；否則後備欄位）
        public float LastUpdateTime { get => _compatLastUpdateTime; set => _compatLastUpdateTime = value; }
        private float _compatLastUpdateTime;
    }
}