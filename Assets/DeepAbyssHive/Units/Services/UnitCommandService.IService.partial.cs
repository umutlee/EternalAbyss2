using DeepAbyssHive.Core.Services;

namespace DeepAbyssHive.Units.Services
{
    public partial class UnitCommandService : IService, ICommandService
    {
        /// <summary>
        /// 若介面只要求 get;，get; set; 也能相容；若介面要求 set;，此版也可編
        /// </summary>
        public string ServiceName { get; set; } = nameof(UnitCommandService);
        
        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            IsInitialized = true;
        }

        public void Cleanup()
        {
            IsInitialized = false;
        }

        // IsCommandAvailable 屬性已在主檔案中定義，這裡不需要重複
    }
}