using DeepAbyssHive.Units.Enums;

namespace DeepAbyssHive.Units.Data
{
    public partial struct UnitData
    {
        // 舊代碼常見字段映射
        public string UnitName { get => _compatUnitName; set => _compatUnitName = value; }
        public float MoveSpeed { get => Speed; set => Speed = value; }
        public float AttackCooldown { get => _compatAttackCooldown; set => _compatAttackCooldown = value; }

        // 舊代碼有把 int UnitType 當 enum 用的情形 → 提供 enum 視圖
        public UnitType UnitTypeEnum { get => (UnitType)UnitType; set => UnitType = (int)value; }

        public float DetectionRange { get => _compatDetectionRange; set => _compatDetectionRange = value; }
        public string AttackSound { get => _compatAttackSound; set => _compatAttackSound = value; }
        public string DeathSound  { get => _compatDeathSound; set => _compatDeathSound = value; }

        private string _compatUnitName;
        private float _compatAttackCooldown;
        private float _compatDetectionRange;
        private string _compatAttackSound;
        private string _compatDeathSound;
    }
}