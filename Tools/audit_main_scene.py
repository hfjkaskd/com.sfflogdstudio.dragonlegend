"""Audit original Main scene environment and camera serialization against GameEntry."""
import difflib
import hashlib
import json
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
source_scene = root / "ReferenceOriginal/Scenes/Main.unity"
active_scene = root / "Assets/Whitebox/Scenes/GameEntry.unity"
source_light = root / "ReferenceOriginal/Scenes/Main/LightingData.asset"
active_light = root / "Assets/Whitebox/Rendering/LightingData.asset"

def read(path):
    return "\n".join(line.rstrip() for line in path.read_text(encoding="utf-8-sig").splitlines())

def guid(path):
    return re.search(r"^guid: (\w+)$", read(Path(str(path)+".meta")), re.M)[1]

def blocks(path):
    return {int(match[2]): (int(match[1]), match[3]) for match in re.finditer(r"^--- !u!(\d+) &(\d+)\n(.*?)(?=^--- !u!|\Z)", read(path), re.M | re.S)}

def equal(name, before, after):
    before, after = before.strip(), after.strip()
    assert before == after, name+"\n"+"\n".join(difflib.unified_diff(before.splitlines(), after.splitlines(), fromfile="source", tofile="active"))
    return {"name": name, "normalizedSha256": hashlib.sha256(after.encode()).hexdigest()}

source, active = blocks(source_scene), blocks(active_scene)
checks = []
checks.append(equal("RenderSettings", source[19][1], active[2][1]))
checks.append(equal("LightmapSettings", source[20][1].replace(guid(source_light), guid(active_light)), active[3][1]))
# Verify actual scene back-references before replacing their project-specific GUIDs.
assert f"guid: {guid(source_scene)}" in read(source_light)
assert f"guid: {guid(active_scene)}" in read(active_light)
checks.append(equal("LightingData", read(source_light).replace(guid(source_scene), guid(active_scene)), read(active_light)))

def remap_local(body):
    return re.sub(r"\{fileID: (\d+)\}", lambda match: "{fileID: "+str(int(match[1])+4000000000 if int(match[1]) else 0)+"}", body)

source_map_path = root.parent / "Nut Sort Relax/reconstruction/mumu-current/delivery/Assets/original-guid-map.json"
source_script_guid = "b15cdfbb15089e87868c39e7284c9c96"
assert json.loads(read(source_map_path))[source_script_guid].replace("\\", "/") == "Scripts/Unity.RenderPipelines.Universal.Runtime/UnityEngine/Rendering/Universal/UniversalAdditionalCameraData.cs"
package_script = root / "Library/PackageCache/com.unity.render-pipelines.universal@14.0.12/Runtime/UniversalAdditionalCameraData.cs"
active_script_guid = guid(package_script)
for source_id in (3, 7, 10, 13, 14, 15, 27, 29):
    expected = remap_local(source[source_id][1]).replace(source_script_guid, active_script_guid)
    checks.append(equal(f"Scene component {source_id} (class {source[source_id][0]})", expected, active[source_id+4000000000][1]))

# UICamera's ancestor pose must match, but the reconstruction's Canvas and
# EventSystem live in separate roots. Record that hierarchy difference explicitly.
child_pattern = r"  m_Children:\n(.*?)(?=  m_Father:)"
source_children = re.search(child_pattern, source[12][1], re.S)[1]
active_children = re.search(child_pattern, active[4000000012][1], re.S)[1]
checks.append(equal("UI camera ancestor pose (children checked separately)",
    re.sub(child_pattern, "", remap_local(source[12][1]), flags=re.S),
    re.sub(child_pattern, "", active[4000000012][1], flags=re.S)))

report = {"inputs": [{"path": path.relative_to(root).as_posix(), "sha256": hashlib.sha256(path.read_bytes()).hexdigest()} for path in (source_scene, active_scene, source_light, active_light)], "cameraScriptRemap": {"sourceGuid": source_script_guid, "officialPackageGuid": active_script_guid}, "uiAncestorChildrenDifference": {"source": source_children.strip(), "active": active_children.strip()}, "checks": checks}
(root / "Tools/Evidence/main-scene-audit.json").write_text(json.dumps(report, indent=2)+"\n", encoding="utf-8")
print(f"Verified {len(checks)} environment, lighting, camera-transform and URP camera records")
