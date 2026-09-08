"""Prepare native NPC weighted mesh authoring without modifying source evidence."""
import json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
data=json.loads((root/'Tools/Evidence/ef_long.json').read_text(encoding='utf8'))
meshes=[]
for a in data['attachments']:
    if a['kind']!=2:continue
    m={k:a[k] for k in ('slot','key','uvs','triangles')}
    if a['weighted']:
        influences=[v for group in a['vertices'] for v in group]
        m.update(counts=[len(v) for v in a['vertices']],vertices=[x for v in influences for x in (v['x'],v['y'])],
                 boneIndices=[v['bone'] for v in influences],weights=[v['weight'] for v in influences])
    else:m['vertices']=a['vertices']
    meshes.append(m)
folder=root/'Artifacts/NpcAuthoring';folder.mkdir(parents=True,exist_ok=True)
(folder/'ef_long.json').write_text(json.dumps({'meshes':meshes}),encoding='utf8')
print(folder)
