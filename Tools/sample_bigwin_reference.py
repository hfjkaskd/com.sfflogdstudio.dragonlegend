"""Source-only popup geometry, including weighted meshes and looping atlas sequences."""
import copy,json,struct,sys
from pathlib import Path
sys.dont_write_bytecode=True
from sample_wild_reference import sample
root=Path(__file__).resolve().parents[1]
folder=root/'Tools/Evidence'
def f32(x):return struct.unpack('f',struct.pack('f',x))[0]
rigs=[]
for name in ('ef_wintanchuang',):
    data=json.loads((folder/(name+'.json')).read_text())
    for animationIndex,animation in enumerate(data['animations']):
        frames=[]
        for time in (0,.033333335,.173,.55,.95,1.27,1.99,animation['duration']):
            time=f32(time) # Unity samples float32 time, including exact attachment boundaries.
            d=copy.deepcopy(data);d['animations']=[d['animations'][animationIndex]]
            for attachment in d['attachments']:
                seq=attachment.get('sequence')
                if not seq:continue
                index=seq['setupIndex']
                for t in d['animations'][0]['timelines']:
                    if t['domain']!='sequence' or t['index']!=attachment['slot'] or t['attachment']!=attachment['key']:continue
                    for frame in t['frames']:
                        if frame['time']>time:continue
                        assert frame['mode']==2
                        index=(frame['index']+int(f32(f32(f32(time-frame['time'])/frame['delay'])+.00001)))%seq['count']
                attachment['path']=(attachment['path'] or attachment['name'])+str(seq['start']+index).zfill(seq['digits'])
            frames.append(sample(d,time))
        rigs.append({'name':name,'animation':animationIndex,'frames':frames})
(folder/'bigwin-poses.json').write_text(json.dumps({'rigs':rigs},indent=2))
print('Popup source geometry frames:',sum(len(r['frames']) for r in rigs))
