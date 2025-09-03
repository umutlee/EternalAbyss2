---
description: 
globs:
alwaysApply: true
---

description: 專案內的錯誤修復與各種調整，以「Change」記錄到 Neo4j。⚠️ 本檔不建立任何新節點類型；凡是寫入請僅使用 WRITE\_CHANGE。   
Guardrails for this repo – DO NOT use create_entities for change batches
globs: \["Eternal Abyss 2/\*\*"\]   
tags: ["alwaysApply","codebuddy","neo4j-cypher","memory","runbook"]


# 強制規則（避免模型誤用）
forbidTools:
  - create_entities          # 🚫 禁用。更動後的紀錄寫入一律用 WRITE_CHANGE。
  - Create_Entities          # 同名大小寫變體也禁用。
---

專案：Eternal Abyss · Workdir：Eternal Abyss 2/
角色：Runbook / 可貼用 Params 集（允許含當日具體值）
版本：2025-08-25

> 重點：本檔提供「查快照的讀操作模板」與「寫入變更的批次 Params」。
> 任何寫入 **只允許** 使用 `WRITE_CHANGE`。**不要** 用 `create_entities` 或類似名稱的工具。

提醒：`committedAt` 可省略；Neo4j 會以 `datetime()` 補上。

---

# 一、快照查詢（READ；可直接貼用）

## 1\) 指定路徑清單

// 使用 NEO4J.TEMPLATE.SNAPSHOT\_BY\_PATHS（READ）

UNWIND $paths AS p MATCH (f:File {path:p})\<-\[:TOUCHES\]-(c:Change) WITH p, c ORDER BY c.committedAt DESC WITH p, collect(c)\[0\] AS lastChange RETURN p AS path, lastChange.id AS changeId, lastChange.title AS title, lastChange.committedAt AS committedAt, lastChange.author AS author;

Params 範例：

{ "paths": \[

  "src/managers/GameManager.ts",

  "src/core/events/EventBus.ts"

] }

## **2\) 指定前綴清單**

// 使用 NEO4J.TEMPLATE.SNAPSHOT\_BY\_PREFIXES（READ）

UNWIND $prefixes AS pref  
 MATCH (f:File)  
 WHERE f.path STARTS WITH pref  
 MATCH (f)\<-\[:TOUCHES\]-(c:Change)  
 WITH pref, c  
 ORDER BY c.committedAt DESC  
 WITH pref, collect(c)\[0\] AS lastChange  
 RETURN pref AS prefix,  
 lastChange.id AS changeId,  
 lastChange.title AS title,  
 lastChange.committedAt AS committedAt,  
 lastChange.author AS author;

Params 範例：

`{ "prefixes": [`  
  `"src/managers/",`  
  `"src/game/systems/"`  
`] }`

---

# **二、寫入變更（WRITE；只用 WRITE\_CHANGE）**

這裡是「批次 Params」**樣板**。**請搭配 `WRITE_CHANGE` 工具**；不得使用 `create_entities`。

## **WRITE\_CHANGE.batch — 參數 Schema**

`{`  
  `"changes": [`  
    `{`  
      `"id": "ea-2025-08-17-01",      // 可選；不給則由系統/Neo4j 產生`  
      `"title": "修正 UnitData 相容層，補 MaxEnergy/AttackSound/DeathSound",`  
      `"author": "your.name",         // 可選`  
      `"committedAt": "2025-08-17T10:20:00Z", // 可選；不給則用 datetime()`  
      `"note": "透過 partial struct 補兼容屬性以消除編譯錯誤",`  
      `"tags": ["bugfix","units","compat"],   // 可選`  
      `"touches": [`  
        `"Assets/DeepAbyssHive/Units/Data/UnitData.cs",`  
        `"Assets/DeepAbyssHive/Units/Compat/UnitData_Compat.cs"`  
      `]`  
    `}`  
  `]`  
`}`

### **欄位說明（供模型對齊）**

* `changes[]`：一次可寫入多筆變更。

* `id`：字串；可自定流水號或留空。

* `title`：必要；一句話摘要。

* `author`：選填；人名或帳號。

* `committedAt`：選填；ISO-8601；留空則 `datetime()`。

* `note`：選填；更長的說明。

* `tags[]`：選填；便於查詢。

* `touches[]`：必要；受影響檔案完整相對路徑清單。

檢查點（Guard）

* 若工具名稱包含「entity / entities」字樣 → **停止**，改用 `WRITE_CHANGE`。

* 若輸入參數頂層不是 `changes` 陣列 → **拒絕寫入** 並回報使用者修正為上述 Schema。

---

# **三、Runbook（建議固定步驟）**

1. 先用「快照查詢」確認目標檔案/前綴最近一次變更。

2. 展開所有萬用字元成為具體 `files` 清單。

3. 準備 `WRITE_CHANGE.batch` 的 `changes[]`，逐筆填上 `title / touches / tags / note`（`id/author/committedAt` 可省）。

4. 使用 **WRITE\_CHANGE** 執行寫入。

5. （選填）更新任務狀態區或建立待辦紀錄。

6. 用 `LIST_RECENT_CHANGES` 或前述快照查詢，核對排序與作者。

---

# **四、放置與適用範圍**

* 本檔與 `neo4j-rules.mdc` 一併放於 `./codebuddy/.rules/`，CodeBuddy 啟動即載入。

* 若切換專案或多倉協作，請複製並調整 `globs` 與 `Workdir` 設定。

* 本檔 **不定義任何 entity schema**；凡遇到本檔的「批次 Params」，模型**不得**推論為 `create_entities`。  

}