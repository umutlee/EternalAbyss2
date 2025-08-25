using DeepAbyssHive.Buildings.Config;

namespace DeepAbyssHive.Buildings.Compat
{
    public static class BuildingConfigExtensions
    {
        public static string ConfigName(this BuildingConfigSO so) => so != null ? so.name : null;
    }
}
