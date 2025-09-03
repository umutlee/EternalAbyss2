using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Terrain.Enums;
using DeepAbyssHive.Terrain.Interfaces;

namespace DeepAbyssHive.Terrain.Chunks
{
    /// <summary>
    /// Runtime 版地形塊：把 chunk 生成 Mesh 與 MeshCollider。
    /// 註：目前高度來自 Perlin 噪聲（seed/noiseScale/heightScale），MVP 不使用 TerrainType[,]。
    /// </summary>
    public class TerrainChunkRuntime : MonoBehaviour, ITerrainChunk
    {
        // --- ITerrainChunk 所需對外 ---
        public Vector2Int Coordinates { get; private set; }
        public bool IsActive { get; private set; } = true;

        // --- 生成所需參數（由 TerrainManager 傳入） ---
        private int   _chunkSize;
        private float _tileSize;
        private int   _seed;
        private float _noiseScale;
        private float _heightScale;

        // --- 組件快取 ---
        private MeshFilter   _mf;
        private MeshRenderer _mr;
        private MeshCollider _mc;

        // --- 建立 / 初始化 ---
        public void Initialize(Vector2Int coord, int chunkSize, float tileSize, int seed, float noiseScale, float heightScale)
        {
            Coordinates = coord;
            _chunkSize = Mathf.Max(1, chunkSize);
            _tileSize  = Mathf.Max(0.0001f, tileSize);
            _seed      = seed;
            _noiseScale = Mathf.Max(0.0001f, noiseScale);
            _heightScale = heightScale;

            _mf = gameObject.GetComponent<MeshFilter>();
            if (_mf == null) _mf = gameObject.AddComponent<MeshFilter>();

            _mr = gameObject.GetComponent<MeshRenderer>();
            if (_mr == null) _mr = gameObject.AddComponent<MeshRenderer>();

            _mc = gameObject.GetComponent<MeshCollider>();
            if (_mc == null) _mc = gameObject.AddComponent<MeshCollider>();

            if (_mr.sharedMaterial == null)
            {
                // 使用內建 Standard shader 作為 fallback，美術日後可替換
                var shader = Shader.Find("Standard");
                var mat = new Material(shader);
                // 中灰、無金屬、低光滑，避免畫面過曝一片白
                mat.color = new Color(0.55f, 0.55f, 0.55f, 1f);
                if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic", 0f);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.2f);
                _mr.sharedMaterial = mat;
            }

            // 陰影設定：一般預設即可，顯式開啟以防專案設定差異
            _mr.receiveShadows = true;
            _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            
            // 指定到 "Terrain" 層（若該層存在）
            int terrainLayer = LayerMask.NameToLayer("Terrain");
            if (terrainLayer != -1) gameObject.layer = terrainLayer;
        }

        // --- ITerrainChunk ---
        public void UpdateTerrain(float deltaTime)
        {
            // 目前不需要逐幀行為；預留
        }

        public void UpdateTerrainData(TerrainType[,] terrainData)
        {
            // 以 Perlin 噪聲產生高度（MVP）
            BuildMeshFromNoise();
        }

        public void Cleanup()
        {
            IsActive = false;

            // 清理 Mesh 與 GameObject
            if (_mf != null && _mf.sharedMesh != null)
            {
                var m = _mf.sharedMesh;
                _mf.sharedMesh = null;
                if (Application.isPlaying) Destroy(m); else DestroyImmediate(m);
            }

            if (gameObject != null)
            {
                if (Application.isPlaying) Destroy(gameObject);
                else DestroyImmediate(gameObject);
            }
        }

        // --- Mesh 生成 ---
        private void BuildMeshFromNoise()
        {
            int vertsPerSide = _chunkSize + 1;
            int vertCount = vertsPerSide * vertsPerSide;

            var vertices = new Vector3[vertCount];
            var uvs      = new Vector2[vertCount];
            var tris     = new int[_chunkSize * _chunkSize * 6];

            // 以「世界格坐標」進行 Perlin：避免每個 chunk 都相同
            int baseX = Coordinates.x * _chunkSize;
            int baseY = Coordinates.y * _chunkSize;
            float noiseOffset = _seed * 0.001f; // seed 做偏移

            // 頂點
            int vi = 0;
            for (int y = 0; y < vertsPerSide; y++)
            {
                for (int x = 0; x < vertsPerSide; x++, vi++)
                {
                    float worldIx = (baseX + x) * _noiseScale + noiseOffset;
                    float worldIy = (baseY + y) * _noiseScale + noiseOffset;
                    float h = Mathf.PerlinNoise(worldIx, worldIy) * _heightScale;

                    vertices[vi] = new Vector3(x * _tileSize, h, y * _tileSize);
                    uvs[vi] = new Vector2((float)x / _chunkSize, (float)y / _chunkSize);
                }
            }

            // 三角形
            int ti = 0;
            for (int y = 0; y < _chunkSize; y++)
            {
                for (int x = 0; x < _chunkSize; x++)
                {
                    int i00 = y * vertsPerSide + x;
                    int i10 = i00 + 1;
                    int i01 = i00 + vertsPerSide;
                    int i11 = i01 + 1;

                    // 兩個三角形（順時針）
                    tris[ti++] = i00; tris[ti++] = i11; tris[ti++] = i10;
                    tris[ti++] = i00; tris[ti++] = i01; tris[ti++] = i11;
                }
            }

            var mesh = new Mesh();
            mesh.indexFormat = (vertCount > 65000) ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.name = $"ChunkMesh_{Coordinates.x}_{Coordinates.y}";

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = tris;

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            _mf.sharedMesh = mesh;
            _mc.sharedMesh = mesh;
        }
    
    // --- ITerrainChunk 介面的其他必要實作 ---
        public Bounds Bounds 
        { 
            get 
            {
                if (_mf != null && _mf.sharedMesh != null)
                    return _mf.sharedMesh.bounds;
                return new Bounds(transform.position, Vector3.one * _chunkSize * _tileSize);
            }
        }

        public TerrainType[,] TerrainTypes { get; private set; }
        public float[,] HeightMap { get; private set; }
        public bool IsLoaded => IsActive;
        public int CurrentLODLevel { get; private set; } = 0;

        public void Load()
        {
            IsActive = true;
        }

        public void Unload()
        {
            Cleanup();
        }

        public void ModifyHeight(Vector2Int localPosition, float height)
        {
            // 預留：高度修改功能
        }

        public void SetTerrainType(Vector2Int localPosition, TerrainType type)
        {
            // 預留：地形類型設定功能
        }

        public void SetLODLevel(int level)
        {
            CurrentLODLevel = level;
            // 預留：LOD 切換功能
        }

        public float GetCreepDensity(Vector2Int localPosition)
        {
            // 預留：菌毯密度查詢
            return 0f;
        }

        public void SetCreepDensity(Vector2Int localPosition, float density, int ownerId)
        {
            // 預留：菌毯密度設定
        }
    }
}