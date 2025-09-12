using UnityEngine;

namespace DeepAbyssHive.Core.Logging
{
    /// <summary>
    /// DAH 日誌系統測試腳本。可掛載到任意 GameObject 進行測試。
    /// </summary>
    public class DAHLogTest : MonoBehaviour
    {
        [Header("測試設定")]
        public bool testOnStart = true;
        public KeyCode testKey = KeyCode.F12;

        void Start()
        {
            if (testOnStart)
            {
                RunTests();
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(testKey))
            {
                RunTests();
            }
        }

        void RunTests()
        {
            DAHLog.Info(LogCategory.SYSTEM, "DAH 日誌系統測試開始");
            
            // 測試各種分類
            DAHLog.Info(LogCategory.CREEP, "菌毯系統正常運行");
            DAHLog.Info(LogCategory.TERRAIN, "地形生成完成");
            DAHLog.Info(LogCategory.UNITS, "單位移動測試");
            DAHLog.Info(LogCategory.BUILDINGS, "建築放置測試");
            DAHLog.Info(LogCategory.PATHFINDING, "路徑規劃測試");
            
            // 測試不同日誌等級
            DAHLog.Warn(LogCategory.SYSTEM, "這是一個警告訊息");
            DAHLog.Error(LogCategory.SYSTEM, "這是一個錯誤訊息");
            DAHLog.Dev(LogCategory.DEV, "這是開發期日誌（僅 Editor 模式）");
            
            DAHLog.Info(LogCategory.SYSTEM, "DAH 日誌系統測試完成 - 請開啟 Smart Console (Ctrl+Alt+L) 查看結果");
        }
    }
}