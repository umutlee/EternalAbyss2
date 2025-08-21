---
description: 
globs:
alwaysApply: true
---

> 版本：2025-08-17 · 專案：Eternal Abyss · Workdir：`Eternal Abyss 2/`\
> 標籤：`alwaysApply`、`kiro`、`neo4j`

## 目的
> 版本：2025-08-17 · 專案：Eternal Abyss · Workdir：`Eternal Abyss 2/`\
> 標籤：`alwaysApply`、`kiro`、`neo4j`

## 目的
統一 Kiro × Neo4j 的記錄與查詢規範，提供**可重用**的 Cypher 模板與命名規約。**嚴禁**將當日/當批次的具體參數（如 `EA-2025-...`）寫死於此規則檔。

## Session Guard（規則檔層級）

- 僅針對 `Eternal Abyss 2/` 之下的檔案路徑建立/關聯 `(:File {path})`。
- 產生 `(:Spec)-[:SCOPES]->(:Task)-[:HAS_CHANGE]->(:Change)-[:TOUCHES]->(:File)` 基本骨架。
- 寫入時**避免萬用字元落地**（`**`、`*`）；需展開為實際檔案路徑。
- 寫入 `Change.committedAt` 缺省為 `datetime()`；並記錄 `updatedAt`。

## 命名規約

- `specKey`：`${PROJECT}-${YYYY-MM-DD}-MR` 例：`EA-2025-08-17-MR`
- `taskKey`：`${PROJECT}-Refactor-B${N}-<Label>` 例：`EA-Refactor-B2-Events`
- `changeId`：`${PROJECT}-${YYYY-MM-DD}-B${N}` 例：`EA-2025-08-17-B3`
- `author`：使用公司 email 例：`chatgpt1@kooapps.com` 或 agent 名稱 例 `Claude Sonnet 4.0`  

---

## 模板一：查快照（以**明確路徑清單**為準）

**ID：NEO4J.TEMPLATE.SNAPSHOT\_BY\_PATHS**

```cypher
// Params: { paths: ["src/...", "assets/..." ] }
UNWIND $paths AS p
MATCH (f:File {path:p})<-[:TOUCHES]-(c:Change)
WITH p, c
ORDER BY c.committedAt DESC
WITH p, collect(c)[0] AS lastChange
RETURN p AS path,
       lastChange.id AS changeId,
       lastChange.title AS title,
       lastChange.committedAt AS committedAt,
       lastChange.author AS author;
```

## 模板二：查快照（以**前綴**聚合，對應 `src/managers/` 等）

**ID：NEO4J.TEMPLATE.SNAPSHOT\_BY\_PREFIXES**

```cypher
// Params: { prefixes: ["src/managers/", "src/core/events/"] }
UNWIND $prefixes AS pref
MATCH (f:File)
WHERE f.path STARTS WITH pref
MATCH (f)<-[:TOUCHES]-(c:Change)
WITH pref, c
ORDER BY c.committedAt DESC
WITH pref, collect(c)[0] AS lastChange
RETURN pref AS prefix,
       lastChange.id AS changeId,
       lastChange.title AS title,
       lastChange.committedAt AS committedAt,
       lastChange.author AS author;
```

## 模板三：寫入/更新 Change（含 Spec/Task/Files 關聯）

**ID：NEO4J.TEMPLATE.WRITE\_CHANGE**

```cypher
// Params (建議)：
// {
//   specKey, taskKey, changeId,
//   title, reason, diffSummary, files: ["src/...","assets/..."],
//   author, committedAt // 可省略；省略時以 datetime() 自動填
// }
MERGE (s:Spec {key:$specKey})
  ON CREATE SET s.title = coalesce($specTitle, "Eternal Abyss 最小風險收斂"),
                s.project = coalesce($project, "Eternal Abyss"),
                s.createdAt = datetime()
MERGE (t:Task {key:$taskKey})
  ON CREATE SET t.title = coalesce($taskTitle, "重構子任務"),
                t.status = coalesce($taskStatus, "in_progress"),
                t.createdAt = datetime()
MERGE (c:Change {id:$changeId})
  ON CREATE SET c.title = $title,
                c.reason = $reason,
                c.diffSummary = $diffSummary,
                c.author = $author,
                c.committedAt = coalesce($committedAt, datetime()),
                c.createdAt = datetime()
  ON MATCH SET  c.title = coalesce($title, c.title),
                c.reason = coalesce($reason, c.reason),
                c.diffSummary = coalesce($diffSummary, c.diffSummary),
                c.author = coalesce($author, c.author),
                c.updatedAt = datetime()
MERGE (s)-[:SCOPES]->(t)
MERGE (t)-[:HAS_CHANGE]->(c)
WITH c, $files AS files
UNWIND files AS fp
MERGE (f:File {path:fp})
MERGE (c)-[:TOUCHES]->(f)
RETURN c.id AS changeId, size(files) AS filesCount;
```

## 模板四：更新任務狀態

**ID：NEO4J.TEMPLATE.UPDATE\_TASK\_STATUS**

```cypher
// Params: { taskKey: "EA-Refactor-B2-Events", newStatus: "done" }
MATCH (t:Task {key:$taskKey})
SET t.status = $newStatus,
    t.updatedAt = datetime()
RETURN t.key AS taskKey, t.status AS status, t.updatedAt AS updatedAt;
```

## 模板五：列出近期變更（審核用）

**ID：NEO4J.TEMPLATE.LIST\_RECENT\_CHANGES**

```cypher
// Params: { limit: 20 }
MATCH (c:Change)
RETURN c.id AS changeId, c.title AS title, c.author AS author,
       c.committedAt AS committedAt
ORDER BY c.committedAt DESC
LIMIT coalesce($limit, 20);
```

## 使用指引

1. **先查快照**（模板一或二）確認最近一次觸達的檔案與時間。
2. 實作**最小 diff**；展開實際 `files` 清單（避免 `**`）。
3. 用模板三寫入 `Change` 與關聯；未提供 `committedAt` 則由伺服器填 `datetime()`。
4. 需要時用模板四調整 `Task` 狀態；審核時用模板五。