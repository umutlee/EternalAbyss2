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