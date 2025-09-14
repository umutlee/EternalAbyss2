using System;

namespace DeepAbyssHive.Core.Constants
{
    /// <summary>
    /// 標準儲存位置常數。Editor 工具與自動搬運會使用這些。
    /// </summary>
    public static class AssetPaths
    {
        // 統一：所有 Config SO 一律放 Resources/Configs，便於 Resources.Load / Addressables
        public const string ConfigsFolder  = "Assets/Resources/Configs";

        // Template 預設放這裡（可依需要微調）
        public const string TemplatesFolder = "Assets/DeepAbyssHive/Units/Templates";

        // Building Prefabs（供 T17c 類工具）
        public const string BuildingPrefabs = "Assets/DeepAbyssHive/QA/Smoke/Dev/Art/Placeholders/Prefabs";
    }
}