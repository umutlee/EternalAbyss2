using DeepAbyssHive.Buildings.Interfaces;
using DeepAbyssHive.Buildings.Services;

namespace DeepAbyssHive.Buildings.Extensions
{
    public static class BuildingServiceExtensions
    {
        public static void Update(this DeepAbyssHive.Buildings.Services.IBuildingQueryService _, float deltaTime) { }
        public static void Update(this DeepAbyssHive.Buildings.Services.IBuildingConstructionService _, float deltaTime) { }
        public static void Update(this DeepAbyssHive.Buildings.Services.IResearchService _, float deltaTime) { }

        public static void SetPaused(this DeepAbyssHive.Buildings.Services.IBuildingConstructionService _, bool paused) { }
        public static void SetPaused(this DeepAbyssHive.Buildings.Services.IResearchService _, bool paused) { }
    }
}