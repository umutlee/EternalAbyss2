namespace DeepAbyssHive.Creep.Data
{
    /// <summary>菌毯來源類型（與現有呼叫對齊，包括 CreepTumor）。</summary>
    public enum CreepSourceType : int
    {
        Unknown    = 0,
        Nest       = 1,
        Node       = 2,
        HiveCore   = 3,
        CreepTumor = 4,
    }
}