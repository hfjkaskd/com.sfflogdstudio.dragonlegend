"""Flatten weighted source vertices for native world-prefab authoring."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
data = json.loads((root / 'Tools/Evidence/ef_slzhuanchang.json').read_text(encoding='utf8'))
for attachment in data['attachments']:
    if attachment.get('weighted'):
        attachment['counts'] = [len(v) for v in attachment['vertices']]
        attachment['influences'] = [w for v in attachment['vertices'] for w in v]
        del attachment['vertices']
folder = root / 'Artifacts/TransitionAuthoring'
folder.mkdir(parents=True, exist_ok=True)
(folder / 'ef_slzhuanchang.json').write_text(json.dumps(data), encoding='utf8')
print(folder)
