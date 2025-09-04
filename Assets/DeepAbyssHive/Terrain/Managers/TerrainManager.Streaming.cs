using UnityEngine;
using System.Collections.Generic;

namespace DeepAbyssHive.Terrain.Managers
{
    /// <summary>
    /// M1-T02：Chunk Streamer 骨架
    /// - 追蹤目標位置決定載入中心
    /// - 更新節流（_streamUpdateInterval）
    /// - 區塊遲滯（跨 _streamHysteresisChunks 個 chunk 才換中心）
    /// - LOD 門檻計算（先印 Dev Log，T03 再實際套用）
    /// </summary>
    public partial class TerrainManager
    {
        // 每個 Chunk 的當前 LOD 快取
        private readonly Dictionary<Vector2Int, int> _chunkLOD = new Dictionary<Vector2Int, int>();
        private const float LOD_HYSTERESIS_CHUNKS = 1f; // 1 個 chunk 寬作為遲滯

        private void ApplyChunkLOD(Vector2Int coord, GameObject chunkGO, Vector3 streamCenterWorld)
        {
            if (!chunkGO) return;
            Vector3 chunkCenter = ChunkToWorldPosition(coord)
                                  + new Vector3(ConfigChunkSize * ConfigTileSize * 0.5f, 0f,
                                                ConfigChunkSize * ConfigTileSize * 0.5f);
            float dist = Vector3.Distance(streamCenterWorld, chunkCenter);
            int desired = GetTargetLOD(dist);

            int prev;
            _chunkLOD.TryGetValue(coord, out prev);

            // 遲滯：若目前為 0，離開 near+margin 才升到 1；若目前為 1，進入 near-margin 才降到 0
            float chunkWorld = ConfigChunkSize * ConfigTileSize;
            float near = Mathf.Max(1f, ViewDistance * 0.6f);
            float margin = LOD_HYSTERESIS_CHUNKS * chunkWorld;
            if (prev == 0 && dist <= near + margin) desired = 0;
            if (prev == 1 && dist >= near - margin) desired = 1;

            // 簡化為兩級 LOD：近距離用 0，遠距離用 1
            desired = Mathf.Clamp(desired, 0, 1);

            if (desired == prev) return;

            var runtime = chunkGO.GetComponent<DeepAbyssHive.Terrain.Chunks.TerrainChunkRuntime>();
            if (runtime)
            {
                runtime.SetLOD(desired);
                _chunkLOD[coord] = desired;
                Debug.Log($"[STREAM] LOD change {coord} => {desired}");
            }
            else
            {
                _chunkLOD[coord] = desired;
            }
        }

        [Header("Streaming")]
        [SerializeField] private float _streamUpdateInterval = 0.25f;
        [SerializeField] private int   _streamHysteresisChunks = 1;
        [SerializeField] private Transform _streamTarget; // 可外部指定；未指定則用 Camera.main

        private float _streamTimer;
        private Vector2Int _lastStreamCenterChunk;
        private bool _hasStreamCenter = false;

        /// <summary>外部可指定串流追蹤目標</summary>
        public void SetStreamingTarget(Transform t) => _streamTarget = t;

        /// <summary>目前串流中心（唯讀，供 Debug 顯示）</summary>
        public Vector2Int CurrentStreamCenterChunk => _lastStreamCenterChunk;
        public float      StreamUpdateInterval     => _streamUpdateInterval;
        public int        StreamHysteresisChunks   => _streamHysteresisChunks;

        /// <summary>由 TickUpdate(dt) 週期性呼叫；負責中心切換與 LOD 門檻計算</summary>
        private void TickStreaming(float dt)
        {
            if (!_isInitialized) return;

            _streamTimer += dt;
            if (_streamTimer < _streamUpdateInterval)
                return;

            _streamTimer = 0f;

            Vector3 worldPos = GetStreamingWorldPos();
            var centerChunk = WorldToChunkCoord(worldPos);

            // 遲滯：至少跨指定 chunk 數才切換中心
            if (!_hasStreamCenter ||
                Mathf.Abs(centerChunk.x - _lastStreamCenterChunk.x) > _streamHysteresisChunks ||
                Mathf.Abs(centerChunk.y - _lastStreamCenterChunk.y) > _streamHysteresisChunks)
            {
                _lastStreamCenterChunk = centerChunk;
                _hasStreamCenter = true;

                // 使用既有 API：讓載入/卸載流程走原本的 LoadTerrain
                LoadTerrain(worldPos);

                // LOD 門檻計算（暫時只印 log）
                float vd = Mathf.Max(1f, ViewDistance);
                int levels = Mathf.Max(1, MaxLODLevels);
                float perBand = vd / levels;

                Debug.Log($"[STREAM] center={centerChunk}, interval={_streamUpdateInterval}s, hysteresis={_streamHysteresisChunks}ch, LODbands≈{perBand:0.##} (levels={levels})");
            }

            // —— 對已載入的 chunk 依距離套 LOD ——   
            var centerWorld = ChunkToWorldPosition(_lastStreamCenterChunk);
            foreach (var kv in _terrainChunks)
            {
                var coord = kv.Key;
                GameObject go = null;
                // 嘗試從 ITerrainChunk 還原 GameObject
                var asMb = kv.Value as MonoBehaviour;
                if (asMb) go = asMb.gameObject;
                if (!go)
                {
                    var found = GameObject.Find($"TerrainChunk_{coord.x}_{coord.y}");
                    if (found) go = found;
                }
                if (go) ApplyChunkLOD(coord, go, centerWorld);
            }
        }

        private Vector3 GetStreamingWorldPos()
        {
            if (_streamTarget) return _streamTarget.position;

            var cam = Camera.main;
            return cam ? cam.transform.position : Vector3.zero;
        }

        /// <summary>根據距離估算目標 LOD（0=最高細節）。先提供給 T03 使用。</summary>
        private int GetTargetLOD(float distance)
        {
            int levels = Mathf.Max(1, MaxLODLevels);
            float vd = Mathf.Max(1f, ViewDistance);
            float perBand = vd / levels;
            int lod = Mathf.Clamp(Mathf.FloorToInt(distance / perBand), 0, levels - 1);
            return lod;
        }
    }
}