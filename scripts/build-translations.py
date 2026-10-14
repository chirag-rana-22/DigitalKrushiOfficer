"""Compile reviewed local translations; run after editing Resources/ui.tsv."""
import json
from pathlib import Path

root = Path(__file__).resolve().parent.parent
entries = {}
for number, line in enumerate((root / "Resources/ui.tsv").read_text(encoding="utf-8").splitlines(), 1):
    if not line.strip():
        continue
    parts = line.split("\t")
    if len(parts) != 4 or any(not part.strip() for part in parts):
        raise ValueError(f"Line {number}: expected English, Hindi, Gujarati, Marathi")
    value = dict(zip(("hi", "gu", "mr"), parts[1:]))
    if parts[0] in entries and entries[parts[0]] != value:
        raise ValueError(f"Line {number}: conflicting duplicate key")
    entries[parts[0]] = value
(root / "Resources/ui.json").write_text(json.dumps(entries, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(f"Compiled {len(entries)} translations in three languages.")
