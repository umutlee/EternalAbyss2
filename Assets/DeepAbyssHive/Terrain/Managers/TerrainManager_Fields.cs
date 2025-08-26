using UnityEngine;

namespace DeepAbyssHive.Terrain.Managers
{
    public partial class TerrainManager : MonoBehaviour
    {
        [SerializeField] private int _chunkSize = 16;
        [SerializeField] private float _tileSize = 1f;
        [SerializeField] private int _loadRadius = 4;

        [SerializeField] private float _noiseScale = 0.05f;
        [SerializeField] private float _heightScale = 10f;
        [SerializeField] private int _seed = 1337;
        [SerializeField] private int _maxModificationsPerFrame = 64;
    }
}