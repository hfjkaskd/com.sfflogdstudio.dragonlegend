"""Flatten native Lucky Spin meshes and sample original animated geometry."""
import copy,json,struct
from pathlib import Path
from sample_wild_reference import sample

root=Path(__file__).resolve().parents[1]
data=json.loads((root/'Tools/Evidence/ef_slotsspin.json').read_text(encoding='utf8'))
output=root/'Artifacts/JackpotPopupAuthoring';output.mkdir(parents=True,exist_ok=True)
meshes=[]
for attachment in data['attachments']:
    if attachment['kind']!=2:continue
    mesh={k:attachment[k] for k in ('slot','key','uvs','triangles')}
    if attachment['weighted']:
        mesh['counts']=[len(v) for v in attachment['vertices']]
        influences=[v for group in attachment['vertices'] for v in group]
        mesh['vertices']=[x for v in influences for x in (v['x'],v['y'])]
        mesh['boneIndices']=[v['bone'] for v in influences]
        mesh['weights']=[v['weight'] for v in influences]
    else:mesh['vertices']=attachment['vertices']
    meshes.append(mesh)
(output/'ef_slotsspin.json').write_text(json.dumps({'meshes':meshes}),encoding='utf8')
rigs=[]
for index,animation in enumerate(data['animations']):
    times={0,.173,animation['duration']}
    for timeline in animation['timelines']:
        for frame in timeline['frames']:times.update((max(0,frame['time']-.0001),frame['time'],frame['time']+.0001))
    single=copy.deepcopy(data);single['animations']=[animation]
    rigs.append({'name':'ef_slotsspin','animation':index,'frames':[sample(single,struct.unpack('f',struct.pack('f',t))[0]) for t in sorted(times)]})
(root/'Tools/Evidence/lucky-spin-art-poses.json').write_text(json.dumps({'rigs':rigs}),encoding='utf8')
print(len(meshes),'meshes;',sum(len(r['frames']) for r in rigs),'source geometry samples')
