"""Read authored Unity YAML text and template scalars without importing Unity."""
import ast
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCALAR_FIELDS = re.compile(r"^(?:m_[Tt]ext|.*(?:Format|Text|Message|Caption|Prompt)|tip|progressing|expiredInitial|expiredRefresh)$")


def scalar_value(raw):
    # Unity emits long scalars as YAML flow scalars: physical lines are folded.
    lines = raw.splitlines()
    folded = lines[0]
    for previous, current in zip(lines, lines[1:]):
        folded += ("\n" if not previous.strip() or not current.strip() else " ") + current.strip()
    if folded.startswith('"') and folded.endswith('"'):
        return ast.literal_eval(folded.replace("\n", "\\n"))
    if folded.startswith("'") and folded.endswith("'"):
        return folded[1:-1].replace("''", "'")
    return folded


def authored_texts():
    for folder in ("Assets/Resources/RecoveredUI", "Assets/Resources/Whitebox", "Assets/Whitebox/Scenes"):
        for path in sorted((ROOT / folder).rglob("*")):
            if path.suffix not in (".prefab", ".unity"):
                continue
            source = path.read_text(encoding="utf-8-sig")
            for match in re.finditer(r"^  (\w+):(?: ([^\n]*))?\n((?:    [^\n]*\n|\n)*)", source, re.M):
                field, first, continuation = match.groups()
                if not SCALAR_FIELDS.match(field) or not first or first.startswith("{"):
                    continue
                value = scalar_value(first + ("\n" + continuation.rstrip("\n") if continuation else ""))
                if re.search("[A-Za-z]", re.sub(r"<[^>]+>", "", value)):
                    yield {"file": str(path.relative_to(ROOT)).replace("\\", "/"),
                           "line": source[:match.start()].count("\n") + 1,
                           "field": field, "text": value}
            for match in re.finditer(r"^  taskFormats:\n((?:  - [^\n]*\n)+)", source, re.M):
                for value in match.group(1).splitlines():
                    yield {"file": str(path.relative_to(ROOT)).replace("\\", "/"),
                           "line": source[:match.start()].count("\n") + 1,
                           "field": "taskFormats", "text": scalar_value(value[4:])}


if __name__ == "__main__":
    print(json.dumps(list(authored_texts()), ensure_ascii=True, indent=2))
