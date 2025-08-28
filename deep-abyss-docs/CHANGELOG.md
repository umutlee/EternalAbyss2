# 变更日志 CHANGELOG
版本: 0.1
日期: 2025-08-28
作者: CodeBuddy

说明
- 本日志对齐 Neo4j Change 记录，聚合同日的代码与文档改动。
- 仅列出关键影响与模块范围；完整 diff 请参考仓库或 Neo4j 中的 diffSummary。

2025-08-28
- EA-2025-08-28-stats-dedupe — SpatialIndex
  - 去重统计类型：将快照结构改名为 SpatialIndexPerfSnapshot，避免与运行时类名冲突
  - 修复 Bounds 初始化：Initialize 传入 Bounds 而非 float
  - 指标增强：AverageQueryTime
- EA-2025-08-28-B7 — Buildings
  - 使用枚举 BuildingState 取代兼容常量（BuildingState_Compat）
- 新增
  - 测试：Assets/DeepAbyssHive/Tests/PlayMode/SpatialIndexSmokeTest.cs（覆盖初始化/增删改/范围与最近查询/占位检测/性能统计）
  - 文档：空间索引性能指标与统计规范.md（指标定义/采集时机/阈值建议/测试协议）

2025-08-27
- EA-2025-08-27-06 — Units
  - UnitData 清理重复字段（无类型变化）
- EA-2025-08-27-01 — Creep
  - 统一枚举括号与取值；Legacy → Data 版本迁移标注

2025-08-26
- ea-2025-08-26-05-stability-fixes — Terrain/SpatialIndex/Units/Buildings/Tests
  - 多模块稳定性修复（编译/接口一致性/基础用例可运行）

参考
- 来源: Neo4j 模型变更记录
- 维护策略: 每次合并前更新本文件；跨日批量提交按模块分节