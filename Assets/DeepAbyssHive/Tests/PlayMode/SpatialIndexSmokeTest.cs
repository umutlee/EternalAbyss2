using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DeepAbyssHive.SpatialIndex.Enums;
using DeepAbyssHive.SpatialIndex.Services;

namespace DeepAbyssHive.Tests.SpatialIndex
{
    /// <summary>
    /// 空間索引冒煙測試（PlayMode）
    /// 覆蓋：Initialize/Add/Update/Remove、QueryRange/QueryBounds/QueryNearest/QueryKNearest、IsPositionOccupied、
    /// 以及效能統計（TotalQueries/AverageQueryTime/FrameQueries 重置）
    /// </summary>
    public class SpatialIndexSmokeTest
    {
        private SpatialIndexService _svc;

        [SetUp]
        public void SetUp()
        {
            _svc = new SpatialIndexService();
            // 使用 1000 立方世界邊界
            _svc.Initialize(new Bounds(Vector3.zero, Vector3.one * 1000f));
        }

        [TearDown]
        public void TearDown()
        {
            _svc?.Cleanup();
            _svc = null;
        }

        [Test]
        public void AddUpdateRemove_BasicFlow_Works()
        {
            var id1 = 1;
            var pos1 = Vector3.zero;
            var b1 = new Bounds(pos1, Vector3.one);

            Assert.IsTrue(_svc.AddObject(id1, pos1, b1, SpatialObjectType.All), "AddObject 應成功");
            _svc.Update(0f); // 處理待處理隊列
            Assert.AreEqual(1, _svc.GetObjectCount(), "物件數應為 1");

            // 更新位置
            var newPos = new Vector3(10, 0, 0);
            Assert.IsTrue(_svc.UpdateObject(id1, newPos), "UpdateObject 應成功");
            _svc.Update(0f);

            var info = _svc.GetObjectInfo(id1);
            Assert.IsTrue(info.HasValue, "應能取得物件資訊");
            Assert.AreEqual(newPos, info.Value.Position, "位置應更新成功");

            // 移除
            Assert.IsTrue(_svc.RemoveObject(id1), "RemoveObject 應成功");
            _svc.Update(0f);
            Assert.AreEqual(0, _svc.GetObjectCount(), "物件數應回到 0");
        }

        [UnityTest]
        public IEnumerator Query_APIs_ReturnExpected_And_UpdateStats()
        {
            // 建立 11x11 的地面網格（間距 2）
            int id = 0;
            for (int x = -5; x <= 5; x++)
            {
                for (int z = -5; z <= 5; z++)
                {
                    var p = new Vector3(x * 2f, 0, z * 2f);
                    var b = new Bounds(p, Vector3.one);
                    Assert.IsTrue(_svc.AddObject(++id, p, b, SpatialObjectType.All));
                }
            }
            _svc.Update(0f);

            var beforeStats = _svc.GetPerformanceStats();

            // QueryRange（半徑 5）— 預期命中多筆
            var rangeArr = _svc.QueryRange(Vector3.zero, 5f, SpatialObjectType.All);
            var rangeCount = rangeArr.Length;
            rangeArr.Dispose();
            Assert.Greater(rangeCount, 0, "QueryRange 應有結果");

            // QueryBounds（邊長 10 的立方）— 預期數量不少於 QueryRange
            var boundsArr = _svc.QueryBounds(new Bounds(Vector3.zero, Vector3.one * 10f), SpatialObjectType.All);
            var boundsCount = boundsArr.Length;
            boundsArr.Dispose();
            Assert.GreaterOrEqual(boundsCount, rangeCount, "QueryBounds 應不少於 QueryRange");

            var afterStats = _svc.GetPerformanceStats();
            Assert.Greater(afterStats.TotalQueries, beforeStats.TotalQueries, "TotalQueries 應遞增");
            Assert.GreaterOrEqual(afterStats.AverageQueryTime, 0f, "AverageQueryTime 應為非負");
            Assert.GreaterOrEqual(afterStats.FrameQueries, 1, "本幀查詢次數應 ≥ 1");

            // 每幀重置：呼叫 UpdateService（透過 Update）
            _svc.Update(0f);
            var resetStats = _svc.GetPerformanceStats();
            Assert.AreEqual(0, resetStats.FrameQueries, "Update 後 FrameQueries 應重置為 0");

            yield return null;
        }

        [Test]
        public void QueryNearest_And_KNearest_Work()
        {
            // 三個點：0, 1, 5
            Assert.IsTrue(_svc.AddObject(1, new Vector3(0, 0, 0), new Bounds(Vector3.zero, Vector3.one), SpatialObjectType.All));
            Assert.IsTrue(_svc.AddObject(2, new Vector3(1, 0, 0), new Bounds(new Vector3(1, 0, 0), Vector3.one), SpatialObjectType.All));
            Assert.IsTrue(_svc.AddObject(3, new Vector3(5, 0, 0), new Bounds(new Vector3(5, 0, 0), Vector3.one), SpatialObjectType.All));
            _svc.Update(0f);

            var nearest = _svc.QueryNearest(new Vector3(0.9f, 0, 0), SpatialObjectType.All, 100f);
            Assert.AreEqual(2, nearest, "最近點應為 ID=2");

            var knearest = _svc.QueryKNearest(Vector3.zero, 2, SpatialObjectType.All, 100f);
            Assert.AreEqual(2, knearest.Length, "K 最近應返回 k 筆或更少");
            knearest.Dispose();
        }

        [Test]
        public void IsPositionOccupied_Works()
        {
            Assert.IsTrue(_svc.AddObject(10, Vector3.zero, new Bounds(Vector3.zero, Vector3.one), SpatialObjectType.All));
            _svc.Update(0f);

            Assert.IsTrue(_svc.IsPositionOccupied(Vector3.zero, 0.6f), "原地半徑 0.6 應被佔用");
            Assert.IsFalse(_svc.IsPositionOccupied(new Vector3(10, 0, 0), 0.5f), "遠處不應被佔用");
            Assert.IsFalse(_svc.IsPositionOccupied(Vector3.zero, 0.6f, excludeObjectId: 10), "排除自身後不應被佔用");
        }
    }
}