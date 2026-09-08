"""Flatten recovered weighted vertex arrays for Unity's native JsonUtility authoring input."""
import json
import sys
from pathlib import Path

root = Path(__file__).resolve().parents[1]
symbols = '--symbols' in sys.argv
destination = root / ('Artifacts/SymbolAuthoring' if symbols else 'Artifacts/WildAuthoring')
destination.mkdir(parents=True, exist_ok=True)
for name in (('ef_qizizimu', 'ef_qizijinli', 'ef_qizibixi', 'ef_wild1', 'ef_slwin1', 'ef_scatter') if symbols else ('ef_wild3', 'ef_slwin3', 'ef_wild1_3')):
    data = json.loads((root / (('Tools/Evidence/Symbols/' if symbols else 'Tools/Evidence/Wild/') + name + '.json')).read_text(encoding='utf8'))
    for attachment in data['attachments']:
        if attachment.get('weighted'):
            attachment['counts'] = [len(v) for v in attachment['vertices']]
            attachment['influences'] = [w for v in attachment['vertices'] for w in v]
            del attachment['vertices']
    (destination / (name + '.json')).write_text(json.dumps(data, ensure_ascii=False), encoding='utf8')
print(destination)
