using System;
using UnityEngine;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Core.Services.Config
{
    /// <summary>
    /// 運行時配置審計：檢查圖層與遮罩一致性，啟動時輸出診斷。
    /// - 驗證：Building 圖層存在、放置/建築相關遮罩包含 Building 層。
    /// - 不中斷：只輸出 Warning/Info，避免影響遊戲流程。
    /// </summary>
    public static class ConfigAuditor
    {
        private const string BuildingLayerName = "Building";

        /// <summary>
        /// 執行一次審計。建議在 Boot 完成 Managers 構建後呼叫。
        /// </summary>
        public static void RunOnce()
        {
            try
            {
                int buildingLayer = LayerMask.NameToLayer(BuildingLayerName);
                bool layerExists = buildingLayer >= 0;
                if (!layerExists)
                {
                    DAHLog.Warn(LogCategory.CONFIG, $"[ConfigAudit] 缺少層 '{BuildingLayerName}'。請到 Project Settings > Tags and Layers 新增。");
                    return;
                }

                // 使用 Common/Placement 的工具統一獲取遮罩（如不可用則回退）
                int buildingOnlyMask = 0;
                try
                {
                    buildingOnlyMask = DeepAbyssHive.Common.Placement.PlacementLayerUtil.GetBuildingOnlyMask();
                }
                catch
                {
                    // 回退：若工具不可用，退而檢查該層位是否能被位運算捕捉
                    buildingOnlyMask = 1 << buildingLayer;
                }

                bool maskIncludes = (buildingOnlyMask & (1 << buildingLayer)) != 0;
                if (!maskIncludes)
                {
                    DAHLog.Warn(LogCategory.CONFIG, $"[ConfigAudit] 遮罩未包含 '{BuildingLayerName}' 層。mask=0x{buildingOnlyMask:X}");
                }
                else
                {
                    DAHLog.Info(LogCategory.CONFIG, $"[ConfigAudit] Building 層 OK (#{buildingLayer})，遮罩包含驗證通過。");
                }
            }
            catch (Exception ex)
            {
                DAHLog.Error(LogCategory.CONFIG, $"[ConfigAudit] 例外：{ex}");
            }
        }

        /// <summary>
        /// 打印一行 GameConfig 摘要，避免強耦合：盡量透過已存在的鍵或反射安全讀取。
        /// </summary>
        public static void PrintGameConfigSummary()
        {
            try
            {
                // 盡量使用靜態入口以避免反射成本
                var cfg = ConfigProviderHooks.GetConfig();
                if (cfg != null)
                {
                    // 嘗試讀取常見欄位，缺失時以反射/預設容錯
                    string name = cfg.name ?? "GameConfig";
                    float minSpacing = GetFloat(cfg, "minSpacing", 1.0f);
                    float snapSize = GetFloat(cfg, "snapSize", 1.0f);
                    float rotationStep = GetFloat(cfg, "rotationStepDegrees", 0f);
                    bool useSpatialIndex = GetBool(cfg, "useSpatialIndexForPlacement", false);
                    bool requireCreep = GetBool(cfg, "requireCreep", false);
                    int pathJobsPerFrame = GetInt(cfg, "pathJobsPerFrame", 8);

                    float creepSpeedMul = GetFloat(cfg, "creepSpeedMul", 1.25f);
                    float offCreepSpeedMul = GetFloat(cfg, "offCreepSpeedMul", 1.0f);

                    float unitDynCheckInterval = GetFloat(cfg, "unitDynCheckInterval", 0.25f);
                    float unitDynRepathCooldown = GetFloat(cfg, "unitDynRepathCooldown", 1.0f);

                    string line =
                        $"[GameConfig] {name}: minSpacing={minSpacing:0.##} snap={snapSize:0.##} rotStep={rotationStep:0.##} " +
                        $"useSI={useSpatialIndex} requireCreep={requireCreep} pathJobs={pathJobsPerFrame} " +
                        $"creepMul={creepSpeedMul:0.##}/{offCreepSpeedMul:0.##} dynChk={unitDynCheckInterval:0.##} repathCD={unitDynRepathCooldown:0.##}";

                    DAHLog.Info(LogCategory.CONFIG, line);
                }
                else
                {
                    // 後備：已有 GameConfigBootLogger 亦會輸出；此處只記一行
                    DAHLog.Warn(LogCategory.CONFIG, "[GameConfig] 未載入，使用後備預設。");
                }
            }
            catch (Exception ex)
            {
                DAHLog.Error(LogCategory.CONFIG, $"[GameConfig] 摘要輸出失敗：{ex}");
            }
        }

        private static float GetFloat(ScriptableObject obj, string name, float defV)
        {
            var t = obj.GetType();
            var f = t.GetField(name);
            if (f != null && f.FieldType == typeof(float)) return (float)f.GetValue(obj);
            var p = t.GetProperty(name);
            if (p != null && p.PropertyType == typeof(float)) return (float)p.GetValue(obj);
            return defV;
        }

        private static int GetInt(ScriptableObject obj, string name, int defV)
        {
            var t = obj.GetType();
            var f = t.GetField(name);
            if (f != null && f.FieldType == typeof(int)) return (int)f.GetValue(obj);
            var p = t.GetProperty(name);
            if (p != null && p.PropertyType == typeof(int)) return (int)p.GetValue(obj);
            return defV;
        }

        private static bool GetBool(ScriptableObject obj, string name, bool defV)
        {
            var t = obj.GetType();
            var f = t.GetField(name);
            if (f != null && f.FieldType == typeof(bool)) return (bool)f.GetValue(obj);
            var p = t.GetProperty(name);
            if (p != null && p.PropertyType == typeof(bool)) return (bool)p.GetValue(obj);
            return defV;
        }
    }
}