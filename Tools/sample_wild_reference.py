"""Compute numeric pose references directly from the extracted binary, independently of Unity assets.

Only the two persistent Wild3 skeletons are covered here. The clipped light is separate.
"""
import json
import math
from pathlib import Path

root = Path(__file__).resolve().parents[1]

def transform(m, p):
    a,b,c,d,x,y = m
    return [a*p[0]+b*p[1]+x,c*p[0]+d*p[1]+y]

def matrix(parent, mode, pose):
    rotation,x,y,sx,sy = pose
    r = math.radians(rotation); co,si = math.cos(r),math.sin(r)
    a,b,c,d,_,_ = parent
    px,py = transform(parent,(x,y))
    if mode == 0:
        return [(a*co+b*si)*sx,(-a*si+b*co)*sy,(c*co+d*si)*sx,(-c*si+d*co)*sy,px,py]
    if mode == 1:
        return [co*sx,-si*sy,si*sx,co*sy,px,py]
    assert mode in (3,4)
    vx,vy = a*co+b*si,c*co+d*si
    length = math.hypot(vx,vy); factor = 1/length if length>.00001 else length
    vx*=factor;vy*=factor
    handed = -1 if mode==3 and a*d-b*c<0 else 1
    return [vx*sx,-vy*handed*sy,vy*sx,vx*handed*sy,px,py]

def evaluate(frames,time,component,percent=False):
    index = max((i for i,f in enumerate(frames) if f['time']<=time),default=-1)
    if index<0:return None
    f=frames[index]
    if index+1==len(frames):return 0 if percent else f['values'][component]
    n=frames[index+1];p0,p1=(0,1) if percent else (f['values'][component],n['values'][component])
    if f.get('curve',0)==1:return p0
    points=[(f['time'],p0)]
    if f.get('curve',0)==2:
        x0,y0,x1,y1=f['bezier'][0 if percent else component]['values']
        for j in range(1,10):
            t=j/10;u=1-t
            points.append((u**3*f['time']+3*u*u*t*x0+3*u*t*t*x1+t**3*n['time'],u**3*p0+3*u*u*t*y0+3*u*t*t*y1+t**3*p1))
    points.append((n['time'],p1))
    for (x0,y0),(x1,y1) in zip(points,points[1:]):
        if time<=x1:return y0+(y1-y0)*(time-x0)/(x1-x0)
    return p1

def sample(data,time):
    pose=[b['values'][:5] for b in data['bones']]
    active=[s['attachment'] for s in data['slots']]
    deforms={}
    for t in data['animations'][0]['timelines']:
        frames=t['frames']
        if t['domain']=='bone':
            for c in range(len(frames[0]['values'])):
                v=evaluate(frames,time,c)
                if v is None:continue
                component=0 if t['kind']==0 else c+1 if t['kind']==1 else c+3
                base=data['bones'][t['index']]['values'][component]
                pose[t['index']][component]=base*v if t['kind']==4 else base+v
        elif t['domain']=='slot' and t['kind']==0:
            for f in frames:
                if f['time']<=time:active[t['index']]=f['attachment']
        elif t['domain']=='deform':
            index=max((i for i,f in enumerate(frames) if f['time']<=time),default=-1)
            if index<0:continue
            values=frames[index]['values']
            if index+1<len(frames):
                alpha=evaluate(frames,time,0,True)
                values=[a+(b-a)*alpha for a,b in zip(values,frames[index+1]['values'])]
            deforms[t['index'],t['attachment']]=values
    matrices=[]
    for b,p in zip(data['bones'],pose):
        matrices.append(matrix([1,0,0,1,0,0] if b['parent']<0 else matrices[b['parent']],b['mode'],p))
    vertices=[]
    for slot,key in enumerate(active):
        if key is None:continue
        a=next(a for a in data['attachments'] if a['slot']==slot and a['key']==key)
        deformation=deforms.get((slot,key))
        if a['kind']==0:
            r=next(r for r in data['regions'] if r['name']==(a['path'] or a['name']))
            bx,by,w,h=r['bounds'];ox,oy,ow,oh=r['offsets'];rotation,x,y,sx,sy,aw,ah=a['values']
            left=ox*aw/ow-aw/2;bottom=oy*ah/oh-ah/2;right=left+w*aw/ow;top=bottom+h*ah/oh
            local=matrix([1,0,0,1,0,0],0,[rotation,x,y,sx,sy])
            points=[transform(local,p) for p in [(left,bottom),(left,top),(right,top),(right,bottom)]]
            points=[transform(matrices[data['slots'][slot]['bone']],p) for p in points]
        elif a['weighted']:
            points=[];index=0
            for influences in a['vertices']:
                point=[0,0]
                for w in influences:
                    x,y=w['x'],w['y']
                    if deformation is not None:x+=deformation[index];y+=deformation[index+1]
                    p=transform(matrices[w['bone']],(x,y))
                    point=[point[i]+p[i]*w['weight'] for i in range(2)];index+=2
                points.append(point)
        else:
            values=a['vertices'] if deformation is None else deformation
            points=[transform(matrices[data['slots'][slot]['bone']],p) for p in zip(values[::2],values[1::2])]
        vertices.extend(v/100 for point in points for v in point)
    return {'time':time,'vertices':vertices}

samples=[]
for name in ('ef_wild3','ef_slwin3'):
    data=json.loads((root/('Tools/Evidence/Wild/'+name+'.json')).read_text(encoding='utf8'))
    times=(0,.173,.73,1.5,2.91,3) if name=='ef_wild3' else (0,.173,.5,.63)
    samples.append({'name':name,'frames':[sample(data,t) for t in times]})
destination=root/'Tools/Evidence/wild-world-samples.json'
destination.write_text(json.dumps({'rigs':samples},indent=2),encoding='utf8')
print(destination)
