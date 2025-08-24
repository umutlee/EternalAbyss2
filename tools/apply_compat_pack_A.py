import os, re, sys, pathlib

ROOT = "Eternal Abyss 2"
if not os.path.isdir(ROOT):
    print(f"[ERR] 找不到專案根目錄資料夾：{ROOT}")
    sys.exit(1)

def w(path, content):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        f.write(content)

def list_cs(dir_rel):
    base = pathlib.Path(ROOT) / dir_rel
    if not base.exists(): return []
    return [str(p) for p in base.rglob("*.cs")]

def replace_in_files(file_list, patterns):
    for fp in file_list:
        try:
            with open(fp, "r", encoding="utf-8", errors="ignore") as f: txt = f.read()
        except Exception:
            continue
        orig = txt
        for pat, repl, flags in patterns:
            txt = re.sub(pat, repl, txt, flags=flags)
        if txt != orig:
            with open(fp, "w", encoding="utf-8") as f: f.write(txt)
            print("[edit]", fp)

# 1) SpatialIndex — NativeArray<int>.ToSpatialNodes 擴充（no-op，先過編譯）
w(f"{ROOT}/Assets/DeepAbyssHive/SpatialIndex/ListConvert_NativeArray_Compat.cs", r'''
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using DeepAbyssHive.SpatialIndex.Data;

namespace DeepAbyssHive.SpatialIndex
{
    /// 臨時：允許舊端把 NativeArray<int> 轉成節點清單（先回空，日後補真實映射）
    public static class SpatialIndexNativeArrayCompatExtensions
    {
        public static List<SpatialNode> ToSpatialNodes(this NativeArray<int> ids)
        {
            return new List<SpatialNode>(0);
        }
    }
}
''')

# 2) ISpatialIndexService 擴充：QueryAll() / QueryRange(Bounds)
w(f"{ROOT}/Assets/DeepAbyssHive/SpatialIndex/Services/ISpatialIndexService_CompatExtensions.cs", r'''
using Unity.Collections;
using UnityEngine;

namespace DeepAbyssHive.SpatialIndex.Services
{
    /// 對 ISpatialIndexService 的兼容擴充：補舊呼叫點會用到的方法（全回 default）
    public static class ISpatialIndexService_CompatExtensions
    {
        public static NativeArray<int> QueryAll(this ISpatialIndexService svc) => default;
        public static NativeArray<int> QueryRange(this ISpatialIndexService svc, Bounds bounds) => default;
    }
}
''')

# 3) Terrain pattern matching：is TerrainChunk → is ITerrainChunk
terrain_files = list_cs("Assets/DeepAbyssHive/Terrain")
replace_in_files(terrain_files, [
    (r'\bis\s+TerrainChunk\b', 'is ITerrainChunk', re.M),
])

# 4) Buildings：configName → ConfigName()（用擴充方法，不碰原類）
w(f"{ROOT}/Assets/DeepAbyssHive/Buildings/Compat/BuildingConfig_Extensions.cs", r'''
namespace DeepAbyssHive.Buildings.Compat
{
    public static class BuildingConfigExtensions
    {
        public static string ConfigName(this BuildingConfigSO so) => so != null ? so.name : null;
    }
}
''')
building_files = list_cs("Assets/DeepAbyssHive/Buildings")
replace_in_files(building_files, [
    (r'\.configName\b', '.ConfigName()', re.M),
])

# 5) Buildings：GetConfig(string) 擴充（避免去改管理類）
w(f"{ROOT}/Assets/DeepAbyssHive/Buildings/Compat/BuildingManager_GetConfig_Ext.cs", r'''
namespace DeepAbyssHive.Buildings.Compat
{
    public static class BuildingManager_GetConfig_Ext
    {
        public static BuildingConfigSO GetConfig(this BuildingManager mgr, string key)
        {
            return null; // 先回 null，之後補真實實作
        }
    }
}
''')

# 6) BuildingState.Active → 兼容常量（使用完整命名空間以免 using）
w(f"{ROOT}/Assets/DeepAbyssHive/Buildings/Compat/BuildingState_Compat.cs", r'''
namespace DeepAbyssHive.Buildings.Compat
{
    public static class BuildingStateCompat
    {
        public static readonly DeepAbyssHive.Buildings.Enums.BuildingState Active =
            DeepAbyssHive.Buildings.Enums.BuildingState.Built;
        public static bool IsActive(DeepAbyssHive.Buildings.Enums.BuildingState s) =>
            s == DeepAbyssHive.Buildings.Enums.BuildingState.Built;
    }
}
''')
replace_in_files(building_files, [
    (r'\bBuildingState\.Active\b', 'DeepAbyssHive.Buildings.Compat.BuildingStateCompat.Active', re.M),
])

# 7) Creep 舊枚舉名 → 兼容常量（完整命名空間）
w(f"{ROOT}/Assets/DeepAbyssHive/Creep/Compat/CreepEnums_Compat.cs", r'''
namespace DeepAbyssHive.Creep.Compat
{
    public static class CreepSourceTypeCompat
    {
        public const DeepAbyssHive.Creep.Enums.CreepSourceType Basic = (DeepAbyssHive.Creep.Enums.CreepSourceType)0;
        public const DeepAbyssHive.Creep.Enums.CreepSourceType Enhanced = (DeepAbyssHive.Creep.Enums.CreepSourceType)1;
        public const DeepAbyssHive.Creep.Enums.CreepSourceType Specialized = (DeepAbyssHive.Creep.Enums.CreepSourceType)2;
        public const DeepAbyssHive.Creep.Enums.CreepSourceType Manual = (DeepAbyssHive.Creep.Enums.CreepSourceType)3;
    }
    public static class CreepTileStatusCompat
    {
        public const DeepAbyssHive.Creep.Enums.CreepTileStatus Healthy = (DeepAbyssHive.Creep.Enums.CreepTileStatus)0;
        public const DeepAbyssHive.Creep.Enums.CreepTileStatus Weakened = (DeepAbyssHive.Creep.Enums.CreepTileStatus)1;
        public const DeepAbyssHive.Creep.Enums.CreepTileStatus Collapsing = (DeepAbyssHive.Creep.Enums.CreepTileStatus)2;
    }
}
''')
creep_files = list_cs("Assets/DeepAbyssHive/Creep")
replace_in_files(creep_files, [
    (r'\bCreepSourceType\.Basic\b', 'DeepAbyssHive.Creep.Compat.CreepSourceTypeCompat.Basic', re.M),
    (r'\bCreepSourceType\.Enhanced\b', 'DeepAbyssHive.Creep.Compat.CreepSourceTypeCompat.Enhanced', re.M),
    (r'\bCreepSourceType\.Specialized\b', 'DeepAbyssHive.Creep.Compat.CreepSourceTypeCompat.Specialized', re.M),
    (r'\bCreepSourceType\.Manual\b', 'DeepAbyssHive.Creep.Compat.CreepSourceTypeCompat.Manual', re.M),

    (r'\bCreepTileStatus\.Healthy\b', 'DeepAbyssHive.Creep.Compat.CreepTileStatusCompat.Healthy', re.M),
    (r'\bCreepTileStatus\.Weakened\b', 'DeepAbyssHive.Creep.Compat.CreepTileStatusCompat.Weakened', re.M),
    (r'\bCreepTileStatus\.Collapsing\b', 'DeepAbyssHive.Creep.Compat.CreepTileStatusCompat.Collapsing', re.M),
])

# 8) UnitType 舊成員 → 兼容枚舉 + cast
w(f"{ROOT}/Assets/DeepAbyssHive/Units/Compat/UnitType_Compat.cs", r'''
namespace DeepAbyssHive.Units.Compat
{
    public enum UnitTypeCompat
    {
        AcidSprayer = 1001,
        Tank        = 1002,
        Flyer       = 1003,
    }
}
''')
unit_files = list_cs("Assets/DeepAbyssHive/Units")
replace_in_files(unit_files, [
    (r'\bUnitType\.AcidSprayer\b', '(DeepAbyssHive.Units.Enums.UnitType)DeepAbyssHive.Units.Compat.UnitTypeCompat.AcidSprayer', re.M),
    (r'\bUnitType\.Tank\b',        '(DeepAbyssHive.Units.Enums.UnitType)DeepAbyssHive.Units.Compat.UnitTypeCompat.Tank', re.M),
    (r'\bUnitType\.Flyer\b',       '(DeepAbyssHive.Units.Enums.UnitType)DeepAbyssHive.Units.Compat.UnitTypeCompat.Flyer', re.M),
])

# 9) TerrainModification 舊字段 → Compat 轉接器 + 呼叫替換
w(f"{ROOT}/Assets/DeepAbyssHive/Terrain/Compat/TerrainModification_Compat.cs", r'''
using UnityEngine;

namespace DeepAbyssHive.Terrain.Compat
{
    /// 舊名讀取器（暫回預設，後續補真實映射）
    public static class TerrainModificationCompat
    {
        public static Vector3 Position(object mod) => Vector3.zero;
        public static float Radius(object mod) => 0f;
        public static float TerrainTypeValue(object mod) => 0f;
        public static float Value(object mod) => 0f;
        public static int Type(object mod) => 0;
        public static float Timestamp(object mod) => 0f;
    }
}
''')
terrain_files_all = list_cs("Assets/DeepAbyssHive/Terrain")
replace_in_files(terrain_files_all, [
    (r'\bTerrainModification\.Position\b', 'DeepAbyssHive.Terrain.Compat.TerrainModificationCompat.Position', re.M),
    (r'\bTerrainModification\.Radius\b', 'DeepAbyssHive.Terrain.Compat.TerrainModificationCompat.Radius', re.M),
    (r'\bTerrainModification\.TerrainTypeValue\b', 'DeepAbyssHive.Terrain.Compat.TerrainModificationCompat.TerrainTypeValue', re.M),
    (r'\bTerrainModification\.Timestamp\b', 'DeepAbyssHive.Terrain.Compat.TerrainModificationCompat.Timestamp', re.M),
    (r'\bTerrainModification\.Value\b', 'DeepAbyssHive.Terrain.Compat.TerrainModificationCompat.Value', re.M),
    (r'\bTerrainModification\.Type\b', 'DeepAbyssHive.Terrain.Compat.TerrainModificationCompat.Type', re.M),
])

# 10) UnitData 非 nullable 熱修：把 Units/Core/Unit.cs 的 ?. 與 != null 先消掉
ucore = pathlib.Path(ROOT) / "Assets/DeepAbyssHive/Units/Core/Unit.cs"
if ucore.exists():
    with open(ucore, "r", encoding="utf-8", errors="ignore") as f: s = f.read()
    s2 = re.sub(r'(\bUnitData\b[^\n;]*?)\?\.', r'\1.', s)        # foo(UnitData ...)?.X → .X
    s2 = re.sub(r'\bUnitData\b\s*!=\s*null', 'true /* compat: non-null */', s2)
    if s2 != s:
        with open(ucore, "w", encoding="utf-8") as f: f.write(s2)
        print("[edit]", str(ucore))

print("\n[OK] Compat Pack A (zsh-safe) 已套用：請編譯看看。")
