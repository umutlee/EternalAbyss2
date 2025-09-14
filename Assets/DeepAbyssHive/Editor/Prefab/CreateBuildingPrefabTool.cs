#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeepAbyssHive.EditorTools.Prefab
{
    public static class CreateBuildingPrefabTool
    {
        private const string MenuCenter = "DeepAbyssHive/Tools/Prefab/Center Children + Reset BoxCollider";
        private const string MenuCreate = "Assets/DeepAbyssHive/Create Building Prefab (DAH)";
        private const string PrefabFolder = "Assets/DeepAbyssHive/QA/Smoke/Dev/Art/Placeholders/Prefabs";

        // ---------- A) Hierarchy 選取 Root：置中 + 貼地 + 重置 BoxCollider ----------
        [MenuItem(MenuCenter, priority = 0)]
        public static void CenterAndResetForSelection()
        {
            var objs = Selection.gameObjects;
            if (objs == null || objs.Length == 0)
            {
                EditorUtility.DisplayDialog("DAH Prefab Tool", "請先在 Hierarchy 選取一個或多個 Root 物件。", "OK");
                return;
            }

            int ok = 0, fail = 0;
            foreach (var go in objs)
            {
                if (ProcessCenterAndReset(go)) ok++; else fail++;
            }
            EditorUtility.DisplayDialog("DAH Prefab Tool", $"完成：{ok} 成功，{fail} 跳過/失敗。", "OK");
        }

        [MenuItem("GameObject/DeepAbyssHive/Center Children + Reset BoxCollider", false, 0)]
        public static void CtxMenuCenter() => CenterAndResetForSelection();

        // ---------- B) Project 選取 .fbx/.obj：建立遊戲用 Prefab ----------
        [MenuItem(MenuCreate, validate = true)]
        private static bool ValidateCreatePrefab()
        {
            foreach (var o in Selection.objects)
            {
                var path = AssetDatabase.GetAssetPath(o);
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext == ".fbx" || ext == ".obj")
                    return true;
            }
            return false;
        }

        [MenuItem(MenuCreate, priority = 0)]
        private static void CreatePrefab()
        {
            EnsureFolder(PrefabFolder);

            int created = 0, failed = 0;
            foreach (var o in Selection.objects)
            {
                var path = AssetDatabase.GetAssetPath(o);
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext != ".fbx" && ext != ".obj") continue;

                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null) { failed++; continue; }

                // 建臨時 Root
                string cleanName = SanitizeName(Path.GetFileNameWithoutExtension(path));
                var root = new GameObject($"pfb_Build_{cleanName}");
                try
                {
                    Undo.RegisterCreatedObjectUndo(root, "DAH Create Building Prefab");
                    // 實例化模型為子物件
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
                    inst.transform.SetParent(root.transform, false);
                    inst.transform.localPosition = Vector3.zero;
                    inst.transform.localRotation = Quaternion.identity;
                    // 做置中、貼地、重置 BoxCollider
                    if (!ProcessCenterAndReset(root))
                        throw new System.SystemException("Center/Reset 失敗（找不到 Renderer?）");

                    // 設 Layer=Building（若存在）
                    int buildLayer = LayerMask.NameToLayer("Building");
                    if (buildLayer >= 0)
                        root.layer = buildLayer;

                    // 儲存 Prefab
                    string outPath = AssetDatabase.GenerateUniqueAssetPath($"{PrefabFolder}/{root.name}.prefab");
                    PrefabUtility.SaveAsPrefabAssetAndConnect(root, outPath, InteractionMode.AutomatedAction);
                    created++;

                    // （可選）更新 Catalog：若恰好找到 1 個 BuildingCatalogSO
                    TryUpdateCatalog(outPath, root.GetComponent<BoxCollider>());
                    Debug.Log($"[DAH Prefab] Created: {outPath}");
                }
                catch (System.SystemException ex)
                {
                    Debug.LogError($"[DAH Prefab] 失敗：{path} → {ex.Message}");
                    failed++;
                }
                finally
                {
                    Object.DestroyImmediate(root);
                }
            }

            EditorUtility.DisplayDialog("DAH Create Prefab", $"完成：{created} 建立，{failed} 失敗/跳過。", "OK");
            AssetDatabase.Refresh();
        }

        // ---------- 核心：置中 + 貼地 + 重置 BoxCollider ----------
        private static bool ProcessCenterAndReset(GameObject root)
        {
            if (root == null) return false;

            var renderers = root.GetComponentsInChildren<Renderer>(includeInactive: false);
            if (renderers == null || renderers.Length == 0) return false;

            Undo.RegisterFullObjectHierarchyUndo(root, "DAH Center & Reset");

            // 世界 bounds
            var worldBounds = ComputeWorldBounds(renderers);
            var t = root.transform;

            // XZ 置中
            Vector3 centerOffsetWorld = worldBounds.center - t.position;
            Vector3 centerOffsetLocal = t.InverseTransformVector(centerOffsetWorld);
            Vector3 xzOffsetLocal = new Vector3(centerOffsetLocal.x, 0f, centerOffsetLocal.z);
            MoveImmediateChildren(t, -xzOffsetLocal);

            // 重新算、底貼地
            worldBounds = ComputeWorldBounds(root.GetComponentsInChildren<Renderer>(false));
            float baseDeltaWorld = worldBounds.min.y - t.position.y;
            Vector3 baseOffsetLocal = t.InverseTransformVector(new Vector3(0f, baseDeltaWorld, 0f));
            MoveImmediateChildren(t, -new Vector3(0f, baseOffsetLocal.y, 0f));

            // 算本地 bounds、重置 BoxCollider
            var localBounds = ComputeLocalBounds(t);
            if (localBounds.size.sqrMagnitude <= 0f) return false;

            var bc = root.GetComponent<BoxCollider>();
            if (bc == null) bc = Undo.AddComponent<BoxCollider>(root);
            bc.center = new Vector3(0f, localBounds.size.y * 0.5f, 0f);
            bc.size = localBounds.size;
            bc.isTrigger = false;
            EditorUtility.SetDirty(bc);
            EditorUtility.SetDirty(root);
            return true;
        }

        private static void MoveImmediateChildren(Transform root, Vector3 deltaLocal)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                var c = root.GetChild(i);
                c.localPosition += deltaLocal;
            }
        }

        private static Bounds ComputeWorldBounds(Renderer[] renderers)
        {
            var b = new Bounds(renderers[0].bounds.center, renderers[0].bounds.size);
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            return b;
        }

        private static Bounds ComputeLocalBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(includeInactive: false);
            bool hasPoint = false;
            Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var r in renderers)
            {
                var wb = r.bounds;
                Vector3 ext = wb.extents;
                Vector3 c = wb.center;
                Vector3[] corners = new Vector3[8]
                {
                    new Vector3(c.x - ext.x, c.y - ext.y, c.z - ext.z),
                    new Vector3(c.x + ext.x, c.y - ext.y, c.z - ext.z),
                    new Vector3(c.x - ext.x, c.y + ext.y, c.z - ext.z),
                    new Vector3(c.x + ext.x, c.y + ext.y, c.z - ext.z),
                    new Vector3(c.x - ext.x, c.y - ext.y, c.z + ext.z),
                    new Vector3(c.x + ext.x, c.y - ext.y, c.z + ext.z),
                    new Vector3(c.x - ext.x, c.y + ext.y, c.z + ext.z),
                    new Vector3(c.x + ext.x, c.y + ext.y, c.z + ext.z),
                };
                foreach (var cw in corners)
                {
                    Vector3 p = root.InverseTransformPoint(cw);
                    if (!hasPoint) { localBounds = new Bounds(p, Vector3.zero); hasPoint = true; }
                    else localBounds.Encapsulate(p);
                }
            }
            return localBounds;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var segments = folder.Split('/');
            string cur = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = cur + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, segments[i]);
                cur = next;
            }
        }

        private static string SanitizeName(string name)
        {
            var s = name.Trim();
            foreach (char c in Path.GetInvalidFileNameChars())
                s = s.Replace(c.ToString(), "_");
            return s.Replace(' ', '_');
        }

        // ---------- C) (opt) 嘗試更新 BuildingCatalogSO ----------
        private static void TryUpdateCatalog(string prefabPath, BoxCollider bc)
        {
            if (bc == null) return;
            var guids = AssetDatabase.FindAssets("t:ScriptableObject BuildingCatalog");
            if (guids == null || guids.Length == 0)
                guids = AssetDatabase.FindAssets("t:BuildingCatalogSO");
            if (guids == null || guids.Length != 1)
            {
                // 僅在恰好一份時更新，避免誤寫
                return;
            }

            string catPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(catPath);
            if (obj == null) return;

            var so = new SerializedObject(obj);
            var entries = so.FindProperty("entries");
            if (entries == null || !entries.isArray) return;

            // 建 id / prefab / half extents
            string id = Path.GetFileNameWithoutExtension(prefabPath).ToLowerInvariant();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Vector3 half = bc.size * 0.5f;

            // 嘗試找同名 id
            int found = -1;
            for (int i = 0; i < entries.arraySize; i++)
            {
                var elem = entries.GetArrayElementAtIndex(i);
                var idProp = elem.FindPropertyRelative("id");
                if (idProp != null && idProp.stringValue == id)
                {
                    found = i; break;
                }
            }
            int idx = found >= 0 ? found : entries.arraySize;
            if (found < 0) entries.InsertArrayElementAtIndex(idx);

            var e = entries.GetArrayElementAtIndex(idx);
            var idP = e.FindPropertyRelative("id");
            var prefabP = e.FindPropertyRelative("prefab");
            var fpP = e.FindPropertyRelative("footprintHalfExtents");
            if (idP != null) idP.stringValue = id;
            if (prefabP != null) prefabP.objectReferenceValue = prefab;
            if (fpP != null) fpP.vector3Value = half;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif