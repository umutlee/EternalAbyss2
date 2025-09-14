using System;
using UnityEngine;
using DeepAbyssHive.Core.Logging;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Buildings.Config;

namespace DeepAbyssHive.Buildings.Selection
{
    /// <summary>
    /// 全域建築選取提供者（靜態）。持有 Catalog、目前索引，並提供循環切換；有事件通知。
    /// </summary>
    public static class BuildingSelectionProvider
    {
        public static event Action<BuildingCatalogEntry> OnSelectionChanged;

        private static BuildingCatalogSO _catalog;
        private static int _index;
        private static bool _loggedOnce;

        public static void InitializeFromConfig()
        {
            var cfg = GameConfigProvider.Current;
            if (cfg == null) return;
            _catalog = cfg.buildingCatalog;
            if (!_loggedOnce)
            {
                _loggedOnce = true;
                var count = (_catalog != null && _catalog.entries != null) ? _catalog.entries.Length : 0;
                DAHLog.Info(LogCategory.CONFIG, $"[BUILDINGS] Catalog entries={count}, nextKey={cfg.buildingCycleNextKey}, prevKey={cfg.buildingCyclePrevKey}");
            }
            // 初始選擇（若有資料）
            if (HasCatalog && (_index < 0 || _index >= _catalog.Count))
            {
                _index = 0;
                OnSelectionChanged?.Invoke(CurrentEntry);
            }
        }

        public static bool HasCatalog => _catalog != null && _catalog.entries != null && _catalog.entries.Length > 0;
        public static BuildingCatalogEntry CurrentEntry
        {
            get
            {
                if (!HasCatalog) return null;
                _index = Mathf.Clamp(_index, 0, _catalog.entries.Length - 1);
                return _catalog.entries[_index];
            }
        }

        public static void CycleNext()
        {
            if (!HasCatalog) return;
            _index = (_index + 1) % _catalog.entries.Length;
            var e = CurrentEntry;
            if (e != null)
            {
                DAHLog.Info(LogCategory.PLACEMENT, $"[BuildingCycle] Selected: {e.id}");
                OnSelectionChanged?.Invoke(e);
            }
        }

        public static void CyclePrev()
        {
            if (!HasCatalog) return;
            _index = (_index - 1 + _catalog.entries.Length) % _catalog.entries.Length;
            var e = CurrentEntry;
            if (e != null)
            {
                DAHLog.Info(LogCategory.PLACEMENT, $"[BuildingCycle] Selected: {e.id}");
                OnSelectionChanged?.Invoke(e);
            }
        }
    }
}