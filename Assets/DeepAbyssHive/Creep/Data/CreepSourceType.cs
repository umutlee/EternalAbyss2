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
        // 保留原有值以兼容现有代码（使用別名而非重複數字）
        Nest             = 6,  // 原本想等同MainHive，但改為獨立值
        Node             = 7,  // 原本想等同SubHive，但改為獨立值
        HiveCore         = 8,  // 原本想等同CreepColony，但改為獨立值
        CreepTumor       = 9,  // 原本想等同SpawningPool，但改為獨立值
        // 新增缺失成員
        Manual           = 10,
        Basic            = 11,
        Enhanced         = 12,
        Specialized      = 13
    }
}