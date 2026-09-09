"""Compare serialized reward text/layout fields by authored hierarchy, not file IDs."""
import hashlib
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
EXPORT = ROOT.parent / "Nut Sort Relax/reconstruction/mumu-current/reference-unity/ExportedProject/Assets/Res/ViewPrefabs"
PAIRS = [("UIRewardView", "BonusRewardPopup"), ("UIJackpotView", "JackpotPopup"),
         ("UITreasureView", "TreasureWindow")]
RECT = "m_LocalRotation m_LocalPosition m_LocalScale m_AnchorMin m_AnchorMax m_AnchoredPosition m_SizeDelta m_Pivot".split()
TEXT = """m_fontAsset m_sharedMaterial m_fontColorGradientPreset m_Color m_fontColor32 m_fontColor m_enableVertexGradient m_colorMode
m_fontColorGradient m_fontSize m_fontSizeBase m_fontWeight m_enableAutoSizing
m_fontSizeMin m_fontSizeMax m_fontStyle m_HorizontalAlignment m_VerticalAlignment
m_textAlignment m_characterSpacing m_wordSpacing m_lineSpacing m_lineSpacingMax
m_paragraphSpacing m_charWidthMaxAdj m_enableWordWrapping m_wordWrappingRatios
m_overflowMode m_enableKerning m_enableExtraPadding m_isRichText m_parseCtrlCharacters
m_isOrthographic m_isCullingEnabled m_horizontalMapping m_verticalMapping
m_uvLineOffset m_geometrySortingOrder m_IsTextObjectScaleStatic m_VertexBufferAutoSizeReduction
m_useMaxVisibleDescender m_pageToDisplay m_margin m_isVolumetricText
m_FontData""".split()


def fields(body):
    return {m[1]: m[2].strip() for m in re.finditer(
        r"^  (\w+):([^\n]*(?:\n(?!  \w+:)[^\n]*)*)", body, re.M)}


def local_id(value):
    return int(re.search(r"fileID: (-?\d+)", value)[1])


def read_nodes(path):
    text = path.read_text(encoding="utf-8-sig")
    records = {int(m[2]): (int(m[1]), fields(m[3])) for m in re.finditer(
        r"^--- !u!(\d+) &(-?\d+)(?: stripped)?\n(.*?)(?=^--- !u!|\Z)", text, re.M | re.S)}
    transforms = {key: f for key, (kind, f) in records.items() if kind in (4, 224) and "m_GameObject" in f and "m_Father" in f}
    go_transform = {local_id(f["m_GameObject"]): key for key, f in transforms.items()}

    def path_for(key):
        f = transforms[key]
        parent = local_id(f["m_Father"])
        if not parent:
            return ""
        name = records[local_id(f["m_GameObject"])][1]["m_Name"]
        return path_for(parent) + "/" + name

    nodes = {}
    for key, (kind, f) in records.items():
        if kind != 114 or not ("m_fontSize" in f or "m_FontData" in f):
            continue
        transform = go_transform[local_id(f["m_GameObject"])]
        name = path_for(transform)
        assert name not in nodes, f"Ambiguous text path: {path}: {name}"
        nodes[name] = {"text": f, "rect": transforms[transform], "ids": (key, transform)}
    return nodes


def run():
    report = {"inputs": [], "checks": [], "differences": []}
    for original, current in PAIRS:
        paths = [ROOT / f"ReferenceOriginal/Res/ViewPrefabs/{original}.prefab",
                 ROOT / f"Assets/Resources/RecoveredUI/{current}.prefab"]
        exported = EXPORT / f"{original}.prefab"
        assert paths[0].read_bytes() == exported.read_bytes(), f"Reference differs from current reverse export: {original}"
        report["inputs"].append({"path": str(exported), "sha256": hashlib.sha256(exported.read_bytes()).hexdigest()})
        for path in paths:
            report["inputs"].append({"path": path.relative_to(ROOT).as_posix(),
                                     "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})
        source, active = map(read_nodes, paths)
        if current == "TreasureWindow":
            parent_text = paths[1].read_text(encoding="utf-8-sig")
            for nested_name in ("TreasureCard", "CollectTip"):
                nested = ROOT / f"Assets/Resources/RecoveredUI/{nested_name}.prefab"
                nested_guid = re.search(r"^guid: (\w+)", Path(str(nested) + ".meta").read_text(), re.M)[1]
                instances = [m[0] for m in re.finditer(r"^--- !u!1001 &.*?(?=^--- !u!|\Z)", parent_text, re.M | re.S)
                             if f"m_SourcePrefab: {{fileID: 100100000, guid: {nested_guid}," in m[0]]
                assert len(instances) == 1, f"Expected one actual {nested_name} instance"
                nodes = read_nodes(nested)
                # This comparison reads authored child records only. Refuse to
                # silently ignore an instance override affecting either record.
                for name, node in nodes.items():
                    for file_id in node["ids"]:
                        assert f"target: {{fileID: {file_id}," not in instances[0], f"Unresolved nested text override: {nested_name}{name}"
                    active[f"/Content/{nested_name}{name}"] = node
                report["inputs"].append({"path": nested.relative_to(ROOT).as_posix(),
                                         "sha256": hashlib.sha256(nested.read_bytes()).hexdigest()})
        for name, node in source.items():
            if name not in active:
                report["differences"].append({"prefab": current, "path": name, "missing": True})
                continue
            for group, keys in (("text", TEXT), ("rect", RECT)):
                for key in keys:
                    if key not in node[group]:
                        continue
                    before, after = node[group][key], active[name][group].get(key)
                    item = {"prefab": current, "path": name, "field": key}
                    if key == "m_sharedMaterial" and "39211f061913f054f84255431c8dce45" in before:
                        material_window = "BonusRewardPopup" if current == "TreasureWindow" else current
                        material = ROOT / f"Assets/Resources/RecoveredUI/{material_window}/#0A5902_4.mat"
                        material_guid = re.search(r"^guid: (\w+)", Path(str(material) + ".meta").read_text(), re.M)[1]
                        item["verifiedReferenceRemap"] = {"source": before, "asset": str(material)}
                        before = before.replace("39211f061913f054f84255431c8dce45", material_guid)
                    if before == after:
                        report["checks"].append(item)
                    else:
                        report["differences"].append(dict(item, source=before, current=after))
    output = ROOT / "Tools/Evidence/reward-typography-audit.json"
    output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"{len(report['checks'])} equal fields; {len(report['differences'])} differences")
    for item in report["differences"]:
        print(json.dumps(item))
    return len(report["differences"])


if __name__ == "__main__":
    raise SystemExit(1 if run() else 0)
