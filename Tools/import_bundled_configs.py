"""Import verified native bundled configs, preserving encoded source evidence."""
import base64
import hashlib
import json
from pathlib import Path
import sys

source = Path(sys.argv[1]) / 'reconstruction/mumu-current'
project = Path(__file__).resolve().parent.parent
output = project / 'Assets/StreamingAssets/RecoveredConfig/Bundled'
output.mkdir(parents=True, exist_ok=True)
records = []
for name in ('GoldenDragon_default', 'GoldenDragon_organic'):
    path = source / f'delivery/UnityFramework/ReferenceOriginal/Res/Config/{name}.txt'
    encoded = path.read_bytes()
    outer = base64.b64decode(encoded.decode('utf-8-sig'))
    key = b'GoldenDragon'
    decoded = base64.b64decode(bytes(value ^ key[i % len(key)] for i, value in enumerate(outer)))
    data = json.loads(decoded)
    reference = source / f'config/{name}.json'
    assert data == json.loads(reference.read_text(encoding='utf-8-sig')), name
    (output / f'{name}.encoded.txt').write_bytes(encoded)
    destination = output / f'{name}.json'
    if destination.exists():
        assert json.loads(destination.read_text(encoding='utf-8-sig')) == data, name
    else:
        destination.write_bytes(decoded)
    records.append({'name': name, 'source': str(path),
                    'encoded_sha256': hashlib.sha256(encoded).hexdigest(),
                    'decoded_sha256': hashlib.sha256(decoded).hexdigest(),
                    'snapshot_sha256': hashlib.sha256(destination.read_bytes()).hexdigest(),
                    'reference_json_sha256': hashlib.sha256(reference.read_bytes()).hexdigest()})
(project / 'Tools/Evidence/bundled-config-import.json').write_text(
    json.dumps(records, indent=2) + '\n', encoding='utf-8')
print('Decoded and verified both bundled configurations against extracted JSON.')
