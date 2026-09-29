"""Switch only entry references, and audit geometry, alpha and scope."""
from pathlib import Path
import hashlib,json,re
from PIL import Image,ImageChops
ROOT=Path(__file__).resolve().parents[2]
E=ROOT/'Artifacts/MainSkin'
m=json.loads((E/'manifest.json').read_text(encoding='utf8'))
def text(p):return p.read_text(encoding='utf-8-sig')
def blocks(t):return re.findall(r'^--- !u!(\d+) &([^\n]+)\n(.*?)(?=^--- !u!|\Z)',t,re.M|re.S)
checks=[]
for item in m['copies']:
    src=ROOT/item['source'];dst=ROOT/item['output']
    assert dst.exists(),dst
    if item['kind']=='texture':
        a=Image.open(src).convert('RGBA');b=Image.open(dst).convert('RGBA')
        assert a.size==b.size,(src,a.size,b.size)
        assert ImageChops.difference(a.getchannel('A'),b.getchannel('A')).getbbox() is None,src
        checks.append({'asset':item['output'],'dimensions':a.size,'alphaUnchanged':True})
    elif src.suffix=='.prefab':
        a=blocks(text(src));b=blocks(text(dst))
        assert [(c,i) for c,i,_ in a]==[(c,i) for c,i,_ in b],src
        for (c,i,body),(_,_,other) in zip(a,b):
            if c in ('1','4','224','223','222'):
                # References to equivalent cloned prefabs are the only permitted
                # differences in hierarchy, transforms, canvas or object records.
                for old,new in m['guidMap'].items():body=body.replace(old,new)
                assert body==other,(src,c,i)
        checks.append({'asset':item['output'],'hierarchyAndLayoutUnchanged':True,'objects':len(a)})

entry=ROOT/'Assets/Resources/Whitebox/GameEntry.prefab'
before=text(entry)
allowed_names={'MainBackground.prefab','BalancePanel.prefab','SpinPlayfield.prefab','CollectEntry.prefab'}
after=before
installed_guids=[]
for item in m['copies']:
    if Path(item['source']).name not in allowed_names:continue
    meta=text(ROOT/(item['source']+'.meta'));g=re.search(r'^guid: (\w+)',meta,re.M)[1]
    after=after.replace(g,m['guidMap'][g])
    installed_guids.append(m['guidMap'][g])
entry.write_text(after,encoding='utf8')
assert all(g in after for g in installed_guids)
for item in m['copies']:
    if item['kind']!='serialized':continue
    for field,value in re.findall(r'(atlasPath|spritePath|blurSpritePath): (.+)',text(ROOT/item['output'])):
        value=json.loads(value) if value.startswith('"') else value
        if not value.startswith('MainSkin/'):continue
        base=ROOT/'Assets/Resources'/value
        assert any(Path(str(base)+ext).exists() for ext in ('.png','.asset')), (field,value)
field=text(ROOT/'Assets/Resources/MainSkin/Assets/RecoveredUI/SpinPlayfield.prefab')
source_catalog=re.search(r'^guid: (\w+)',text(ROOT/'Assets/Resources/RecoveredSymbols/OriginalSymbolCatalog.asset.meta'),re.M)[1]
assert 'symbols: {fileID: 11400000, guid: '+source_catalog in field
assert 'baseSymbols: {fileID: 11400000, guid: '+m['guidMap'][source_catalog] in field
baseline=json.loads((E/'baseline.json').read_text())
allowed={'Assets/Resources/Whitebox/GameEntry.prefab','Assets/Whitebox/Runtime/RecoveredSpinPlayfield.cs'}
unexpected=[]
for path,digest in baseline.items():
    p=ROOT/path
    if path not in allowed and (not p.exists() or hashlib.sha256(p.read_bytes()).hexdigest()!=digest):unexpected.append(path)
assert not unexpected,unexpected
report={'checks':checks,'existingFilesChanged':sorted(allowed),'otherExistingAssetsUnchanged':True,
        'mainEntryReferencesInstalled':True,'freeCatalogUnchanged':True,'textureCount':sum(i['kind']=='texture' for i in m['copies'])}
(E/'audit.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print(json.dumps({'checks':len(checks),'unexpectedChanges':unexpected,'entrySwitched':after!=before}))
