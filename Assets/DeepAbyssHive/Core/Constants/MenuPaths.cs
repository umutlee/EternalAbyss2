using System;

namespace DeepAbyssHive.Core.Constants
{
    /// <summary>
    /// Editor 菜單路徑常數（用於 [CreateAssetMenu] / [MenuItem]）
    /// 使用 const 以允許編譯期常數進屬性參數。
    /// </summary>
    public static class MenuPaths
    {
        public const string Root      = "DeepAbyssHive/";
        public const string Configs   = Root + "Configs/";    // 所有 ScriptableObject 設定檔
        public const string Templates = Root + "Templates/";  // 資料模板（如 *TemplateSO）
        public const string Tools     = Root + "Tools/";      // 工具
        public const string Dev       = Root + "Dev/";
    }
}