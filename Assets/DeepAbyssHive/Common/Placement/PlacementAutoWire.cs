using System;
using System.Reflection;
using UnityEngine;
using DeepAbyssHive.Common.Placement;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Core.Logging;

/// <summary>
/// 在場景載入後自動嘗試把 PlacementValidator 與現有系統接線：
/// A) SpatialIndex 並聯：嘗試尋找 DeepAbyssHive.SpatialIndex.Managers.SpatialIndexManager 上的 QueryBounds/HasOverlap 之類方法；
///    期望方法簽章接受 (Bounds, LayerMask, float) 或 (Bounds) 並回傳「是否有衝突」的 bool；我們將其轉成「可放置 = !有衝突」。
/// B) 菌毯要求：嘗試尋找 DeepAbyssHive.Creep.Managers.CreepManager 上的 Covers/IsCovered 之類方法；
///    期望 (Bounds) -> bool，回傳是否被菌毯覆蓋。
/// 找不到時僅輸出 DEV HUD 警告，不會中斷流程。
/// </summary>
public static class PlacementAutoWire
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void TryWire()
    {
        TryWireSpatialIndex();
        TryWireCreep();
        // 立即輸出一次狀態摘要，方便驗收
        var cfg = GameConfigProvider.Current;
        DAHLog.Info(LogCategory.PLACEMENT, $"[DEV HUD] PlacementAutoWire: spatial={(PlacementValidator.HasSpatialIndex ? "OK" : "NONE")}, creep={(PlacementValidator.HasRequireCreep ? "OK" : "NONE")}, cfg(useSI={cfg.useSpatialIndexForPlacement}, requireCreep={cfg.requireCreep}, margin={cfg.margin:0.###}, minSpacing={cfg.minSpacing:0.###})");
    }

    private static void TryWireSpatialIndex()
    {
        if (PlacementValidator.HasSpatialIndex) return;
        // 以反射容錯尋找 Manager 與方法
        var typeName = "DeepAbyssHive.SpatialIndex.Managers.SpatialIndexManager";
        var t = Type.GetType(typeName);
        if (t == null)
        {
            DAHLog.Info(LogCategory.PLACEMENT, "[DEV HUD] AutoWire: 無 SpatialIndexManager（略過 A）");
            return;
        }
        var mgr = UnityEngine.Object.FindObjectOfType(t);
        if (mgr == null)
        {
            DAHLog.Info(LogCategory.PLACEMENT, "[DEV HUD] AutoWire: 場景中未找到 SpatialIndexManager（略過 A）");
            return;
        }

        MethodInfo mi = t.GetMethod("QueryBounds", BindingFlags.Public | BindingFlags.Instance)
                      ?? t.GetMethod("HasOverlap", BindingFlags.Public | BindingFlags.Instance);

        if (mi == null)
        {
            DAHLog.Warning(LogCategory.PLACEMENT, "[DEV HUD] AutoWire: SpatialIndex 上找不到 QueryBounds/HasOverlap，無法並聯（略過 A）");
            return;
        }

        // 建立委派：若方法回傳「是否衝突」，則可放置 = !ret
        PlacementValidator.SpatialIndexPredicate = (Bounds b, LayerMask mask, float margin) =>
        {
            try
            {
                var parameters = mi.GetParameters();
                object ret;
                if (parameters.Length == 3)
                    ret = mi.Invoke(mgr, new object[] { b, mask, margin });
                else if (parameters.Length == 1)
                    ret = mi.Invoke(mgr, new object[] { b });
                else
                    return true; // 參數不匹配：保守允許，並由 Physics 主導

                if (ret is bool collide) return !collide;
                return true;
            }
            catch (Exception e)
            {
                DAHLog.Warning(LogCategory.PLACEMENT, $"[DEV HUD] AutoWire SpatialIndex 失敗：{e.Message}");
                return true;
            }
        };
    }

    private static void TryWireCreep()
    {
        if (PlacementValidator.HasRequireCreep) return;
        var typeName = "DeepAbyssHive.Creep.Managers.CreepManager";
        var t = Type.GetType(typeName);
        if (t == null)
        {
            DAHLog.Info(LogCategory.PLACEMENT, "[DEV HUD] AutoWire: 無 CreepManager（略過 B）");
            return;
        }
        var mgr = UnityEngine.Object.FindObjectOfType(t);
        if (mgr == null)
        {
            DAHLog.Info(LogCategory.PLACEMENT, "[DEV HUD] AutoWire: 場景中未找到 CreepManager（略過 B）");
            return;
        }

        MethodInfo mi = t.GetMethod("Covers", BindingFlags.Public | BindingFlags.Instance)
                      ?? t.GetMethod("IsCovered", BindingFlags.Public | BindingFlags.Instance);

        if (mi == null)
        {
            DAHLog.Warning(LogCategory.PLACEMENT, "[DEV HUD] AutoWire: CreepManager 上找不到 Covers/IsCovered，未接菌毯要求（略過 B）");
            return;
        }

        PlacementValidator.RequireCreepPredicate = (Bounds b) =>
        {
            try
            {
                var parameters = mi.GetParameters();
                object ret;
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(Bounds))
                    ret = mi.Invoke(mgr, new object[] { b });
                else
                    ret = mi.Invoke(mgr, new object[] { b.center }); // 允許部分實作只接受中心點
                if (ret is bool covered) return covered;
                return true;
            }
            catch (Exception e)
            {
                DAHLog.Warning(LogCategory.PLACEMENT, $"[DEV HUD] AutoWire Creep 失敗：{e.Message}");
                return true;
            }
        };
    }
}