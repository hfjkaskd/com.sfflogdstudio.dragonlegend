"""Extract authored FreeRells order and rectangles; no layout inferred from names."""
import json
import re
import sys
from pathlib import Path

source = Path(sys.argv[1])
destination = Path(sys.argv[2])
text = source.read_text(encoding="utf-8-sig")
blocks = {int(i): body for i, body in re.findall(
    r"^--- !u!\d+ &(\d+)\r?\n(.*?)(?=^--- !u!|\Z)", text, re.M | re.S)}

def refs(body, field):
    match = re.search(r"^  " + field + r":\r?\n((?:  - .*\r?\n)+)", body, re.M)
    return [int(i) for i in re.findall(r"fileID: (\d+)", match[1])]

def ref(body, field):
    return int(re.search(r"^  " + field + r": \{fileID: (\d+)\}", body, re.M)[1])

def vector(body, field):
    value = re.search(r"^  " + field + r": \{([^}]+)\}", body, re.M)[1]
    return {axis: float(number) for axis, number in re.findall(r"(\w): ([^,}]+)", value)}

def rect(component):
    body = blocks[component]
    obj = body if body.startswith("GameObject:") else blocks[ref(body, "m_GameObject")]
    transform = blocks[refs(obj, "m_Component")[0]]
    assert vector(transform, "m_AnchorMin") == {"x": .5, "y": .5}
    assert vector(transform, "m_AnchorMax") == {"x": .5, "y": .5}
    assert vector(transform, "m_Pivot") == {"x": .5, "y": .5}
    assert vector(transform, "m_LocalScale") == {"x": 1, "y": 1, "z": 1}
    pos = vector(transform, "m_AnchoredPosition")
    size = vector(transform, "m_SizeDelta")
    return {"name": re.search(r"^  m_Name: (.*)$", obj, re.M)[1].strip(),
            "sourceId": str(component), "x": pos["x"], "y": pos["y"],
            "width": size["x"], "height": size["y"]}

main = next(body for body in blocks.values() if "  FreeRells:" in body)
columns = []
for column_id in refs(main, "FreeRells"):
    column = rect(column_id)
    column["reels"] = [rect(i) for i in refs(blocks[column_id], "RollReels")]
    assert len(column["reels"]) == 3
    columns.append(column)
assert len(columns) == 5
root = rect(ref(main, "FreeRoll"))
root["columns"] = columns
destination.write_text(json.dumps(root, indent=2) + "\n", encoding="utf-8")
print(json.dumps(root))
