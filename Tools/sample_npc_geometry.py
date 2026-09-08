"""Combine independent source matrices, weighted vertices and atlas frame selection."""
import copy,json,struct,sys
from pathlib import Path
sys.dont_write_bytecode=True
from sample_wild_reference import sample
root=Path(__file__).resolve().parents[1]
data=json.loads((root/'Tools/Evidence/ef_long.json').read_text(encoding='utf8'))
reference=json.loads((root/'Tools/Evidence/npc-constraint-poses.json').read_text(encoding='utf8'))
def f32(x):return struct.unpack('<f',struct.pack('<f',x))[0]
rigs=[]
for index,animation in enumerate(data['animations']):
    frames=[]
    for reference_frame in reference['frames']:
        if reference_frame['clip']!=animation['name']:continue
        time=reference_frame['time'];d=copy.deepcopy(data);d['animations']=[d['animations'][index]]
        for a in d['attachments']:
            sequence=a.get('sequence')
            if not sequence:continue
            selected=sequence['setupIndex']
            for t in animation['timelines']:
                if t['domain']!='sequence' or t['index']!=a['slot'] or t['attachment']!=a['key']:continue
                for frame in t['frames']:
                    if frame['time']>time:continue
                    assert frame['mode'] in (0,1,2)
                    selected=frame['index']
                    if frame['mode']==2:selected=(selected+int(f32(f32(f32(time-frame['time'])/frame['delay'])+.00001)))%sequence['count']
                    if frame['mode']==1:selected=min(sequence['count']-1,selected+int(f32(f32(f32(time-frame['time'])/frame['delay'])+.00001)))
            a['path']=(a['path'] or a['name'])+str(sequence['start']+selected).zfill(sequence['digits'])
        values=reference_frame['matrices'];matrices=[values[i:i+6] for i in range(0,len(values),6)]
        frames.append(sample(d,time,world_matrices=matrices))
    rigs.append({'name':'ef_long','animation':index,'frames':frames})
(root/'Tools/Evidence/npc-geometry.json').write_text(json.dumps({'rigs':rigs},indent=2),encoding='utf8')
print('NPC geometry reference frames:',sum(len(r['frames']) for r in rigs))
