using UnityEngine;
using DeepAbyssHive.Creep.Data;

namespace DeepAbyssHive.Creep.Services
{
    public static class CreepSourceServiceExtensions
    {
        public static void AddSource(this ICreepSourceService s, int id, CreepSourceType type, Vector3 pos) { /* no-op or route if已支援 */ }
        public static void RemoveSource(this ICreepSourceService s, int id) { }
        public static CreepSource GetSource(this ICreepSourceService s, int id) => default;
        public static void UpdateSource(this ICreepSourceService s, int id, CreepSourceType type, Vector3 pos) { }
    }
}