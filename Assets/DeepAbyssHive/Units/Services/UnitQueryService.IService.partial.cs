using DeepAbyssHive.Core.Services;

namespace DeepAbyssHive.Units.Services
{
    /// <summary>
    /// UnitQueryService 的 IService 實作
    /// </summary>
    public partial class UnitQueryService : IService
    {
        /// <summary>
        /// 服務名稱
        /// </summary>
        public string ServiceName { get; } = nameof(UnitQueryService);
        
        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// 初始化服務
        /// </summary>
        public void Initialize()
        {
            IsInitialized = true;
        }

        /// <summary>
        /// 清理服務
        /// </summary>
        public void Cleanup()
        {
            IsInitialized = false;
        }
    }
}