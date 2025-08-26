using System.Collections.Generic;
using DeepAbyssHive.SpatialIndex.Interfaces;
using UnityEngine;

namespace DeepAbyssHive.SpatialIndex
{
    // ISpatialIndex 接口實現 - 提供正確的方法簽名
    public partial class QuadTreeSpatialIndex
    {
        // 實現 ISpatialIndex.Insert (object版本)
        void ISpatialIndex.Insert(object obj, Vector3 position, Vector3 size)
        {
            // TODO: 轉換 object 到 CreepData 並調用現有 Insert 方法
        }

        // 實現 ISpatialIndex.Update
        void ISpatialIndex.Update(object obj, Vector3 oldPosition, Vector3 newPosition, Vector3 size)
        {
            // TODO: 先移除再插入
        }

        // 實現 ISpatialIndex.Remove (object版本)
        void ISpatialIndex.Remove(object obj, Vector3 position, Vector3 size)
        {
            // TODO: 轉換 object 到 CreepData 並調用現有 Remove 方法
        }

        // 實現 ISpatialIndex.QueryRange
        List<object> ISpatialIndex.QueryRange(Vector3 position, Vector3 size)
        {
            // TODO: 調用現有 Query 方法並轉換結果
            return new List<object>();
        }

        // 實現 ISpatialIndex.QueryNearest
        List<object> ISpatialIndex.QueryNearest(Vector3 position, float maxDistance, int maxResults)
        {
            // TODO: 實現最近鄰查詢
            return new List<object>();
        }

        // 實現 ISpatialIndex.QueryRaycast
        List<object> ISpatialIndex.QueryRaycast(Ray ray, float maxDistance)
        {
            // TODO: 實現射線查詢
            return new List<object>();
        }

        // 實現 ISpatialIndex.Rebuild
        void ISpatialIndex.Rebuild()
        {
            // TODO: 重建索引
        }

        // 實現 ISpatialIndex.GetCount
        int ISpatialIndex.GetCount()
        {
            // TODO: 返回對象數量
            return 0;
        }

        // 實現 ISpatialIndex.GetDepth
        int ISpatialIndex.GetDepth()
        {
            // TODO: 返回索引深度
            return _maxDepth;
        }

        // 實現 ISpatialIndex.GetBounds
        Bounds ISpatialIndex.GetBounds()
        {
            // TODO: 返回索引邊界
            return new Bounds(Vector3.zero, new Vector3(_worldSize, _worldSize, _worldSize));
        }
    }
}
