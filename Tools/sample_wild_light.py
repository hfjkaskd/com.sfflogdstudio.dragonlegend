"""Source-based clipped coverage and first-moment references for the original Wild3Light."""
import json
import sys
sys.dont_write_bytecode=True
from sample_wild_reference import root, sample

def cross(a,b):return a[0]*b[1]-a[1]*b[0]
def subtract(a,b):return [a[0]-b[0],a[1]-b[1]]
def clip_polygon(subject,boundary):
    sign=1 if sum(cross(a,b) for a,b in zip(boundary,boundary[1:]+boundary[:1]))>0 else -1
    for a,b in zip(boundary,boundary[1:]+boundary[:1]):
        if not subject:break
        result=[];edge=subtract(b,a)
        for p,q in zip(subject[-1:]+subject[:-1],subject):
            inside_p=sign*cross(edge,subtract(p,a))>=0
            inside_q=sign*cross(edge,subtract(q,a))>=0
            if inside_p!=inside_q:
                direction=subtract(q,p)
                parameter=cross(subtract(a,p),edge)/cross(direction,edge)
                result.append([p[i]+parameter*direction[i] for i in range(2)])
            if inside_q:result.append(q)
        subject=result
    return subject

def moment(points):
    area=mx=my=0
    for i in range(1,len(points)-1):
        a,b,c=points[0],points[i],points[i+1]
        weight=abs(cross(subtract(b,a),subtract(c,a)))/2
        area+=weight;mx+=weight*(a[0]+b[0]+c[0])/3;my+=weight*(a[1]+b[1]+c[1])/3
    return area,mx,my

data=json.loads((root/'Tools/Evidence/Wild/ef_wild1_3.json').read_text(encoding='utf8'))
frames=[]
for time in (0,.05,.1,.173,.25,.36,.5,.7,.8):
    records={r['slot']:r for r in sample(data,time,True)}
    boundary=None;end=-1;total=[0,0,0];unclipped=0
    for slot in range(len(data['slots'])):
        r=records.get(slot)
        if r:
            if r['clip']:boundary=r['points'];end=r['endSlot']
            else:
                for start in range(0,len(r['triangles']),3):
                    triangle=[r['points'][i] for i in r['triangles'][start:start+3]]
                    unclipped+=moment(triangle)[0]
                    m=moment(clip_polygon(triangle,boundary) if boundary else triangle)
                    total=[a+b for a,b in zip(total,m)]
        if slot==end:boundary=None
    frames.append({'time':time,'area':total[0],'momentX':total[1],'momentY':total[2],'unclippedArea':unclipped})
destination=root/'Tools/Evidence/wild-light-samples.json'
destination.write_text(json.dumps({'frames':frames},indent=2),encoding='utf8')
print(destination)
for f in frames:print(f['time'],f['area'],f['unclippedArea'])
