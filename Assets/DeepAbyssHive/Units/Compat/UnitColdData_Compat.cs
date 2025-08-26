using System.Collections.Generic;
using DeepAbyssHive.Units.Enums;

namespace DeepAbyssHive.Units.Data
{
    public partial struct UnitColdData
    {
        // 舊代碼常用字段：若底層已有對應（如 UnitId/UnitType/EvolutionInfo），請改為直通
        public int Id { get => _compatId; set => _compatId = value; }               // 若已有 UnitId，改為直通
        public UnitType Type { get => _compatType; set => _compatType = value; }    // 若已有 UnitType (enum/int)，改為直通轉型
        public int OwnerId { get => _compatOwnerId; set => _compatOwnerId = value; }

        public UnitAttributes BaseAttributes
        {
            get => _compatBaseAttributes ?? _compatAttributes;   // 盡量回傳現用 Attributes
            set
            {
                _compatBaseAttributes = value;
                _compatAttributes = value;
            }
        }

        public UnitAttributes Attributes { get => _compatAttributes; set => _compatAttributes = value; }

        // 舊名 Evolution → 新結構多為 EvolutionInfo
        public object Evolution { get => _compatEvolution; set => _compatEvolution = value; }

        public List<object> AdaptiveTraits { get => _compatAdaptiveTraits ??= new List<object>(); set => _compatAdaptiveTraits = value; }
        public string PrefabPath { get => _compatPrefabPath; set => _compatPrefabPath = value; }

        private int _compatId;
        private UnitType _compatType;
        private int _compatOwnerId;
        private UnitAttributes _compatAttributes;
        private UnitAttributes? _compatBaseAttributes;
        private object _compatEvolution;
        private List<object> _compatAdaptiveTraits;
        private string _compatPrefabPath;
    }
}