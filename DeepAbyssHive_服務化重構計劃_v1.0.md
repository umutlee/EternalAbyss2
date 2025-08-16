# DeepAbyssHive 服務化重構計劃 v1.0
**創建日期**: 2025-08-16  
**狀態**: Phase 3.2 準備完成  
**總預估時間**: 38天  

## 專案現況
- ✅ **Phase 1**: Partial class拆分完成 (6個Manager拆分為partial類)
- ✅ **Phase 2**: 配置外置完成 (所有ConfigSO和Config.asset建立)  
- ✅ **Phase 3.1**: 編譯錯誤修復完成 (25個錯誤全部解決)
- ✅ **服務介面**: 所有IService介面定義完成
- 🔄 **Phase 3.2**: 服務實現階段 (準備開始)

## Phase 3.2: 服務實現階段 (25天，Medium風險)

### Sprint 3.2.1: SpatialIndexManager 服務實現 (3天，Low風險)
**目標**: 提取ISpatialIndexService邏輯
**交付物**:
- ISpatialIndexService實現
- 查詢邏輯提取
- 索引邏輯提取  
- 單元測試
- Smoke測試

**風險因素**: 複雜度最低，分離清晰
**緩解措施**: 作為第一個Sprint驗證工作流程

### Sprint 3.2.2: UnitManager 服務實現 (5天，Medium-Low風險)
**目標**: 提取IUnitQueryService和IUnitCommandService邏輯
**交付物**:
- IUnitQueryService實現
- IUnitCommandService實現
- 移動邏輯提取
- 戰鬥邏輯提取
- 進化邏輯提取
- 單元測試

**風險因素**: 多個服務介面，複雜進化邏輯
**緩解措施**: 分步提取，持續測試

### Sprint 3.2.3: BuildingManager 服務實現 (4天，Medium風險)
**目標**: 提取IBuildingQueryService、IBuildingConstructionService、IResearchService邏輯
**交付物**:
- IBuildingQueryService實現
- IBuildingConstructionService實現
- IResearchService實現
- 建造隊列邏輯
- 研究系統邏輯
- 單元測試

**風險因素**: 三個服務介面，複雜研究依賴，建造隊列管理
**緩解措施**: 按服務分別提取，驗證依賴關係

### Sprint 3.2.4: TerrainManager 服務實現 (4天，Medium風險)
**目標**: 提取ITerrainQueryService和ITerrainModificationService邏輯
**交付物**:
- ITerrainQueryService實現
- ITerrainModificationService實現
- 塊管理邏輯
- 修改隊列邏輯
- 生成邏輯提取
- 單元測試

**風險因素**: 塊系統複雜度，修改隊列管理，生成算法提取
**緩解措施**: 保持塊系統完整性，分步驗證

### Sprint 3.2.5: GameManager 服務實現 (3天，Medium-Low風險)
**目標**: 提取協調和生命週期管理邏輯
**交付物**:
- Manager協調邏輯
- 生命週期管理
- 性能監控
- 狀態管理
- 單元測試

**風險因素**: 中央協調複雜度，多Manager依賴
**緩解措施**: 保持現有協調模式，漸進式提取

### Sprint 3.2.6: CreepManager 服務實現 - **CRITICAL** (8天，High風險)
**目標**: 從1973行代碼中提取ICreepQueryService和ICreepSimulationService
**交付物**:
- ICreepQueryService實現
- ICreepSimulationService實現
- Growth邏輯提取
- 網路管理邏輯
- Source管理邏輯
- 全面單元測試
- 性能基準測試

**風險因素**: 
- 最大代碼庫 (1973行)
- 複雜網路算法
- 性能關鍵的Growth模擬
- 空間索引集成

**特殊處理策略**:
- 增量提取 (每次最多300行)
- 持續測試 (每步都要smoke test)
- 性能監控 (提取前後性能對比)
- 回滾準備 (保留原始備份)
- 分階段驗證 (Growth/Query/Sources分別驗證)

## Phase 3.3: 服務集成階段 (8天，High風險)

### Sprint 3.3 Integration: 服務集成與依賴注入
**目標**: 集成所有服務與Manager，建立完整依賴注入
**交付物**:
- ServiceManager集成
- 所有Manager服務依賴
- 依賴注入設置
- 服務生命週期管理
- 集成測試
- 性能驗證

**風險因素**: 
- 複雜服務依賴
- Manager集成複雜度
- 性能影響評估
- 循環依賴風險

**緩解措施**: 分步集成+依賴圖分析+回滾策略

## Phase 3.4: 測試與優化階段 (5天，Low風險)

### Sprint 3.4 Testing: 全面測試與優化
**目標**: 最終測試、性能優化和文檔
**交付物**:
- 端到端測試
- 性能基準測試
- 內存使用分析
- 文檔更新
- 遷移指南

**風險因素**: 性能回歸檢測，內存洩漏識別
**緩解措施**: 全面性能分析，內存profiling

## 風險控制策略

### CreepManager 風險控制
1. **增量提取**: 每次最多300行
2. **持續測試**: 每步都要smoke test  
3. **性能監控**: 提取前後性能對比
4. **回滾準備**: 保留原始備份
5. **分階段驗證**: Growth/Query/Sources分別驗證

### 服務集成風險控制
1. **分步集成**: 一次集成一個服務
2. **依賴圖分析**: 避免循環依賴
3. **性能基準**: 集成前後性能對比
4. **回滾策略**: 每步都可回滾

## 質量門禁標準
1. **代碼行數**: 每個服務≤300行
2. **測試覆蓋率**: 單元測試覆蓋率≥80%
3. **性能標準**: 性能不劣化
4. **內存使用**: 內存使用不增加
5. **API兼容性**: API兼容性保持
6. **編譯標準**: 編譯零警告
7. **功能驗證**: Smoke test通過

## 工作流程規範
每次修改必須遵循：
1. **讀取Neo4j專案快照** 獲取最新狀態
2. **5-10行中文總結計劃** 包含目標、依據、影響檔案、相容性
3. **等待用戶OK確認** 
4. **產生最小unified diff** (作用在`Eternal Abyss 2/`)
5. **寫回Change記錄** (summary/reason/diff/files)
6. **必要時更新Task狀態**

## 檢查清單
每次工作前必須檢查：
1. 讀取Neo4j專案快照獲取最新狀態
2. 確認當前Sprint目標和deliverables  
3. 檢查前置依賴是否完成
4. 評估風險因素和緩解措施
5. 確認質量門禁標準
6. 準備回滾策略
7. 設定驗證方法

## 關鍵觸發詞
遇到以下詞彙時必須先查詢Neo4j獲取完整計劃上下文：
- "開始Sprint"
- "服務實現" 
- "CreepManager"
- "風險評估"
- "集成測試"
- "性能驗證"

## 項目時間線
- **開始日期**: 2025-08-16
- **預估結束日期**: 2025-09-23
- **總計**: 38天
- **關鍵路徑**: Sprint_3_2_6 (CreepManager) → Sprint_3_3_Integration

## 里程碑
### M1: 服務邏輯提取完成 (2025-09-05)
- 所有6個Manager的服務邏輯成功提取到獨立服務類
- 標準: 所有服務類≤300行，單元測試覆蓋率≥80%，編譯零錯誤

### M2: 服務集成完成 (2025-09-15)  
- 所有服務與Manager完成集成，依賴注入系統正常運行
- 標準: 集成測試通過，性能無劣化，API兼容性保持

---
**文檔版本**: v1.0  
**最後更新**: 2025-08-16 10:00 (台北時間)  
**創建者**: CodeBuddy  
**狀態**: 準備開始Sprint 3.2.1