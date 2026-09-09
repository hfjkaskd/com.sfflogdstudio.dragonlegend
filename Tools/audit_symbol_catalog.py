"""Compare the current symbol catalog with original Main scene serialization."""
import hashlib
import json
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
original = root / "ReferenceOriginal"

def text(path):
    return path.read_text(encoding="utf-8-sig")

def normalized(path):
    return "\n".join(line.rstrip() for line in text(path).splitlines()).encode()

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def guid_map(folder):
    result = {}
    for meta in folder.rglob("*.meta"):
        match = re.search(r"^guid: ([0-9a-f]{32})$", text(meta), re.M)
        if match:
            guid = match[1]
            assert guid not in result, f"Duplicate GUID {guid} in {folder}"
            result[guid] = meta.with_suffix("")
    return result

source_map = guid_map(original)
active_map = guid_map(root / "Assets")
scene = original / "Scenes/Main.unity"
catalog = root / "Assets/Resources/RecoveredSymbols/OriginalSymbolCatalog.asset"
section = text(scene).split("  SymbolInfos:\n", 1)[1].split("  ZhuanChuang:", 1)[0]
sources = re.findall(r"  - name: (.*?)\n(.*?)(?=  - name: |\Z)", section, re.S)
current = text(catalog)
definitions = re.findall(r'  - originalName: "(.*?)"\n(.*?)(?=  - originalName: |  baseOrder:)', current, re.S)
assert len(sources) == len(definitions) == 11
records = []
for (name, source), (current_name, definition) in zip(sources, definitions):
    assert name == current_name
    symbol_id = int(re.search(r"    id: (\d+)", source)[1])
    assert symbol_id == int(re.search(r"    id: (\d+)", definition)[1])
    record = {"id": symbol_id, "name": name, "sprites": []}
    for source_field, current_field in (("sprite", "spritePath"), ("blurSprite", "blurSpritePath")):
        guid = re.search(rf"    {source_field}: .*guid: ([0-9a-f]{{32}})", source)[1]
        source_sprite = source_map[guid]
        resource = re.search(rf"    {current_field}: (.+)", definition)[1]
        active_sprite = root / "Assets/Resources" / (resource + ".asset")
        assert normalized(source_sprite) == normalized(active_sprite), f"Sprite geometry differs: {name}/{source_field}"
        textures = []
        for texture_guid in set(re.findall(r"guid: ([0-9a-f]{32})", text(source_sprite))):
            source_texture, active_texture = source_map[texture_guid], active_map[texture_guid]
            assert source_texture.read_bytes() == active_texture.read_bytes(), f"Texture bytes differ: {name}/{source_field}"
            textures.append({"source": source_texture.relative_to(original).as_posix(), "active": active_texture.relative_to(root).as_posix(), "sha256": digest(source_texture)})
        record["sprites"].append({"field": source_field, "sourceGuid": guid, "resource": resource, "normalizedSpriteSha256": hashlib.sha256(normalized(source_sprite)).hexdigest(), "textures": textures})
    effects = [source_map[guid].relative_to(original).as_posix() for guid in re.findall(r"guid: ([0-9a-f]{32})", source.split("    effects:", 1)[1])]
    assert effects == re.findall(r"    - (.+)", definition)
    record["originalEffectPaths"] = effects
    records.append(record)

# GManager.InitSymbols 2370fa4: copy source then RemoveAt(8) for Base;
# copy source then RemoveRange(7, Count-7) for Free. These are list indices.
base = [entry["id"] for index, entry in enumerate(records) if index != 8]
free = [entry["id"] for entry in records[:7]]
assert base == [int(value) for value in re.findall(r"  - (\d+)", current.split("  baseOrder:\n")[1].split("  freeOrder:\n")[0])]
assert free == [int(value) for value in re.findall(r"  - (\d+)", current.split("  freeOrder:\n")[1])]
report = {"sourceSceneSha256": digest(scene), "catalogSha256": digest(catalog), "baseOrder": base, "freeOrder": free, "definitions": records}
(root / "Tools/Evidence/symbol-catalog-audit.json").write_text(json.dumps(report, indent=2, ensure_ascii=False)+"\n", encoding="utf-8")
print(f"Verified {len(records)} definitions, 22 sprite payloads and texture bytes; Base {base}; Free {free}")
