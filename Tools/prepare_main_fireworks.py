"""Prepare native Main fireworks meshes and independently sampled original geometry."""
import copy
import json
import struct
from pathlib import Path
from sample_wild_reference import sample

root = Path(__file__).resolve().parents[1]
author = root / 'Artifacts/MainFireworksAuthoring'
author.mkdir(parents=True, exist_ok=True)
rigs = []
for name in ('ef_slyanhua',):
    data = json.loads((root / f'Tools/Evidence/{name}.json').read_text(encoding='utf8'))
    meshes = []
    for a in data['attachments']:
        if a['kind'] != 2:
            continue
        m = {k: a[k] for k in ('slot', 'key', 'uvs', 'triangles')}
        if a['weighted']:
            influences = [v for group in a['vertices'] for v in group]
            m.update(counts=[len(v) for v in a['vertices']],
                     vertices=[x for v in influences for x in (v['x'], v['y'])],
                     boneIndices=[v['bone'] for v in influences],
                     weights=[v['weight'] for v in influences])
        else:
            m['vertices'] = a['vertices']
        meshes.append(m)
    (author / f'{name}.json').write_text(json.dumps({'meshes': meshes}), encoding='utf8')
    for index, animation in enumerate(data['animations']):
        times = {0, .033333335, .173, .55, .95, 1.27, 1.99, animation['duration']}
        for timeline in animation['timelines']:
            for frame in timeline['frames']:
                times.update((max(0, frame['time'] - .0001), frame['time'], frame['time'] + .0001))
        d = copy.deepcopy(data)
        d['animations'] = [d['animations'][index]]
        frames = [sample(d, struct.unpack('f', struct.pack('f', t))[0]) for t in sorted(times)]
        rigs.append({'name': name, 'animation': index, 'frames': frames})
(root / 'Tools/Evidence/main-fireworks-poses.json').write_text(json.dumps({'rigs': rigs}, indent=2), encoding='utf8')
print('Main fireworks:', len(rigs), 'clips,', sum(len(r['frames']) for r in rigs), 'source geometry frames')
