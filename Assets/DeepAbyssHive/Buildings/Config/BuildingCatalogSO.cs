using UnityEngine;

namespace DeepAbyssHive.Buildings.Config
{
    /// <summary>
    /// 建築型錄項目：最小可用欄位。footprintHalfExtents 供碰撞/預覽用（沿用既有驗證規則）。
    /// </summary>
    [System.Serializable]
    public class BuildingCatalogEntry
    {
        public string id;
        public GameObject prefab;
        [Tooltip("碰撞/佔地的半徑（世界單位）。對應 Validation 的 halfExtents。")]
        public Vector3 footprintHalfExtents = new Vector3(1, 1, 1);
        [Tooltip("（預留）成本或價格。當前不檢查資源，只展示用。")]
        public int cost = 0;
    }

    /// <summary>
    /// 建築型錄（ScriptableObject）
    /// </summary>
    [CreateAssetMenu(menuName = "DeepAbyssHive/Configs/Building Catalog", fileName = "BuildingCatalog")]
    public class BuildingCatalogSO : ScriptableObject
    {
        public BuildingCatalogEntry[] entries = new BuildingCatalogEntry[0];

        public int Count => entries != null ? entries.Length : 0;
        public BuildingCatalogEntry Get(int index)
        {
            if (entries == null || entries.Length == 0) return null;
            if (index < 0) index = 0;
            if (index >= entries.Length) index = entries.Length - 1;
            return entries[index];
        }
    }
}