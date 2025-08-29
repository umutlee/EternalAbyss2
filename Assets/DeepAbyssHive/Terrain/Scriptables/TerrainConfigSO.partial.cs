using UnityEngine;
using DeepAbyssHive.Terrain.Enums;

namespace DeepAbyssHive.Terrain.Scriptables
{
    /// <summary>
    /// TerrainConfigSO 的相容性擴充
    /// </summary>
    public partial class TerrainConfigSO
    {
        [Header("地形修改設定")]
        [SerializeField]
        private float _defaultBrushSize = 10.0f;
        
        [SerializeField]
        private float _defaultBrushStrength = 0.5f;
        
        [SerializeField]
        private TerrainModificationType _defaultModificationType = TerrainModificationType.Raise;
        
        [SerializeField]
        private AnimationCurve _defaultFalloffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        
        [Header("地形生成設定")]
        [SerializeField]
        private int _heightmapResolution = 513;
        
        [SerializeField]
        private int _alphamapResolution = 1024;
        
        [SerializeField]
        private int _baseMapResolution = 1024;
        
        [SerializeField]
        private float _pixelError = 5.0f;
        
        [SerializeField]
        private float _baseMapDistance = 1000.0f;
        
        /// <summary>
        /// 預設筆刷大小
        /// </summary>
        public float DefaultBrushSize => _defaultBrushSize;
        
        /// <summary>
        /// 預設筆刷強度
        /// </summary>
        public float DefaultBrushStrength => _defaultBrushStrength;
        
        /// <summary>
        /// 預設修改類型
        /// </summary>
        public TerrainModificationType DefaultModificationType => _defaultModificationType;
        
        /// <summary>
        /// 預設衰減曲線
        /// </summary>
        public AnimationCurve DefaultFalloffCurve => _defaultFalloffCurve;
        
        /// <summary>
        /// 高度圖解析度
        /// </summary>
        public int HeightmapResolution => _heightmapResolution;
        
        /// <summary>
        /// Alpha 圖解析度
        /// </summary>
        public int AlphamapResolution => _alphamapResolution;
        
        /// <summary>
        /// 基礎圖解析度
        /// </summary>
        public int BaseMapResolution => _baseMapResolution;
        
        /// <summary>
        /// 像素誤差
        /// </summary>
        public float PixelError => _pixelError;
        
        /// <summary>
        /// 基礎圖距離
        /// </summary>
        public float BaseMapDistance => _baseMapDistance;
    }
}