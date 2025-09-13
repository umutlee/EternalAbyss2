#if false // TEMP: 停用，待 Runtime asmdef 建好後再啟用
#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DeepAbyssHive.SpatialIndex.Enums;
using DeepAbyssHive.SpatialIndex.Services;
using DeepAbyssHive.SpatialIndex.Data;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Tests.SpatialIndex
{
    /// <summary>
    /// 空間索引規模化效能測試（PlayMode，無需場景）
    /// - 覆蓋批量導入、固定次數 QueryRange/QueryBounds、效能統計輸出
    /// - 以 JSON 行輸出指標：N、queries、avgQueryTime、objects、frameQueries
    /// 說明：
    /// - 測試不對平均耗時設置嚴格斷言，避免編輯器/機器差異造成誤報；
    ///   嚴格閾值請在 CI 環境對輸出 JSON 進行門檻校驗。
    /// </summary>
    public class SpatialIndexScalePerfTest
    {
        private SpatialIndexService _svc;

        [UnityTest]
        public IEnumerator Scale_1k_And_5k_Perf_Smoke()
        {
            yield return RunPerfCase(1000, queryTrials: 20, radius: 10f, worldSize: 1000f);
            yield return RunPerfCase(5000, queryTrials: 20, radius: 12f, worldSize: 1200f);
        }

        private IEnumerator RunPerfCase(int N, int queryTrials, float radius, float worldSize)
        {
            _svc = new SpatialIndexService();
            _svc.Initialize(new Bounds(Vector3.zero, Vector3.one * worldSize));

            // 構造資料（均勻隨機）
            var objects = new SpatialObjectInfo[N];
            var half = worldSize * 0.5f * 0.9f; // 留 buffer 避免越界
            var rng = new System.Random(12345 + N);
            for (int i = 0; i < N; i++)
            {
                var x = (float)(rng.NextDouble() * 2 - 1) * half;
                var z = (float)(rng.NextDouble() * 2 - 1) * half;
                var pos = new Vector3(x, 0, z);
                objects[i] = new SpatialObjectInfo
                {
                    ObjectId = i + 1,
                    Position = pos,
                    Bounds = new Bounds(pos, Vector3.one),
                    ObjectType = SpatialObjectType.All
                };
            }

            // 批量導入
            var inserted = _svc.AddObjectsBatch(objects);
            Assert.AreEqual(N, inserted, "批量導入數量應與 N 一致");
            _svc.Update(0f);
            Assert.AreEqual(N, _svc.GetObjectCount(), "索引內物件數應等於 N");

            // 固定次數 QueryRange/QueryBounds，採樣多個隨機中心
            for (int t = 0; t < queryTrials; t++)
            {
                var cx = (float)(rng.NextDouble() * 2 - 1) * (half * 0.8f);
                var cz = (float)(rng.NextDouble() * 2 - 1) * (half * 0.8f);
                var center = new Vector3(cx, 0, cz);

                var arr1 = _svc.QueryRange(center, radius, SpatialObjectType.All);
                // 不要求數量上限，僅確認 API 可用且不拋例外
                Assert.GreaterOrEqual(arr1.Length, 0);
                arr1.Dispose();

                var b = new Bounds(center, new Vector3(radius * 2, radius * 2, radius * 2));
                var arr2 = _svc.QueryBounds(b, SpatialObjectType.All);
                Assert.GreaterOrEqual(arr2.Length, 0);
                arr2.Dispose();
            }

            // 取統計
            var stats = _svc.GetPerformanceStats();
            // 功能性斷言
            Assert.GreaterOrEqual(stats.TotalQueries, queryTrials * 2, "TotalQueries 應至少等於試次 * 2");
            Assert.GreaterOrEqual(stats.AverageQueryTime, 0f, "AverageQueryTime 應為非負");
            Assert.AreEqual(N, stats.ObjectCount, "ObjectCount 應等於 N");

            // JSON 行輸出（便於 CI 解析）
            var json = new StringBuilder();
            json.Append("{");
            json.AppendFormat("\"case\":\"SpatialIndexScalePerfTest\",\"N\":{0}", N);
            json.AppendFormat(",\"queries\":{0}", stats.TotalQueries);
            json.AppendFormat(",\"frameQueries\":{0}", stats.FrameQueries);
            json.AppendFormat(",\"avgQueryTimeSec\":{0}", stats.AverageQueryTime.ToString("0.########"));
            json.AppendFormat(",\"objects\":{0}", stats.ObjectCount);
            json.Append("}");
            DAHLog.Info(LogCategory.COMMON, json.ToString());

            _svc.Cleanup();
            _svc = null;
            yield return null;
        }
    }
}
#endif
#endif