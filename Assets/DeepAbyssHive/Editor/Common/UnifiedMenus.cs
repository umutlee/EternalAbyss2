#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DeepAbyssHive.EditorTools
{
    /// <summary>
    /// 提供一組「乾淨層級」的選單入口，包裝既有工具。
    /// - 不直接依賴其他 Editor 類別：採反射/檔案存在判斷，避免編譯錯誤。
    /// - 之後逐步把舊選單遷移到 MenuPaths 常數上即可。
    /// </summary>
    internal static class UnifiedMenus
    {
        // ===== Tools =====
        // Standards & Fixes菜單已在DAHStandardsWindow.cs中定義，避免重複

        [MenuItem(MenuPaths.Tools + "Smart Console", priority = 11)]
        private static void OpenSmartConsole()
        {
            var t = Type.GetType("DeepAbyssHive.EditorTools.Logging.SmartConsoleWindow, Assembly-CSharp-Editor");
            if (t == null) t = Type.GetType("SmartConsoleWindow");
            if (t != null && typeof(EditorWindow).IsAssignableFrom(t))
                EditorWindow.GetWindow(t, false, "Smart Console");
            else
                EditorUtility.DisplayDialog("Smart Console", "找不到 SmartConsoleWindow。\n請確認 Editor/Logging 是否已導入。", "OK");
        }

        // ===== Configs =====
        // GameConfig菜單已在GameConfigMenu.cs中定義，避免重複

        [MenuItem(MenuPaths.Configs + "Open Resources/Configs", priority = 50)]
        private static void OpenConfigsFolder() => PingOrCreateFolder("Assets/Resources/Configs");

        // ===== Templates =====
        [MenuItem(MenuPaths.Templates + "Create UnitTemplateSO", priority = 0)]
        private static void CreateUnitTemplateSO()
        {
            // 尋找 UnitTemplateSO 類型並建立資產於 Resources/Configs/UnitTemplates
            var t = Type.GetType("DeepAbyssHive.Units.Config.UnitTemplateSO, Assembly-CSharp");
            if (t == null) { EditorUtility.DisplayDialog("Create UnitTemplateSO", "找不到 UnitTemplateSO 腳本。", "OK"); return; }
            var asset = ScriptableObject.CreateInstance(t);
            var dir = "Assets/Resources/Configs/UnitTemplates";
            EnsureFolder(dir);
            var path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/UnitTemplate.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            Ping(path);
        }

        [MenuItem(MenuPaths.Templates + "Create BuildingCatalogSO", priority = 1)]
        private static void CreateBuildingCatalog()
        {
            var t = Type.GetType("DeepAbyssHive.Buildings.Config.BuildingCatalogSO, Assembly-CSharp");
            if (t == null) { EditorUtility.DisplayDialog("Create BuildingCatalogSO", "找不到 BuildingCatalogSO 腳本。", "OK"); return; }
            var asset = ScriptableObject.CreateInstance(t);
            var dir = "Assets/Resources/Configs";
            EnsureFolder(dir);
            var path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/BuildingCatalog.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            Ping(path);
        }

        // ===== Art / Building =====
        [MenuItem(MenuPaths.Art + "Building/Make Prefabs From Selection", priority = 0)]
        private static void Art_Building_MakePrefabsFromSelection()
        {
            // 轉呼叫 BuildingPrefabWizard（若存在）；否則提示
            var t = Type.GetType("DeepAbyssHive.EditorTools.Buildings.BuildingPrefabWizard, Assembly-CSharp-Editor");
            var m = t?.GetMethod("MakePrefabsFromSelection", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (m != null) { m.Invoke(null, null); }
            else EditorUtility.DisplayDialog("Building Prefab Wizard", "找不到 BuildingPrefabWizard。\n請先導入 T17C 補丁。", "OK");
        }

        [MenuItem(MenuPaths.Art + "Building/Normalize Selected In Scene", priority = 1)]
        private static void Art_Building_NormalizeInScene()
        {
            var t = Type.GetType("DeepAbyssHive.EditorTools.Buildings.BuildingPrefabWizard, Assembly-CSharp-Editor");
            var m = t?.GetMethod("NormalizeSelectedInScene", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (m != null) { m.Invoke(null, null); }
            else EditorUtility.DisplayDialog("Normalize", "找不到 BuildingPrefabWizard。\n請先導入 T17C 補丁。", "OK");
        }

        // ===== Helpers =====
        private static void PingOrCreateFolder(string path)
        {
            EnsureFolder(path);
            Ping(path);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace("\\", "/");
            var name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void Ping(string assetOrFolderPath)
        {
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetOrFolderPath);
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }
    }
}
#endif