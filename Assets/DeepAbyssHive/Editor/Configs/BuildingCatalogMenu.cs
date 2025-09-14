#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DeepAbyssHive.Core.Constants;

namespace DeepAbyssHive.EditorTools.Standards
{
    /// <summary>
    /// 建立或選取唯一的 BuildingCatalog，並確保位於 Resources/Configs
    /// </summary>
    public static class BuildingCatalogMenu
    {
        private const string Menu = "DeepAbyssHive/Configs/Create or Select BuildingCatalog";

        [MenuItem(Menu)]
        public static void CreateOrSelectMenu()
        {
            var obj = CreateOrSelectAtStandardPath();
            if (obj != null) { Selection.activeObject = obj; EditorGUIUtility.PingObject(obj); }
        }

        public static UnityEngine.Object CreateOrSelectAtStandardPath()
        {
            // 先找現有
            var guids = AssetDatabase.FindAssets("t:BuildingCatalogSO");
            if (guids != null && guids.Length > 0)
            {
                // 選第一個，必要時移動到標準資料夾
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                if (!path.Replace("\\","/").StartsWith(AssetPaths.ConfigsFolder + "/"))
                {
                    Ensure(AssetPaths.ConfigsFolder);
                    string name = Path.GetFileName(path);
                    string dest = AssetDatabase.GenerateUniqueAssetPath(AssetPaths.ConfigsFolder + "/" + name);
                    var err = AssetDatabase.MoveAsset(path, dest);
                    path = string.IsNullOrEmpty(err) ? dest : path;
                }
                return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            }

            // 建立新的（反射找類型）
            var type = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => SafeGetTypes(a))
                        .FirstOrDefault(t => t != null && t.Name == "BuildingCatalogSO" && typeof(ScriptableObject).IsAssignableFrom(t));
            if (type == null)
            {
                EditorUtility.DisplayDialog("BuildingCatalog", "找不到 BuildingCatalogSO 類型，無法建立資產。", "OK");
                return null;
            }

            Ensure(AssetPaths.ConfigsFolder);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(AssetPaths.ConfigsFolder + "/BuildingCatalog.asset");
            var instance = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(instance, assetPath);
            AssetDatabase.SaveAssets();
            return instance;
        }

        private static Type[] SafeGetTypes(System.Reflection.Assembly a)
        {
            try { return a.GetTypes(); } catch { return new Type[0]; }
        }

        private static void Ensure(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parts = folder.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
#endif