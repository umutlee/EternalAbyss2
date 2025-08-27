namespace DeepAbyssHive.Creep.Data
{
    /// <summary>菌毯來源類型（與現有呼叫對齊，包括 CreepTumor）。</summary>
    public enum CreepSourceType : int
    {
        Unknown          = 0,
        MainHive         = 1,
        SubHive          = 2,
        CreepColony      = 3,
        SpawningPool     = 4,
        EvolutionChamber = 5,
        // 保留原有值以兼容现有代码
        Nest             = 1, // 等同于MainHive
        Node             = 2, // 等同于SubHive
        HiveCore         = 3, // 等同于CreepColony
        CreepTumor       = 4, // 等同于SpawningPool
    }
}