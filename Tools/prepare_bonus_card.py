"""Prepare the already verified ef_jinbi binary data for native Bonus artwork."""
import copy,json,struct
from pathlib import Path
from sample_wild_reference import sample
root=Path(__file__).resolve().parents[1]
data=json.loads((root/'Assets/Whitebox/Editor/RecoveredCoinEffect.json').read_text(encoding='utf8'))
source=root/'Artifacts/BonusSource';source.mkdir(parents=True,exist_ok=True)
(source/'ef_jinbi.json').write_text(json.dumps(data,ensure_ascii=False),encoding='utf8')
meshes=[]
for a in data['attachments']:
    if a['kind']!=2:continue
    assert not a['weighted'], 'New weighted card mesh needs authoring conversion'
    meshes.append({k:a[k] for k in ('slot','key','vertices','uvs','triangles')})
author=root/'Artifacts/BonusAuthoring';author.mkdir(parents=True,exist_ok=True)
(author/'ef_jinbi.json').write_text(json.dumps({'meshes':meshes}),encoding='utf8')
rigs=[]
for index,animation in enumerate(data['animations']):
    # Include both sides of attachment changes and deform keys, plus between-key samples.
    times={0,.033333335,.173,.31,.49,.55,.71,.95,animation['duration']}
    for t in animation['timelines']:
        if t['domain']=='deform' or (t['domain']=='slot' and t['kind']==0):
            for f in t['frames']:
                times.update((max(0,f['time']-.0001),f['time'],f['time']+.0001))
    d=copy.deepcopy(data);d['animations']=[d['animations'][index]]
    frames=[sample(d,struct.unpack('f',struct.pack('f',t))[0]) for t in sorted(times)]
    rigs.append({'name':'ef_jinbi','animation':index,'frames':frames})
(root/'Tools/Evidence/bonus-card-poses.json').write_text(json.dumps({'rigs':rigs},indent=2),encoding='utf8')
print('Bonus:',len(data['animations']),'clips,',sum(len(r['frames']) for r in rigs),'source geometry frames')
