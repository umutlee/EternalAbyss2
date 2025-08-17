using System;

namespace DeepAbyssHive.Creep.Data
{
    /// <summary>
    /// 菌毯源點類型枚舉
    /// 定義不同類型的菌毯生成源點
    /// </summary>
    [Serializable]
    public enum CreepSourceType
    {
        /// <summary>無源點</summary>
        None = 0,
        
        /// <summary>主巢穴 - 最強的菌毯源點</summary>
        MainHive = 1,
        
        /// <summary>次級巢穴 - 中等強度源點</summary>
        SubHive = 2,
        
        /// <summary>菌毯腫瘤 - 基礎擴張源點</summary>
        CreepTumor = 3,
        
        /// <summary>孢子爬行者 - 移動式源點</summary>
        SporeCrawler = 4,
        
        /// <summary>脊刺爬行者 - 防禦型源點</summary>
        SpineCrawler = 5,
        
        /// <summary>進化腔 - 特殊功能源點</summary>
        EvolutionChamber = 6,
        
        /// <summary>孵化池 - 單位生產源點</summary>
        SpawningPool = 7,
        
        /// <summary>菌毯殖民地 - 大範圍源點</summary>
        CreepColony = 8,
        
        /// <summary>感染建築 - 轉化的敵方建築</summary>
        InfestedStructure = 9,
        
        /// <summary>臨時源點 - 短期存在的源點</summary>
        Temporary = 10
    }
}