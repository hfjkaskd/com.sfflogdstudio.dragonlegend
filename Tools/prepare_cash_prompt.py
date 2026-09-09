"""Preserve original cash prompt mesh influences for the Unity rig author."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
data = json.loads((root / 'Tools/Evidence/ef_tixiantc.json').read_text(encoding='utf8'))
meshes = []
for attachment in data['attachments']:
    if attachment['kind'] != 2:
        continue
    mesh = {k: attachment[k] for k in ('slot', 'key', 'uvs', 'triangles')}
    if attachment['weighted']:
        mesh['counts'] = [len(v) for v in attachment['vertices']]
        influences = [v for group in attachment['vertices'] for v in group]
        mesh['vertices'] = [x for v in influences for x in (v['x'], v['y'])]
        mesh['boneIndices'] = [v['bone'] for v in influences]
        mesh['weights'] = [v['weight'] for v in influences]
    else:
        mesh['vertices'] = attachment['vertices']
    meshes.append(mesh)
output = root / 'Artifacts/CashPromptAuthoring'
output.mkdir(parents=True, exist_ok=True)
(output / 'ef_tixiantc.json').write_text(json.dumps({'meshes': meshes}), encoding='utf8')
print(len(meshes), 'meshes')
