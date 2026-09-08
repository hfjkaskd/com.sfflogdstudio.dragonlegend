"""Flatten both Free special rigs and independently sample every source clip."""
import copy,json,struct
from pathlib import Path
from sample_wild_reference import sample

root=Path(__file__).resolve().parents[1]
output=root/'Artifacts/FreeSymbolAuthoring'
output.mkdir(parents=True,exist_ok=True)
samples=[]
for name in ('ef_jinbi','ef_longzhu'):
    data=json.loads((root/'Tools/Evidence/FreeSymbols'/f'{name}.json').read_text(encoding='utf8'))
    if name=='ef_jinbi':
        # SkeletonGraphic with no startingAnimation displays the authored setup pose.
        data['animations'].append({'name':'__setup','duration':0,'timelines':[]})
    flattened=copy.deepcopy(data)
    for attachment in flattened['attachments']:
        if attachment.get('weighted'):
            attachment['counts']=[len(v) for v in attachment['vertices']]
            attachment['influences']=[w for v in attachment['vertices'] for w in v]
            del attachment['vertices']
    (output/f'{name}.json').write_text(json.dumps(flattened,ensure_ascii=False),encoding='utf8')
    for animation in data['animations']:
        times={0,.173,animation['duration']}
        for timeline in animation['timelines']:
            for frame in timeline['frames']:
                times.update((max(0,frame['time']-.0001),frame['time'],frame['time']+.0001))
        single=copy.deepcopy(data);single['animations']=[animation]
        samples.append({'name':name+'/'+animation['name'],
            'frames':[sample(single,struct.unpack('f',struct.pack('f',time))[0]) for time in sorted(times)]})
(root/'Tools/Evidence/free-symbol-world-samples.json').write_text(json.dumps({'rigs':samples},separators=(',',':')),encoding='utf8')
print('Source geometry frames:',sum(len(r['frames']) for r in samples))
