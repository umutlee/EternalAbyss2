using UnityEngine;
using DeepAbyssHive.Creep.Enums;

namespace DeepAbyssHive.Creep.Interfaces
{
    public interface ICreepSource
    {
        int Id { get; }
        CreepSourceType Type { get; }
        Vector3 Position { get; }
    }
}