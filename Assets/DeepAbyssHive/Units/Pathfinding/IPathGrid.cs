using UnityEngine;

namespace DeepAbyssHive.Units.Pathfinding
{
    /// <summary>
    /// 導航格網介面：A* 將依此取樣「可走性」與「成本」。
    /// </summary>
    public interface IPathGrid
    {
        // 參考格網設定
        float CellSize { get; }
        Vector3 Origin { get; }

        // 世界座標 ↔ 格點換算
        bool TryWorldToCell(Vector3 world, out int x, out int y);
        Vector3 CellCenter(int x, int y);

        // 取樣
        bool IsWalkable(int x, int y);
        float Cost(int x, int y);
    }
}