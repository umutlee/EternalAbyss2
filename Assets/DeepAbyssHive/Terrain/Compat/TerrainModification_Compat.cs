using UnityEngine;
using DeepAbyssHive.Terrain.Data;
using DeepAbyssHive.Terrain.Enums;

namespace DeepAbyssHive.Terrain.Compat
{
    /// <summary>
    /// TerrainModification 兼容層
    /// 補齊舊代碼使用到的屬性與 TerrainModificationType 枚舉值
    /// </summary>
    public partial struct TerrainModification
    {
        #region 兼容屬性
        
        /// <summary>
        /// 兼容：修改類型（舊版本使用）
        /// </summary>
        public TerrainModificationType ModificationType
        {
            get => (TerrainModificationType)Type;
            set => Type = (int)value;
        }
        
        /// <summary>
        /// 兼容：修改強度（舊版本使用）
        /// </summary>
        public float Intensity
        {
            get => Strength;
            set => Strength = value;
        }
        
        /// <summary>
        /// 兼容：修改半徑（舊版本使用）
        /// </summary>
        public float Radius
        {
            get => Range;
            set => Range = value;
        }
        
        /// <summary>
        /// 兼容：是否立即應用（舊版本使用）
        /// </summary>
        public bool ApplyImmediately { get; set; }
        
        /// <summary>
        /// 兼容：修改優先級（舊版本使用）
        /// </summary>
        public int Priority { get; set; }
        
        /// <summary>
        /// 兼容：修改持續時間（舊版本使用）
        /// </summary>
        public float Duration { get; set; }
        
        #endregion
        
        #region 兼容建構子
        
        /// <summary>
        /// 兼容建構子：使用舊版本參數
        /// </summary>
        /// <param name="modificationType">修改類型</param>
        /// <param name="position">位置</param>
        /// <param name="intensity">強度</param>
        /// <param name="radius">半徑</param>
        public TerrainModification(TerrainModificationType modificationType, Vector3 position, float intensity, float radius)
        {
            Type = (int)modificationType;
            Position = position;
            Strength = intensity;
            Range = radius;
            ApplyImmediately = true;
            Priority = 0;
            Duration = 0f;
        }
        
        /// <summary>
        /// 兼容建構子：使用舊版本參數（完整版本）
        /// </summary>
        /// <param name="modificationType">修改類型</param>
        /// <param name="position">位置</param>
        /// <param name="intensity">強度</param>
        /// <param name="radius">半徑</param>
        /// <param name="applyImmediately">是否立即應用</param>
        /// <param name="priority">優先級</param>
        /// <param name="duration">持續時間</param>
        public TerrainModification(TerrainModificationType modificationType, Vector3 position, float intensity, float radius, 
            bool applyImmediately, int priority = 0, float duration = 0f)
        {
            Type = (int)modificationType;
            Position = position;
            Strength = intensity;
            Range = radius;
            ApplyImmediately = applyImmediately;
            Priority = priority;
            Duration = duration;
        }
        
        #endregion
        
        #region 兼容方法
        
        /// <summary>
        /// 兼容方法：設置修改類型
        /// </summary>
        /// <param name="type">修改類型</param>
        public void SetModificationType(TerrainModificationType type)
        {
            ModificationType = type;
        }
        
        /// <summary>
        /// 兼容方法：獲取修改類型
        /// </summary>
        /// <returns>修改類型</returns>
        public TerrainModificationType GetModificationType()
        {
            return ModificationType;
        }
        
        /// <summary>
        /// 兼容方法：設置修改參數
        /// </summary>
        /// <param name="intensity">強度</param>
        /// <param name="radius">半徑</param>
        public void SetModificationParameters(float intensity, float radius)
        {
            Intensity = intensity;
            Radius = radius;
        }
        
        /// <summary>
        /// 兼容方法：檢查是否為有效修改
        /// </summary>
        /// <returns>是否有效</returns>
        public bool IsValidModification()
        {
            return Strength > 0f && Range > 0f;
        }
        
        #endregion
    }
    
    /// <summary>
    /// 地形修改類型枚舉（兼容舊版本）
    /// </summary>
    public enum TerrainModificationType
    {
        /// <summary>
        /// 無修改
        /// </summary>
        None = 0,
        
        /// <summary>
        /// 提升地形
        /// </summary>
        Raise = 1,
        
        /// <summary>
        /// 降低地形
        /// </summary>
        Lower = 2,
        
        /// <summary>
        /// 平整地形
        /// </summary>
        Flatten = 3,
        
        /// <summary>
        /// 平滑地形
        /// </summary>
        Smooth = 4,
        
        /// <summary>
        /// 粗糙化地形
        /// </summary>
        Roughen = 5,
        
        /// <summary>
        /// 挖掘
        /// </summary>
        Dig = 6,
        
        /// <summary>
        /// 填充
        /// </summary>
        Fill = 7,
        
        /// <summary>
        /// 創建坑洞
        /// </summary>
        CreateCrater = 8,
        
        /// <summary>
        /// 創建山丘
        /// </summary>
        CreateHill = 9,
        
        /// <summary>
        /// 侵蝕效果
        /// </summary>
        Erode = 10,
        
        /// <summary>
        /// 沉積效果
        /// </summary>
        Deposit = 11,
        
        /// <summary>
        /// 自定義修改
        /// </summary>
        Custom = 99
    }
}