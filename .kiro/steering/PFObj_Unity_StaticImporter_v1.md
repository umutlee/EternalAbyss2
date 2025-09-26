description: Permafrost .pfobj → Unity 匯入器（靜態網格；自動產出 Prefab + Mesh + Material）
globs: ["Assets/**"]
alwaysApply: true

# PLAN
- 說明：.pfobj 是 Permafrost Engine 的 **純文字模型格式**，記錄 v/vt/vn/權重/材質/關節等資訊（類似 OBJ，但以頂點連續三筆=一個三角形）。Unity 無法原生匯入。
- 目的：提供一個 **ScriptedImporter**，讓 Unity 直接匯入 `.pfobj` 為 Mesh + Prefab（僅支援「靜態網格」：v/vt/vn；忽略骨架、動畫）。
- 範圍：新增 1 檔 `Assets/Permafrost/PFObjImporter.cs`；不改既有代碼。≤200 行。
- DoD：把任何 `*.pfobj` 拖進專案，會生成 Mesh 子資產與一個帶 MeshRenderer 的 Prefab；若同資料夾有 `texture XXX` 指到的貼圖，會自動掛上。

# CHANGES
- PATCH: Assets/Permafrost/PFObjImporter.cs
  --- C# ---
  #if UNITY_EDITOR
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using System.IO;
  using System.Text;
  using UnityEditor;
  using UnityEditor.AssetImporters;
  using UnityEngine;

  // Minimal .pfobj importer: supports v / vt / vn vertex soup (every 3 verts = 1 triangle).
  // Ignores joints/weights/animation; reads a single material texture if present via "texture <file>".
  [ScriptedImporter(1, new[] {"pfobj"})]
  public class PFObjImporter : ScriptedImporter
  {
      public override void OnImportAsset(AssetImportContext ctx)
      {
          var text = File.ReadAllText(ctx.assetPath, Encoding.UTF8);
          var dir  = Path.GetDirectoryName(ctx.assetPath).Replace("\\", "/");
          var name = Path.GetFileNameWithoutExtension(ctx.assetPath);

          var verts = new List<Vector3>();
          var uvs   = new List<Vector2>();
          var nors  = new List<Vector3>();
          string texName = null;

          // The format usually repeats blocks like: v / vt / vn / vw / vm ...
          // We'll parse line-by-line and push v/vt/vn in order.
          var nl = new[] {'\n'};
          var lines = text.Split(nl, StringSplitOptions.RemoveEmptyEntries);
          NumberFormatInfo nfi = CultureInfo.InvariantCulture.NumberFormat;

          foreach (var raw in lines)
          {
              var line = raw.Trim();
              if (line.Length == 0 || line.StartsWith("#")) continue;

              if (line.StartsWith("v "))
              {
                  // v x y z
                  var sp = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                  if (sp.Length >= 4)
                  {
                      float x = float.Parse(sp[1], nfi);
                      float y = float.Parse(sp[2], nfi);
                      float z = float.Parse(sp[3], nfi);
                      verts.Add(new Vector3(x, y, z));
                  }
              }
              else if (line.StartsWith("vt "))
              {
                  var sp = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                  if (sp.Length >= 3)
                  {
                      float u = float.Parse(sp[1], nfi);
                      float v = float.Parse(sp[2], nfi);
                      uvs.Add(new Vector2(u, v));
                  }
              }
              else if (line.StartsWith("vn "))
              {
                  var sp = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                  if (sp.Length >= 4)
                  {
                      float x = float.Parse(sp[1], nfi);
                      float y = float.Parse(sp[2], nfi);
                      float z = float.Parse(sp[3], nfi);
                      nors.Add(new Vector3(x, y, z));
                  }
              }
              else if (line.StartsWith("texture "))
              {
                  // inside material block: texture <file>
                  var sp = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                  if (sp.Length >= 2) texName = sp[1];
              }
          }

          // Align counts: pfobj lists v/vt/vn per-vertex; make sure lengths一致
          int n = verts.Count;
          if (uvs.Count != n) { // fill missing
              while (uvs.Count < n) uvs.Add(Vector2.zero);
          }
          if (nors.Count != n) {
              while (nors.Count < n) nors.Add(Vector3.up);
          }

          // Build triangle indices: every 3 vertices => 1 triangle
          var tris = new int[(n/3)*3];
          for (int i = 0; i < tris.Length; i++) tris[i] = i;

          var mesh = new Mesh();
          mesh.name = name + "_Mesh";
          mesh.SetVertices(verts);
          mesh.SetNormals(nors);
          mesh.SetUVs(0, uvs);
          mesh.SetTriangles(tris, 0, true);
          mesh.RecalculateBounds();
          if (nors.Count == 0) mesh.RecalculateNormals();

          // Try to load texture in same folder
          Material mat = new Material(Shader.Find("Standard"));
          mat.name = name + "_Mat";
          if (!string.IsNullOrEmpty(texName))
          {
              var texPath = dir + "/" + texName;
              var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
              if (tex != null) mat.mainTexture = tex;
          }

          // Create main prefab-like object
          var go = new GameObject(name);
          var mf = go.AddComponent<MeshFilter>();
          var mr = go.AddComponent<MeshRenderer>();
          mf.sharedMesh = mesh;
          mr.sharedMaterial = mat;

          // Register assets
          ctx.AddObjectToAsset("mesh", mesh);
          ctx.AddObjectToAsset("mat",  mat);
          ctx.AddObjectToAsset("main", go);
          ctx.SetMainObject(go);
      }
  }
  #endif
  --- C# ---

# VERIFY (Unity Editor)
- 將 `*.pfobj` 拖進 Project：應自動生成一個同名 Prefab（Main Object）＋ Mesh 子資產＋ Material。若同資料夾已有貼圖（被 `texture <file>` 指到），會自動掛上。
- 在 Scene 置入生成的 Prefab，應可看到網格。

# NOTES
- 目前只支援 **靜態網格**。`vw`（權重）、`j`（joint）與動畫相關欄位被忽略；如需骨架與動畫，建議轉 glTF/FBX（可後續再做擴充）。
- 若網格面向翻轉，可在 MeshRenderer 勾選 **Double Sided Global Illumination** 或在 importer 內部反轉三角形順序（將 `tris` 以 (i+2,i+1,i) 建立）。
- 如果某些檔案的 v/vt/vn 行數不齊，Importer 會自動補 0 向量與單位法線。

# ROLLBACK
- 刪除 `Assets/Permafrost/PFObjImporter.cs` 後重新導入，所有 .pfobj 將回到未識別狀態。
