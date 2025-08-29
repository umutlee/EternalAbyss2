using DeepAbyssHive.Core.Services;

namespace DeepAbyssHive.Buildings.Services
{
    /// <summary>
    /// 與 BuildingConstructionService 同命名空間/同目錄，確保同一 asmdef
    /// IsCommandAvailable 屬性已在主檔案中定義
    /// </summary>
    public partial class BuildingConstructionService : ICommandService
    {
        // IsCommandAvailable 屬性已在主檔案中定義，這裡不需要重複
    }
}