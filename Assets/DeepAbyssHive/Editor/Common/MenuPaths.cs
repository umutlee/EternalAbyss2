#if UNITY_EDITOR
namespace DeepAbyssHive.EditorTools
{
    /// <summary>集中管理 Editor 選單路徑。所有新選單請使用這裡的常數。</summary>
    internal static class MenuPaths
    {
        public const string Root        = "DeepAbyssHive/";
        public const string Tools       = Root + "Tools/";
        public const string Configs     = Root + "Configs/";
        public const string Templates   = Root + "Templates/";
        public const string Art         = Root + "Art/";
        public const string HUD         = Root + "HUD/";
        public const string Units       = Root + "Units/";
        public const string Debug       = Root + "Debug/";
    }
}
#endif