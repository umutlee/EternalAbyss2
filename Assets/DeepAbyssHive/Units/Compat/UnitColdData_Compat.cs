using System.Collections.Generic;
using DeepAbyssHive.Units.Enums;

namespace DeepAbyssHive.Units.Data
{
    public partial struct UnitColdData
    {
        // 相容屬性（外部呼叫 UnitColdData.UnitId / Type / OwnerId 等）
        public int UnitId { get => _compatId; set => _compatId = value; }
        public int Id { get => _compatId; set => _compatId = value; } // 舊名相容
        public UnitType Type { get => _compatType; set => _compatType = value; }
        public int OwnerId { get => _compatOwnerId; set => _compatOwnerId = value; }

        public UnitAttributes BaseAttributes
        {
            get => _compatBaseAttributes ?? _compatAttributes;
            set { _compatBaseAttributes = value; _compatAttributes = value; }
        }
        public UnitAttributes Attributes { get => _compatAttributes; set => _compatAttributes = value; }

        // 舊名 Evolution：保留 object 以相容舊程式碼的動態內容
        public object Evolution { get => _compatEvolution; set => _compatEvolution = value; }

        // 與現用程式一致：使用陣列而非 List<object>
        public AdaptiveTrait[] AdaptiveTraits
        {
            get => _compatAdaptiveTraits ?? System.Array.Empty<AdaptiveTrait>();
            set => _compatAdaptiveTraits = value;
        }
        public string PrefabPath { get => _compatPrefabPath; set => _compatPrefabPath = value; }

        // 私有相容欄位
        private int _compatId;
        private UnitType _compatType;
        private int _compatOwnerId;
        private UnitAttributes _compatAttributes;
        private UnitAttributes? _compatBaseAttributes;
        private object _compatEvolution;
        private AdaptiveTrait[] _compatAdaptiveTraits;
        private string _compatPrefabPath;
    }
}
