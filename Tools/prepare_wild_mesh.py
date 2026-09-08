"""Flatten recovered weighted vertex arrays for Unity's native JsonUtility authoring input."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
destination = root / 'Artifacts/WildAuthoring'
destination.mkdir(parents=True, exist_ok=True)
for name in ('ef_wild3', 'ef_slwin3', 'ef_wild1_3'):
    data = json.loads((root / ('Tools/Evidence/Wild/' + name + '.json')).read_text(encoding='utf8'))
    for attachment in data['attachments']:
        if attachment.get('weighted'):
            attachment['counts'] = [len(v) for v in attachment['vertices']]
            attachment['influences'] = [w for v in attachment['vertices'] for w in v]
            del attachment['vertices']
    (destination / (name + '.json')).write_text(json.dumps(data, ensure_ascii=False), encoding='utf8')
print(destination)
