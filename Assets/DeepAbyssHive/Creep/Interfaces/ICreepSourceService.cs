using UnityEngine;
using DeepAbyssHive.Creep.Data;

namespace DeepAbyssHive.Creep.Interfaces
{
    public interface ICreepSourceService
    {
        // lifecycle
        void Update();

        // CRUD on sources
        void AddSource(int id, Vector3 position, CreepSourceType type);
        void RemoveSource(int id);
        bool TryGetSource(int id, out CreepSource source);
        void UpdateSource(int id, Vector3 position, CreepSourceType type);

        // Optional helpers
        CreepSource GetSource(int id); // may throw if not found; used by callers
    }
}