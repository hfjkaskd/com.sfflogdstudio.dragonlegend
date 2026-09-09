"""Record original APK font-atlas bytes used by the imported-texture regression."""
import base64
import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
reverse = root.parent / "Nut Sort Relax/reconstruction/mumu-current"
inventory = json.loads((reverse / "assets/inventory.json").read_text(encoding="utf-8"))
matches = [entry for entry in inventory if entry.get("type") == "Texture2D" and
           entry.get("name") == "QuorumStd-Black_zitidi.com Atlas"]
assert len(matches) == 2
records = []
for entry in matches:
    path = reverse / entry["tree"]
    data = json.loads(path.read_text(encoding="utf-8"))
    payload = base64.b64decode(data["image data"]["data"], validate=True)
    assert data["image data"]["encoding"] == "base64"
    assert len(payload) == data["m_CompleteImageSize"] == 1024 * 1024
    assert data["m_TextureFormat"] == 1 and data["m_MipCount"] == 1
    records.append({"inventory": entry, "sourceSha256": hashlib.sha256(path.read_bytes()).hexdigest(),
                    "texture": {key: value for key, value in data.items() if key != "image data"},
                    "rawBytes": len(payload), "rawSha256": hashlib.sha256(payload).hexdigest()})
assert records[0]["rawSha256"] == records[1]["rawSha256"] == "2a4211a1f2d98d05cf095cdaddcac7a1ea1abb78fe40bb0ff887634585b89464"
(root / "Tools/Evidence/reward-atlas-native-audit.json").write_text(json.dumps(records, indent=2) + "\n", encoding="utf-8")
print("Both original APK atlas objects contain the same 1048576-byte Alpha8 payload")
