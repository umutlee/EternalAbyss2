#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections.Generic;

namespace DeepAbyssHive.EditorTools.Buildings
{
    /// <summary>
    /// Building Prefab Wizard
    /// - 從 Project 選取的模型/Prefab 批量建立「可放置建築」Prefab
    /// - 標準化：子物件居中(XZ)且貼地(Y=0)；在 Root 上加 BoxCollider；Layer=Building（存在時）
    /// </summary>
    public static class BuildingPrefabWizard
    {
        private const string OutFolder = "Assets/DeepAbyssHive/Buildings/Prefabs";
        private const string MenuRoot = "DeepAbyssHive/Art/Building/";

        [MenuItem(MenuRoot + "Make Building Prefabs From Selection", priority = 0)]
        public static void MakePrefabsFromSelection()
        {
            var selection = Selection.objects;
            if (selection == null || selection.Length == 0)
            {
                EditorUtility.DisplayDialog("Building Prefab Wizard", "請在 Project 視窗選取 1..N 個模型或 Prefab 資產。", "OK");
                return;
            }

            EnsureFolder(OutFolder);
            int created = 0, skipped = 0;
            foreach (var obj in selection)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path))
                {
                    skipped++; continue;
                }

                // 僅處理資產（模型/Prefab）
                var ext = Path.GetExtension(path).ToLowerInvariant();
                bool isModel = ext == ".fbx" || ext == ".obj" || ext == ".blend";
                bool isPrefab = ext == ".prefab" || AssetDatabase.GetMainAssetTypeAtPath(path).Name.Contains("GameObject");
                if (!isModel && !isPrefab)
                {
                    skipped++; continue;
                }

                var root = new GameObject(Path.GetFileNameWithoutExtension(path) + "_root");
                try
                {
                    // 實例化視覺作為子物件
                    var child = PrefabUtility.InstantiatePrefab(obj) as GameObject;
                    if (child == null) { Object.DestroyImmediate(root); skipped++; continue; }
                    child.transform.SetParent(root.transform, worldPositionStays: false);
                    child.transform.localPosition = Vector3.zero;
                    child.transform.localRotation = Quaternion.identity;

                    // 標準化：居中 XZ、貼地 Y=0
                    NormalizeChildToRoot(root, child);

                    // 加 Root BoxCollider（覆蓋整體尺寸）
                    BakeRootCollider(root);

                    // 設 Layer = Building（存在才設）
                    var buildingLayer = LayerMask.NameToLayer("Building");
                    if (buildingLayer >= 0)
                        SetLayerRecursively(root, buildingLayer);
                    else
                        SafeLog("[BUILDING] Layer 'Building' 不存在，已跳過設置 Layer（Prefab 仍建立）。");

                    // 存成 Prefab
                    string outPath = Path.Combine(OutFolder, Path.GetFileNameWithoutExtension(path) + ".prefab").Replace("\\", "/");
                    bool success;
                    PrefabUtility.SaveAsPrefabAsset(root, outPath, out success);
                    if (success) { created++; SafeLog($"[BUILDING] Prefab → {outPath}"); }
                    else { skipped++; SafeLog($"[BUILDING] Save Prefab 失敗 → {outPath}"); }
                }
                finally
                {
                    Object.DestroyImmediate(root);
                }
            }

            EditorUtility.DisplayDialog("Building Prefab Wizard", $"完成：created={created}, skipped={skipped}\\n輸出資料夾：{OutFolder}", "OK");
        }

        [MenuItem(MenuRoot + "Normalize Selected In Scene (CenterXZ + Ground + Rebuild Root BoxCollider)", priority = 10)]
        public static void NormalizeSelectedInScene()
        {
            var go = Selection.activeGameObject;
            if (go == null)
            {
                EditorUtility.DisplayDialog("Normalize", "請選取場景中的 Root 物件（其下應包含實際模型子物件）。", "OK");
                return;
            }

            // 找到第一個含 Renderer 的子樹為視覺
            var rends = go.GetComponentsInChildren<Renderer>(true);
            if (rends.Length == 0)
            {
                EditorUtility.DisplayDialog("Normalize", "找不到含 Renderer 的子物件，請確認選取的是包含模型的根物件。", "OK");
                return;
            }

            var child = go.transform.childCount > 0 ? go.transform.GetChild(0).gameObject : go;
            NormalizeChildToRoot(go, child);
            BakeRootCollider(go);
            SafeLog($"[BUILDING] 已標準化：{go.name}");
        }

        private static void NormalizeChildToRoot(GameObject root, GameObject child)
        {
            // 以 Renderer 合併 Bounds 計算偏移
            var rends = child.GetComponentsInChildren<Renderer>(true);
            if (rends.Length == 0) return;

            // 暫存原 transform
            var rT = root.transform;
            var cT = child.transform;
            var rootPos = rT.position; var rootRot = rT.rotation; var rootScale = rT.localScale;
            rT.position = Vector3.zero; rT.rotation = Quaternion.identity; rT.localScale = Vector3.one;
            cT.localPosition = Vector3.zero; cT.localRotation = Quaternion.identity;

            Bounds b = new Bounds(rends[0].bounds.center, rends[0].bounds.size);
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

            // 轉為 root 空間
            var centerLocal = rT.InverseTransformPoint(b.center);
            var sizeLocal = AbsVec(rT.InverseTransformVector(b.size));

            // 偏移：讓中心落在 XZ 原點，並讓底部貼到 y=0（child 往 -minY 方向移動）
            var minLocal = centerLocal - sizeLocal * 0.5f;
            var offset = new Vector3(-centerLocal.x, -minLocal.y, -centerLocal.z);
            cT.localPosition += offset;

            // 還原 root 變換
            rT.position = rootPos; rT.rotation = rootRot; rT.localScale = rootScale;
        }

        private static void BakeRootCollider(GameObject root)
        {
            // 重新計算在 Root 空間的合併 Bounds
            var rends = root.GetComponentsInChildren<Renderer>(true);
            if (rends.Length == 0) return;

            var rT = root.transform;
            Bounds b = new Bounds(rT.InverseTransformPoint(rends[0].bounds.center),
                                  AbsVec(rT.InverseTransformVector(rends[0].bounds.size)));
            for (int i = 1; i < rends.Length; i++)
            {
                var c = rT.InverseTransformPoint(rends[i].bounds.center);
                var s = AbsVec(rT.InverseTransformVector(rends[i].bounds.size));
                Bounds nb = new Bounds(c, s);
                b.Encapsulate(nb);
            }

            var bc = root.GetComponent<BoxCollider>();
            if (!bc) bc = root.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, b.size.y * 0.5f, 0f);
            bc.size = new Vector3(b.size.x, b.size.y, b.size.z);
        }

        private static Vector3 AbsVec(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursively(t.gameObject, layer);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace("\\", "/");
            var name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void SafeLog(string msg)
        {
            // 嘗試走 DAH 結構化日誌；失敗則退回 Unity Console
            try
            {
                DeepAbyssHive.Core.Logging.DAHLog.Info(DeepAbyssHive.Core.Logging.LogCategory.BUILDINGS, msg);
            }
            catch
            {
                Debug.Log(msg);
            }
        }
    }
}
#endif