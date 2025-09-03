#if UNITY_EDITOR
using UnityEngine;

namespace DeepAbyssHive.Terrain.Managers
{
    /// <summary>
    /// Editor Gizmos：顯示中心 Chunk 與視距圈（Scene 視圖）
    /// </summary>
    public partial class TerrainManager
    {
        private void OnDrawGizmosSelected()
        {
            if (!_hasStreamCenter) return;

            // 畫中心 chunk 邊框
            Vector3 chunkOrigin = ChunkToWorldPosition(_lastStreamCenterChunk);
            float size = ChunkSize * Mathf.Max(0.0001f, ConfigTileSize);
            Vector3 center = new Vector3(chunkOrigin.x + size * 0.5f, 0f, chunkOrigin.z + size * 0.5f);

            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawWireCube(center, new Vector3(size, 0.01f, size));

            // 畫視距圈（簡易 Y=0 平面投影）
            Gizmos.DrawWireSphere(center, Mathf.Max(0f, ViewDistance));
        }
    }
}
#endif