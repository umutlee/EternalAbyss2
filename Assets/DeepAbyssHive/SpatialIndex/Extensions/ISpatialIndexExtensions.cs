using DeepAbyssHive.SpatialIndex.Interfaces;

namespace DeepAbyssHive.SpatialIndex.Extensions
{
    public static class ISpatialIndexExtensions
    {
        /// <summary>
        /// 兼容用的 no-op。後續若需要真正最佳化，可在各實作自行提供。
        /// </summary>
        public static void Optimize(this ISpatialIndex index) { /* no-op */ }
    }
}