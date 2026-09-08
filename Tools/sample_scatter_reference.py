"""Sample every Scatter clip directly from the original binary-derived data."""
import copy,json,struct
from pathlib import Path
from sample_wild_reference import sample
root=Path(__file__).resolve().parents[1]
data=json.loads((root/'Tools/Evidence/Symbols/ef_scatter.json').read_text(encoding='utf8'))
rigs=[]
for animation in data['animations']:
    times={0,.173,.55,animation['duration']}
    for timeline in animation['timelines']:
        for frame in timeline['frames']:
            times.update((max(0,frame['time']-.0001),frame['time'],frame['time']+.0001))
    d=copy.deepcopy(data);d['animations']=[animation]
    rigs.append({'name':animation['name'],'frames':[sample(d,struct.unpack('f',struct.pack('f',t))[0]) for t in sorted(times)]})
(root/'Tools/Evidence/scatter-world-samples.json').write_text(json.dumps({'rigs':rigs},indent=2),encoding='utf8')
print('Scatter source geometry frames:',sum(len(r['frames']) for r in rigs))
