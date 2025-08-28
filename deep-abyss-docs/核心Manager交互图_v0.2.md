# 核心 Manager 交互图（v0.2）
版本: 0.2
日期: 2025-08-28
作者: CodeBuddy
上游版本：_archive/2025-08/依赖关系图v0.1-核心Manager交互.md

变更摘要
- 补充 SpatialIndexService 初始化顺序（Bounds 注入）
- 明确 ServiceManager 的注册/获取流程
- 标注 Buildings/Units/Terrain/Creep 对 SpatialIndex 的典型依赖

概览
- GameManager：驱动生命周期（Initialize/Update/Fixed/Late）
- ServiceManager：统一注册与依赖解析
- SpatialIndexService：空间查询服务（QueryRange/QueryBounds/QueryNearest）
- Building/Unit/Terrain/Creep Managers：各自系统逻辑，通过服务获取查询能力

交互（文字版）
1) GameManager → ServiceManager：注册/初始化各服务（含 SpatialIndexService）
2) BuildingManager/UnitManager/TerrainManager/CreepManager → ServiceManager：GetService<ISpatialIndexService>()
3) SpatialIndexService.Initialize(new Bounds(center,size)) 后提供查询接口
4) 各 Manager 在 Update 流程中按需调用查询（注意 FrameQueries 与统计重置）

注意
- Core/Interfaces/ISpatialIndex 已 Obsolete，使用 SpatialIndex.Interfaces
- 兼容层（Compat）仍有 TODO，避免在生产路径调用