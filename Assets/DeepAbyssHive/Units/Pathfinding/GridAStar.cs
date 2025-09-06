using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepAbyssHive.Units.Pathfinding
{
    /// <summary>
    /// 輕量 A*：與 IPathGrid 搭配使用。為了最小依賴，回傳 bool + out path，失敗原因以 Debug.Log 提示。
    /// </summary>
    public static class GridAStar
    {
        private struct Cell : IEquatable<Cell>
        {
            public int x, y;
            public Cell(int x, int y) { this.x = x; this.y = y; }
            public bool Equals(Cell other) => x == other.x && y == other.y;
            public override bool Equals(object o) => o is Cell c && Equals(c);
            public override int GetHashCode() => (x * 73856093) ^ (y * 19349663);
        }

        /// <summary>
        /// 主要入口：成功回 true 並給出 world 路徑。
        /// </summary>
        public static bool TryFindPath(IPathGrid grid, Vector3 startWorld, Vector3 goalWorld,
            out List<Vector3> path, bool allowDiagonal = true, int maxExpand = 4096)
        {
            path = null;
            if (!grid.TryWorldToCell(startWorld, out int sx, out int sy) ||
                !grid.TryWorldToCell(goalWorld, out int gx, out int gy))
            {
                Debug.LogWarning("[Path] WorldToCell failed (out-of-bounds?)");
                return false;
            }
            var start = new Cell(sx, sy);
            var goal  = new Cell(gx, gy);

            if (!grid.IsWalkable(start.x, start.y))
            {
                Debug.LogWarning("[Path] Start not walkable");
                return false;
            }
            if (!grid.IsWalkable(goal.x, goal.y))
            {
                Debug.LogWarning("[Path] Goal not walkable");
                return false;
            }

            var open = new MinHeap();                          // f-score 最小堆
            var came = new Dictionary<Cell, Cell>(1024);       // 前驅
            var g    = new Dictionary<Cell, float>(1024) { [start] = 0f };
            var f    = new Dictionary<Cell, float>(1024) { [start] = Heuristic(start, goal, allowDiagonal) };
            var inOpen = new HashSet<Cell> { start };
            open.Push(start, f[start]);

            int expanded = 0;
            while (open.Count > 0)
            {
                var current = open.Pop();
                inOpen.Remove(current);

                if (current.Equals(goal))
                {
                    path = Reconstruct(came, current, grid);
                    return true;
                }

                if (++expanded > maxExpand)
                {
                    Debug.LogWarning($"[Path] Abort: expand>{maxExpand}");
                    return false;
                }

                foreach (var nb in Neighbors(current, allowDiagonal))
                {
                    // 防角切：對角移動時，兩個正交鄰格需可走
                    if (!grid.IsWalkable(nb.x, nb.y)) continue;
                    if (allowDiagonal && IsDiagonalStep(current, nb))
                    {
                        var a = new Cell(nb.x, current.y);
                        var b = new Cell(current.x, nb.y);
                        if (!grid.IsWalkable(a.x, a.y) || !grid.IsWalkable(b.x, b.y)) continue;
                    }

                    float step = StepCost(current, nb, grid, allowDiagonal);
                    float tentative = g[current] + step;
                    if (!g.TryGetValue(nb, out float old) || tentative < old)
                    {
                        came[nb] = current;
                        g[nb] = tentative;
                        float h = Heuristic(nb, goal, allowDiagonal);
                        float fs = tentative + h;
                        f[nb] = fs;
                        if (!inOpen.Contains(nb))
                        {
                            inOpen.Add(nb);
                            open.Push(nb, fs);
                        }
                        else
                        {
                            open.DecreaseKey(nb, fs);
                        }
                    }
                }
            }
            Debug.LogWarning("[Path] Open exhausted, no path");
            return false;
        }

        // --- Heuristic / Step cost ---

        private static float Heuristic(Cell a, Cell b, bool diag)
        {
            int dx = Math.Abs(a.x - b.x);
            int dy = Math.Abs(a.y - b.y);
            if (!diag) return dx + dy; // Manhattan
            int min = Math.Min(dx, dy), max = Math.Max(dx, dy);
            // Octile：sqrt2*min + (max - min)
            return 1.41421356f * min + (max - min);
        }

        private static float StepCost(Cell a, Cell b, IPathGrid grid, bool diag)
        {
            float baseCost = (a.x == b.x || a.y == b.y) ? 1f : 1.41421356f;
            // 以目標格的 Cost 作為局部代價（可讓 creep/地形影響偏好）
            float tileCost = grid.Cost(b.x, b.y);
            return baseCost * tileCost;
        }

        // --- Neighbors ---

        private static IEnumerable<Cell> Neighbors(Cell c, bool diag)
        {
            yield return new Cell(c.x + 1, c.y);
            yield return new Cell(c.x - 1, c.y);
            yield return new Cell(c.x, c.y + 1);
            yield return new Cell(c.x, c.y - 1);
            if (diag)
            {
                yield return new Cell(c.x + 1, c.y + 1);
                yield return new Cell(c.x - 1, c.y + 1);
                yield return new Cell(c.x + 1, c.y - 1);
                yield return new Cell(c.x - 1, c.y - 1);
            }
        }

        private static bool IsDiagonalStep(Cell a, Cell b) => a.x != b.x && a.y != b.y;

        // --- Path reconstruction ---

        private static List<Vector3> Reconstruct(Dictionary<Cell, Cell> came, Cell current, IPathGrid grid)
        {
            var rev = new List<Vector3>(64);
            rev.Add(grid.CellCenter(current.x, current.y));
            while (came.TryGetValue(current, out var prev))
            {
                current = prev;
                rev.Add(grid.CellCenter(current.x, current.y));
            }
            rev.Reverse();
            return rev;
        }

        // --- Minimal binary heap keyed by Cell ---

        private class MinHeap
        {
            private readonly List<Cell> _nodes = new List<Cell>(1024);
            private readonly List<float> _prio = new List<float>(1024);
            private readonly Dictionary<Cell, int> _index = new Dictionary<Cell, int>(1024);

            public int Count => _nodes.Count;

            public void Push(Cell c, float p)
            {
                _nodes.Add(c); _prio.Add(p);
                int i = _nodes.Count - 1;
                _index[c] = i;
                SiftUp(i);
            }

            public Cell Pop()
            {
                int last = _nodes.Count - 1;
                var top = _nodes[0];
                Swap(0, last);
                _nodes.RemoveAt(last);
                _prio.RemoveAt(last);
                _index.Remove(top);
                if (_nodes.Count > 0) SiftDown(0);
                return top;
            }

            public void DecreaseKey(Cell c, float newP)
            {
                if (!_index.TryGetValue(c, out int i)) return;
                if (newP >= _prio[i]) return;
                _prio[i] = newP;
                SiftUp(i);
            }

            private void SiftUp(int i)
            {
                while (i > 0)
                {
                    int p = (i - 1) >> 1;
                    if (_prio[p] <= _prio[i]) break;
                    Swap(i, p);
                    i = p;
                }
            }

            private void SiftDown(int i)
            {
                int n = _nodes.Count;
                while (true)
                {
                    int l = i * 2 + 1, r = l + 1, s = i;
                    if (l < n && _prio[l] < _prio[s]) s = l;
                    if (r < n && _prio[r] < _prio[s]) s = r;
                    if (s == i) break;
                    Swap(i, s);
                    i = s;
                }
            }

            private void Swap(int a, int b)
            {
                if (a == b) return;
                (_nodes[a], _nodes[b]) = (_nodes[b], _nodes[a]);
                (_prio[a], _prio[b])   = (_prio[b], _prio[a]);
                _index[_nodes[a]] = a;
                _index[_nodes[b]] = b;
            }
        }
    }
}