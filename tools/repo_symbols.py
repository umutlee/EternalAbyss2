# tools/repo_symbols.py
# 用法：python3 tools/repo_symbols.py [可選：想查的型別名...]
import re, json, sys
from pathlib import Path

root = Path(".")
cs_files = list(root.rglob("*.cs"))

type_pat = re.compile(r'\b(?:public|internal|protected|private|static|sealed|abstract|partial\s+)*\b(class|struct|interface|enum)\s+([A-Za-z_][A-Za-z0-9_]*)')
ns_pat = re.compile(r'\bnamespace\s+([A-Za-z0-9_.]+)')

index = {}      # shortName -> list of {"fqn","kind","file"}
by_file = {}    # file -> { "namespace": ns, "types":[...] }

for f in cs_files:
    try:
        txt = f.read_text(encoding="utf-8", errors="ignore")
    except Exception:
        continue
    ns = None
    m = ns_pat.search(txt)
    if m:
        ns = m.group(1)
    decls = []
    for kind, name in type_pat.findall(txt):
        fqn = f"{ns}.{name}" if ns else name
        entry = {"fqn": fqn, "kind": kind, "file": str(f)}
        decls.append(entry)
        index.setdefault(name, []).append(entry)
    by_file[str(f)] = {"namespace": ns, "types": decls}

report = {
    "files_scanned": len(cs_files),
    "total_types": sum(len(v["types"]) for v in by_file.values()),
    "index": index,
}

out_dir = Path("tools/_artifacts"); out_dir.mkdir(parents=True, exist_ok=True)
(out_dir / "repo_symbols.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")

# 如果帶了想查的型別，就列出命中；否則列出常見疑難
watch = sys.argv[1:] or ["CreepSourceType","BuildingManager","BuildingConfigSO","ISpatialIndexService","ISpatialIndex","SpatialNode"]
print("== Repo Symbols Quick Lookup ==")
for name in watch:
    items = report["index"].get(name, [])
    print(f"\n{name}: {len(items)} hit(s)")
    for it in items:
        print(f"  - {it['kind']:9s} {it['fqn']}  @ {it['file']}")
print(f"\nWrote JSON: {out_dir/'repo_symbols.json'}")
