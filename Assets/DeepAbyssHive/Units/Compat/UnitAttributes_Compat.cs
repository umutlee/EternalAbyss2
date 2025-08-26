namespace DeepAbyssHive.Units.Data
{
    public partial struct UnitAttributes
    {
        // 舊代碼使用 MaxEnergy，現結構多為 MaxHealth → 提供別名
        public float MaxEnergy { get => MaxHealth; set => MaxHealth = value; }

        // 舊代碼使用 DetectionRange，現結構多為 SightRange → 提供別名
        public float DetectionRange { get => SightRange; set => SightRange = value; }
    }
}