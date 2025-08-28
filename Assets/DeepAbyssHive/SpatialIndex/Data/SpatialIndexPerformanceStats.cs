namespace DeepAbyssHive.SpatialIndex.Data
{
    /// <summary>空間索引效能統計（唯一來源）</summary>
    public class SpatialIndexPerformanceStats
    {
        public long TotalQueries { get; set; }
        /// <summary>平均查詢耗時（秒）</summary>
        public float AverageQueryTime { get; set; }
        public int FrameQueries { get; set; }
        public int ObjectCount { get; set; }
        public int PendingOperations { get; set; }

        public void ResetFrameStats() => FrameQueries = 0;

        public void Reset()
        {
            TotalQueries = 0;
            AverageQueryTime = 0f;
            FrameQueries = 0;
            ObjectCount = 0;
            PendingOperations = 0;
        }
    }
}
