
namespace DeepAbyssHive.Buildings.Compat
{
    public static class BuildingStateCompat
    {
        public static readonly DeepAbyssHive.Buildings.Enums.BuildingState Active =
            DeepAbyssHive.Buildings.Enums.BuildingState.Built;
        public static bool IsActive(DeepAbyssHive.Buildings.Enums.BuildingState s) =>
            s == DeepAbyssHive.Buildings.Enums.BuildingState.Built;
    }
}
