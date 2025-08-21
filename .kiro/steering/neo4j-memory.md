---
description: 
globs:
alwaysApply: true
---


> 版本：2025-08-17 · 專案：Eternal Abyss · Workdir：`Eternal Abyss 2/`\
> 標籤：`alwaysApply`、`kiro`、`neo4j-memory`\
> 角色：Runbook / 可貼用 Params 集（**允許含當日具體值**）
> 版本：2025-08-17 · 專案：Eternal Abyss · Workdir：`Eternal Abyss 2/`\
> 標籤：`alwaysApply`、`kiro`、`neo4j-memory`\
> 角色：Runbook / 可貼用 Params 集（**允許含當日具體值**）

## 今日執行（2025-08-17）— 批次 Params（可直接搭配 `WRITE_CHANGE` 使用）

> 提醒：`committedAt` 可省略；Neo4j 會以 `datetime()` 補上。

### Batch 1 — Managers 生命週期一致性與空值防護

```json
{
  "specKey": "EA-2025-08-17-MR",
  "taskKey": "EA-Refactor-B1-Managers",
  "changeId": "EA-2025-08-17-B1",
  "title": "Batch 1: Managers 生命週期一致性與空值防護",
  "reason": "收斂初始化/釋放流程，避免重入與資源洩漏；維持對外 API 不變。",
  "diffSummary": "- 加入 isInitialized/isDisposed 旗標\n- 入口集中 try/catch 與統一記錄\n- 補齊空值檢查與資源釋放判斷\n- 僅調整私有欄位與內部邏輯",
  "files": [
    "src/managers/...",
    "src/core/services/...",
    "src/core/logging/..."
  ],
  "author": "chatgpt1@kooapps.com"
}
```

### Batch 2 — 事件/指令內部適配與型別守衛

```json
{
  "specKey": "EA-2025-08-17-MR",
  "taskKey": "EA-Refactor-B2-Events",
  "changeId": "EA-2025-08-17-B2",
  "title": "Batch 2: 事件/指令內部適配與型別守衛",
  "reason": "在不變更事件對外契約下，統一內部 payload 結構並提升容錯。",
  "diffSummary": "- 新增 guards/assertPayload\n- 新增 adapters/ 將歷史 payload 轉標準形\n- 僅替換內部訂閱點為適配入口\n- 記錄第一次容錯警告",
  "files": [
    "src/core/events/...",
    "src/managers/...",
    "src/game/systems/..."
  ],
  "author": "chatgpt1@kooapps.com"
}
```

### Batch 3 — 配置預設與存檔遷移

```json
{
  "specKey": "EA-2025-08-17-MR",
  "taskKey": "EA-Refactor-B3-ConfigSave",
  "changeId": "EA-2025-08-17-B3",
  "title": "Batch 3: 配置預設與存檔遷移",
  "reason": "避免舊存檔讀取錯誤並提升讀檔穩定性，維持鍵名相容。",
  "diffSummary": "- 新增 config/defaults 與 save/migrations/\n- 讀檔流程插入 applyDefaults() 與 migrate()\n- 增加 ENABLE_SAVE_MIGRATION_LOG 開關",
  "files": [
    "assets/config/...",
    "src/core/config/...",
    "src/core/save/..."
  ],
  "author": "chatgpt1@kooapps.com"
}
```

### Batch 4 — ECS/UnitComp 更新與釋放規範

```json
{
  "specKey": "EA-2025-08-17-MR",
  "taskKey": "EA-Refactor-B4-ECS",
  "changeId": "EA-2025-08-17-B4",
  "title": "Batch 4: ECS/UnitComp 更新與釋放規範",
  "reason": "防止幽靈更新與事件未退訂導致的記憶體洩漏。",
  "diffSummary": "- UnitComp 加入 destroyed/disabled 旗標\n- update() 首行早退\n- 事件退訂集中於 onDisable/onDestroy\n- 移除 Debug-only 計時器",
  "files": [
    "src/game/components/UnitComp.ts",
    "src/game/systems/...",
    "src/core/oops/..."
  ],
  "author": "chatgpt1@kooapps.com"
}
```

---

## 快照查詢（可直接貼用）

### 指定路徑清單

```cypher
// 使用 NEO4J.TEMPLATE.SNAPSHOT_BY_PATHS
UNWIND $paths AS p
MATCH (f:File {path:p})<-[:TOUCHES]-(c:Change)
WITH p, c
ORDER BY c.committedAt DESC
WITH p, collect(c)[0] AS lastChange
RETURN p AS path, lastChange.id AS changeId, lastChange.title AS title,
       lastChange.committedAt AS committedAt, lastChange.author AS author;
```

**Params 範例**

```json
{ "paths": [
  "src/managers/GameManager.ts",
  "src/core/events/EventBus.ts"
] }
```

### 指定前綴清單

```cypher
// 使用 NEO4J.TEMPLATE.SNAPSHOT_BY_PREFIXES
UNWIND $prefixes AS pref
MATCH (f:File)
WHERE f.path STARTS WITH pref
MATCH (f)<-[:TOUCHES]-(c:Change)
WITH pref, c
ORDER BY c.committedAt DESC
WITH pref, collect(c)[0] AS lastChange
RETURN pref AS prefix, lastChange.id AS changeId, lastChange.title AS title,
       lastChange.committedAt AS committedAt, lastChange.author AS author;
```

**Params 範例**

```json
{ "prefixes": [
  "src/managers/",
  "src/game/systems/"
] }
```

---

## Runbook（每批固定步驟）

1. **先查快照**（路徑或前綴）
2. **展開 files 清單**（避免萬用字元）
3. **寫入 Change**（對應本檔提供的 Batch Params）
4. **更新 Task 狀態**（選填）
5. **審核**：用 `LIST_RECENT_CHANGES` 檢視排序與作者

## 注意

- 將本檔與 `neo4j-rules.mdc` 一併放於 `./codebuddy/.rules/`，CodeBuddy 啟動即載入。
- 若切換專案或多倉協作，請複製並調整 `project`/`workdir` 設定與命名規約。
