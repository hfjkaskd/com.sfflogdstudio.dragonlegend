"""Extract the original 4.1.24 coin effect; reject unsupported data, never skip it.
Format reference: https://github.com/EsotericSoftware/spine-runtimes/blob/4.1/spine-csharp/src/SkeletonBinary.cs
No Spine runtime or assembly is included in Unity.
"""
import struct,json,sys,hashlib
from pathlib import Path
if len(sys.argv)!=3:
 raise SystemExit('Usage: extract_coin_effect.py INPUT.skel.bytes OUTPUT.json (atlas is read beside INPUT)')
source=Path(sys.argv[1]).resolve();destination=Path(sys.argv[2]).resolve()
if destination.suffix.lower()!='.json' or destination.parent==source.parent:
 raise SystemExit('Output must be a .json outside the source asset directory')
b=Path(sys.argv[1]).read_bytes();p=8

def u8():
 global p
 v=b[p];p+=1;return v

def var():
 v=0
 for shift in range(0,35,7):
  c=u8();v|=(c&127)<<shift
  if c<128:return v
 raise ValueError('varint')

def string():
 global p
 n=var()
 if n==0:return None
 n-=1;v=b[p:p+n].decode('utf-8');p+=n;return v

def num():
 global p
 v=struct.unpack_from('>f',b,p)[0];p+=4;return v

def color():return [u8()/255 for _ in range(4)]

def sequence(attachment):
 present=u8();assert present in (0,1),'sequence boolean'
 if present:attachment['sequence']={'count':var(),'start':var(),'digits':var(),'setupIndex':var()}

def ref():
 n=var();return strings[n-1] if n else None

def zero(label):
 value=var();assert value==0,(label,value,p)

result={'sha256':hashlib.sha256(b).hexdigest(),'version':string(),'bounds':[num() for _ in range(4)]}
assert result['version']=='4.1.24'
assert u8()==1
result.update(fps=num(),images=string(),audio=string())
strings=[string() for _ in range(var())]
result['bones']=[]
for i in range(var()):
 d={'name':string(),'parent':var() if i else -1,'values':[num() for _ in range(8)],'mode':var(),'skin':u8(),'color':color()}
 assert d['mode'] in range(5) and d['skin']==0 and d['values'][5:7]==[0,0], d
 result['bones'].append(d)
result['slots']=[]
for i in range(var()):result['slots'].append({'name':string(),'bone':var(),'color':color(),'dark':color(),'attachment':ref(),'blend':var()})
zero('ik')
constraints=[]
for i in range(var()):
 d={'name':string(),'order':var(),'skin':u8(),'bones':[var() for _ in range(var())],'target':var(),'local':u8(),'relative':u8(),'offsets':[num() for _ in range(6)],'mix':[num() for _ in range(6)]}
 constraints.append(d)
if constraints:result['transformConstraints']=constraints
zero('path')
result['attachments']=[]
for i in range(var()):
 slot=var()
 for j in range(var()):
  key=ref();name=ref();kind=u8()
  d={'slot':slot,'key':key,'name':name or key,'kind':kind}
  if kind==0:
   d.update(path=ref(),values=[num() for _ in range(7)],color=color())
   sequence(d)
  elif kind==2:
   d.update(path=ref(),color=color());n=var();d['uvs']=[num() for _ in range(n*2)]
   def shorts():
    return [(u8()<<8)|u8() for _ in range(var())]
   d['triangles']=shorts();d['weighted']=bool(u8());d['vertices']=[]
   if d['weighted']:
    for v in range(n):d['vertices'].append([{'bone':var(),'x':num(),'y':num(),'weight':num()} for _ in range(var())])
   else:d['vertices']=[num() for _ in range(n*2)]
   d['hull']=var();sequence(d)
   d['edges']=shorts();d['size']=[num(),num()]
  elif kind==6:
   d['endSlot']=var();n=var();d['weighted']=bool(u8());d['vertices']=[]
   if d['weighted']:
    for v in range(n):d['vertices'].append([{'bone':var(),'x':num(),'y':num(),'weight':num()} for _ in range(var())])
   else:d['vertices']=[num() for _ in range(n*2)]
   d['color']=color()
  else:raise ValueError(('unsupported attachment',kind,key,p))
  result['attachments'].append(d)
zero('extra skins');zero('events')

def timeline(domain,index,kind,count):
 d={'domain':domain,'index':index,'kind':kind,'frames':[]}
 if domain=='slot' and kind==0:
  d['frames']=[{'time':num(),'attachment':ref()} for _ in range(count)];return d
 dimensions=({1:4,2:3,5:1} if domain=='slot' else {0:1,1:2,2:1,3:1,4:2,5:1,6:1})[kind]
 var() # number of allocated Bezier channels
 values=(lambda:[u8()/255 for _ in range(dimensions)]) if domain=='slot' else (lambda:[num() for _ in range(dimensions)])
 frame={'time':num(),'values':values()}
 for i in range(count):
  d['frames'].append(frame)
  if i==count-1:break
  following={'time':num(),'values':values()};curve=u8();assert curve<=2
  frame['curve']=curve
  if curve==2:frame['bezier']=[{'values':[num() for _ in range(4)]} for _ in range(dimensions)]
  frame=following
 return d
result['animations']=[]
for _ in range(var()):
 a={'name':string(),'timelines':[]};expected=var()
 for domain in ['slot','bone']:
  for i in range(var()):
   index=var()
   for j in range(var()):
    kind=u8();count=var();a['timelines'].append(timeline(domain,index,kind,count))
 for label in ['ik animation','transform animation','path animation']:zero(label)
 for skin in range(var()):
  assert var()==0,'non-default deform skin'
  for si in range(var()):
   slot=var()
   for ai in range(var()):
    key=ref();kind=u8();count=var();assert kind in (0,1),'attachment timeline'
    if kind==1:
     t={'domain':'sequence','index':slot,'attachment':key,'kind':kind,'frames':[]}
     for f in range(count):
      time=num();packed=struct.unpack_from('>i',b,p)[0];p+=4
      mode=packed&15;assert mode<=6,'sequence mode'
      t['frames'].append({'time':time,'mode':mode,'index':packed>>4,'delay':num()})
     a['timelines'].append(t);continue
    var();time=num()
    mesh=next(m for m in result['attachments'] if m['slot']==slot and m['key']==key)
    assert mesh['kind'] in (2,6)
    length=2*sum(len(v) for v in mesh['vertices']) if mesh['weighted'] else len(mesh['vertices'])
    t={'domain':'deform','index':slot,'attachment':key,'kind':kind,'frames':[]}
    for f in range(count):
     values=[0.0]*length;n=var()
     if n:
      start=var();assert start+n<=length
      values[start:start+n]=[num() for _ in range(n)]
     if not mesh['weighted']:values=[x+y for x,y in zip(values,mesh['vertices'])]
     frame={'time':time,'values':values};t['frames'].append(frame)
     if f+1<count:
      time=num();curve=u8();assert curve<=2;frame['curve']=curve
      if curve==2:frame['bezier']=[{'values':[num() for _ in range(4)]}]
    a['timelines'].append(t)
 for label in ['draw order','event animation']:zero(label)
 assert expected==len(a['timelines']),(expected,len(a['timelines']))
 a['duration']=max(f['time'] for t in a['timelines'] for f in t['frames']);result['animations'].append(a)
assert p==len(b),(p,len(b))
regions=[]
atlas_name=Path(sys.argv[1]).name.removesuffix('.skel.bytes')+'.atlas.txt'
for line in Path(sys.argv[1]).with_name(atlas_name).read_text().splitlines():
 if ':' not in line and not line.endswith('.png'):
  region={'name':line,'rotate':0};regions.append(region)
 elif regions:
  key,value=line.split(':',1);region[key]=[int(v) for v in value.split(',')] if key in ['bounds','offsets'] else int(value)
for region in regions:
 if 'offsets' not in region:region['offsets']=[0,0,region['bounds'][2],region['bounds'][3]]
result['regions']=regions
Path(sys.argv[2]).write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print('Consumed',p,'bytes;',[(a['name'],a['duration'],len(a['timelines'])) for a in result['animations']])
