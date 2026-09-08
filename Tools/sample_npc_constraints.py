"""Independent ordered bone/constraint reference from original ef_long data."""
import json
import struct
import sys
from pathlib import Path
sys.dont_write_bytecode = True
from sample_wild_reference import matrix, transform, evaluate

root = Path(__file__).resolve().parents[1]
data = json.loads((root/'Tools/Evidence/ef_long.json').read_text(encoding='utf8'))
bones = data['bones']; constraints = data['transformConstraints']
children = [[j for j,b in enumerate(bones) if b['parent']==i] for i in range(len(bones))]
ready = set(); steps = []
def require(i):
    if i < 0 or i in ready: return
    require(bones[i]['parent']);ready.add(i);steps.append(i)
def invalidate(i):
    for child in children[i]:
        if child in ready: invalidate(child)
        ready.discard(child)
for index,c in enumerate(constraints):
    assert c['order']==index and c['relative']==0 and c['skin']==0 and len(c['bones'])==1
    assert c['mix'][0]==c['mix'][5]==0 and c['offsets'][3:]==[0,0,0]
    bone=c['bones'][0];require(c['target']);require(bone)
    steps.append(-index-1);invalidate(bone);ready.add(bone)
for i in range(len(bones)):require(i)
assert steps.count(25)==2 and steps.count(28)==2, 'Parent constraint must reset the previously constrained descendants'
converted=[]
for c in constraints:
    if c['local']:
        assert c is constraints[0] and bones[c['bones'][0]]['parent']==bones[c['target']]['parent']
    else: assert c['mix'][3:]==[0,0,0]
    converted.append({'bone':c['bones'][0],'target':c['target'],'local':bool(c['local']),
        'offset':dict(zip(('x','y'),c['offsets'][1:3])),
        'translationMix':dict(zip(('x','y'),c['mix'][1:3])),
        'scaleMix':dict(zip(('x','y'),c['mix'][3:5]))})
frames=[]
for animation in data['animations']:
    for time in (0,.173,.5,.7666667103767395,1.1,1.8,2.9,animation['duration']):
        time=struct.unpack('<f',struct.pack('<f',time))[0]
        pose=[b['values'][:5] for b in bones]
        for t in animation['timelines']:
            if t['domain']!='bone':continue
            assert t['kind'] in (0,1,4)
            for component in range(len(t['frames'][0]['values'])):
                value=evaluate(t['frames'],time,component)
                if value is None:continue
                axis=0 if t['kind']==0 else component+1 if t['kind']==1 else component+3
                base=bones[t['index']]['values'][axis]
                pose[t['index']][axis]=base*value if t['kind']==4 else base+value
        world=[None]*len(bones)
        for step in steps:
            if step>=0:
                b=bones[step];world[step]=matrix([1,0,0,1,0,0] if b['parent']<0 else world[b['parent']],b['mode'],pose[step])
            else:
                c=constraints[-step-1];i=c['bones'][0];o,m=c['offsets'],c['mix'];p=pose[i][:]
                if c['local']:
                    target=pose[c['target']]
                    for axis in (1,2):p[axis]+=(target[axis]-p[axis]+o[axis])*m[axis]
                    for axis in (3,4):
                        if m[axis]!=0 and p[axis]!=0:p[axis]=(p[axis]+(target[axis]-p[axis]+o[axis])*m[axis])/p[axis]
                    b=bones[i];world[i]=matrix(world[b['parent']],b['mode'],p)
                else:
                    point=transform(world[c['target']],o[1:3])
                    for axis in (0,1):world[i][axis+4]+=(point[axis]-world[i][axis+4])*m[axis+1]
        frames.append({'clip':animation['name'],'time':time,'pose':[v for p in pose for v in p],
                       'matrices':[v for w in world for v in w]})
result={'parents':[b['parent'] for b in bones],'modes':[b['mode'] for b in bones],
        'program':{'constraints':converted,'steps':steps},'frames':frames}
(root/'Tools/Evidence/npc-constraint-poses.json').write_text(json.dumps(result,indent=2),encoding='utf8')
print(f'NPC: {len(steps)} ordered updates, {len(frames)} full bone matrix references')
