namespace DeepAbyssHive.SpatialIndex.Data
{
    /// <summary>
    /// 空間索引效能統計（唯一來源）
    /// </summary>
    public class SpatialIndexPerformanceStats__TEMP
    {
        /// <summary>累計查詢總數</summary>
        public long TotalQueries { get; set; }

        /// <summary>當前幀查詢次數</summary>
        public int FrameQueries { get; set; }

        /// <summary>索引內對象數</summary>
        public int ObjectCount { get; set; }

        /// <summary>待處理操作數</summary>
        public int PendingOperations { get; set; }

        /// <summary>重置幀統計</summary>
        public void ResetFrameStats() => FrameQueries = 0;

        /// <summary>重置所有統計</summary>
        public void Reset()
        {
            TotalQueries = 0;
            FrameQueries = 0;
            ObjectCount = 0;
            PendingOperations = 0;
        }
    }
}