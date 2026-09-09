"""Verify the shared reward SDF font payload and copied material parameters."""
import difflib
import hashlib
import json
from pathlib import Path
import re
import tarfile

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT.parent / "Nut Sort Relax/reconstruction/mumu-current/reference-unity/ExportedProject/Assets"
FONT = "QuorumStd-Black_zitidi.com"


def guid(path):
    return re.search(r"^guid: (\w+)", Path(str(path) + ".meta").read_text(), re.M)[1]


def text(path):
    return path.read_text(encoding="utf-8-sig").strip()


def run():
    report = {"inputs": [], "checks": [], "differences": []}

    def compare(label, source, active, replacements=None, binary=False):
        for path in (source, active):
            report["inputs"].append({"path": str(path), "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})
        before, after = (source.read_bytes(), active.read_bytes()) if binary else (text(source), text(active))
        for old, new in (replacements or {}).items():
            assert old in before, f"Missing expected remap in {label}: {old}"
            before = before.replace(old, new)
        if before != after:
            diff = "binary payload differs" if binary else "\n".join(difflib.unified_diff(before.splitlines(), after.splitlines()))
            report["differences"].append({"name": label, "diff": diff})
            print(label, diff[:8000])
        else:
            report["checks"].append({"name": label, "remaps": replacements or {},
                                     "payloadSha256": hashlib.sha256(after if binary else after.encode()).hexdigest()})

    shader = ROOT / "Assets/TextMesh Pro/Shaders/TMP_SDF.shader"
    package = ROOT / "Library/PackageCache/com.unity.textmeshpro@3.0.9/Package Resources/TMP Essential Resources.unitypackage"
    with tarfile.open(package, "r:gz") as archive:
        official_shader = archive.extractfile(guid(shader) + "/asset").read()
        assert official_shader == shader.read_bytes(), "Imported SDF shader differs from official installed TMP resources"
    report["inputs"].append({"path": str(package), "sha256": hashlib.sha256(package.read_bytes()).hexdigest()})
    report["checks"].append({"name": "Official TMP SDF shader bytes", "payloadSha256": hashlib.sha256(official_shader).hexdigest()})
    font_script = ROOT / "Library/PackageCache/com.unity.textmeshpro@3.0.9/Scripts/Runtime/TMP_FontAsset.cs"
    font_file = ROOT / f"Assets/Resources/RecoveredArt/Res/Font/{FONT}.otf"
    assert guid(font_file) == "83a0c6f40746cee418b341acfcb6f545"
    compare("Original font bytes", SOURCE / f"Res/Font/{FONT}.otf", font_file, binary=True)
    compare("Atlas PNG bytes", SOURCE / f"Res/Font/{FONT} Atlas.png",
            ROOT / f"Assets/Resources/RecoveredArt/Res/Font/{FONT} Atlas.png", binary=True)
    compare("Complete SDF font serialization", SOURCE / f"Res/Font/{FONT} SDF.asset",
            ROOT / f"Assets/Resources/RecoveredText/Res/Font/{FONT} SDF.asset", {
                "eef129d5e40c07af534a8e24da6b5c16": guid(font_script),
                "m_SourceFontFileGUID: 73588d9165a7c5a4e8befbf84c0b178c": "m_SourceFontFileGUID: " + guid(font_file)})
    shader_remap = {"a495248e59fa6714793dae46034b0996": guid(shader)}
    compare("Font atlas material", SOURCE / f"Res/Font/{FONT} Atlas Material.mat",
            ROOT / f"Assets/Resources/RecoveredText/Res/Font/{FONT} Atlas Material.mat", shader_remap)
    material_dir = SOURCE / "TextMesh Pro/Resources/Fonts & Materials"
    compare("Shared black-outline material", material_dir / "#000000_3.mat",
            ROOT / "Assets/Resources/Fonts & Materials/#000000_3.mat", shader_remap)
    for window in ("BonusRewardPopup", "JackpotPopup"):
        compare(window + " claim material", material_dir / "#0A5902_4.mat",
                ROOT / f"Assets/Resources/RecoveredUI/{window}/#0A5902_4.mat", shader_remap)
    green_font = ROOT / "Assets/Resources/RecoveredUI/CoinRewardText/Green.asset"
    green_material = ROOT / "Assets/Resources/RecoveredUI/CoinRewardText/Green_Material.mat"
    green_atlas = ROOT / "Assets/Resources/RecoveredArt/Res/UI/pop-up/font_b.png"
    assert f"guid: {guid(green_material)}" in text(green_font)
    assert f"guid: {guid(green_atlas)}" in text(green_material)
    compare("Complete legacy Green font serialization", SOURCE / "Res/Font/Green.asset", green_font)
    compare("Legacy Green material", SOURCE / "Res/Font/Green_Material.mat", green_material)
    compare("Legacy Green atlas PNG bytes", SOURCE / "Res/UI/pop-up/font_b.png", green_atlas, binary=True)
    (ROOT / "Tools/Evidence/reward-font-assets-audit.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"{len(report['checks'])} matching payloads; {len(report['differences'])} differences")
    return len(report["differences"])


if __name__ == "__main__":
    raise SystemExit(1 if run() else 0)
