using System;

namespace DeepAbyssHive.Core.Config
{
    /// <summary>
    /// 統一定義「Unit Grounding」對應的 GameConfig 欄位名稱。
    /// 若 GameConfigSO 目前尚未新增這些欄位，系統會使用預設值。
    /// </summary>
    public static class GameConfigKeys_UnitGrounding
    {
        // bool
        public const string Enable = "enableUnitGrounding";
        // int
        public const string PerFrame = "unitGroundPerFrame";
        // float
        public const string SampleInterval = "unitGroundSampleInterval";
        public const string Offset = "unitGroundOffset";
        public const string SafeRadius = "unitSafeRadius";
        public const string MinY = "unitMinY";
        public const string CastUp = "unitGroundCastUp";
        public const string CastDown = "unitGroundCastDown";
    }
}