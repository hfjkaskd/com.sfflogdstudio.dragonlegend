"""Prepare isolated main-screen skin; never rewrites shared source artwork."""
from pathlib import Path
import hashlib,json,re,shutil,uuid

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/Resources/MainSkin'
EVIDENCE=ROOT/'Artifacts/MainSkin'
GENERATED=Path('C:/Users/pc/.codex/generated_images/01a08cb9-bacd-7211-96e0-abe0eb26bd8c')
INDEX=json.loads((ROOT/'Artifacts/main-skin-guid-index.json').read_text())
def read(p):return p.read_text(encoding='utf-8-sig')
def guid(p):return re.search(r'^guid: (\w+)',read(Path(str(p)+'.meta')),re.M)[1]
def fresh(p):return uuid.uuid5(uuid.NAMESPACE_URL,'dragonlegend-main-warm-v1/'+str(p.relative_to(OUT)).replace('\\','/')).hex
def meta(src,dst):
    text=read(Path(str(src)+'.meta'))
    Path(str(dst)+'.meta').write_text(re.sub(r'^guid: \w+', 'guid: '+fresh(dst),text,flags=re.M),encoding='utf8')

OUT.mkdir(parents=True,exist_ok=True);EVIDENCE.mkdir(parents=True,exist_ok=True)
baseline=EVIDENCE/'baseline.json'
if not baseline.exists():
    baseline.write_text(json.dumps({str(p.relative_to(ROOT)).replace('\\','/'):hashlib.sha256(p.read_bytes()).hexdigest() for p in (ROOT/'Assets').rglob('*') if p.is_file() and OUT not in p.parents},indent=2))
art='Assets/Resources/RecoveredArt/'
specs=[
 ('background',art+'Res/UI/zhujiemian/zjm_bg.png','404f8cbd-d774-498a-a482-4b1c770fc0e1'),
 ('board',art+'Res/UI/zhujiemian/zjm_bg_qipan.png','fc994935-e77d-4219-a4d7-5d3e6519984a'),
 ('top',art+'Res/UI/zhujiemian/zjm_bg_shang.png','2645e770-dd46-4fbf-8766-05bc1584c274'),
 ('dragon',art+'Res/Spine/long/ef_long.png','bf1a75c0-b6f6-41c3-b588-61288aeeeb86'),
 ('spin',art+'Res/Spine/spin/ef_slspineaniu.png','ff068b3b-27e3-46f1-aede-af70984038fb'),
 ('bank',art+'Res/Spine/yinhang/ef_slyinhang.png','32224828-4281-49ef-b140-26a5e063c4e6'),
 ('treasure',art+'Res/Spine/按钮/shoucang/ef_shoucangicon.png','bf4de95c-fd5a-4654-9447-3481a91e2d96'),
 ('grand',art+'Res/Spine/jackpot3小个/grand_icon/ef_grandicon.png','bbc951eb-de7e-4ec6-91fc-ad1ab83843c5'),
 ('major',art+'Res/Spine/jackpot3小个/major_icon/ef_majoricon.png','e8c6d9aa-3bc5-4ea8-83a8-b4cd5a0dc8aa'),
 ('minor',art+'Res/Spine/jackpot3小个/minor_icon/ef_minoricon.png','f7c874f9-a0ea-47ab-b8eb-a80bae5f8f24'),
 ('scatter',art+'Res/UI/font_img/scatter.png','b65913b6-3054-4032-8919-f6246410638a'),
 ('fish',art+'Res/UI/zhujiemian/qizi/yu.png','73a44538-2eeb-4d75-a6b5-8f57ef50bef0'),
 ('fishblur',art+'Res/UI/zhujiemian/qizi/yu_m.png','73a44538-2eeb-4d75-a6b5-8f57ef50bef0'),
 ('scatterblur',art+'Res/UI/zhujiemian/qizi/scatter_m.png','b65913b6-3054-4032-8919-f6246410638a'),
]
mapping={};paths={};manifest=[];copies=[]
for mode,source,generated in specs:
    src=ROOT/source;dst=OUT/'Textures'/f'{mode}.png';dst.parent.mkdir(parents=True,exist_ok=True)
    meta(src,dst);mapping[guid(src)]=fresh(dst)
    paths[str(src.relative_to(ROOT/'Assets/Resources').with_suffix('')).replace('\\','/')]='MainSkin/Textures/'+mode
    manifest.append(dict(mode=mode,source=source,generated=str(GENERATED/f'exec-{generated}.png'),output=str(dst.relative_to(ROOT)).replace('\\','/')))
    copies.append(dict(source=source,output=str(dst.relative_to(ROOT)).replace('\\','/'),kind='texture'))
(ROOT/'Tools/MainSkin/textures.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf8')

prefabs=['MainBackground','BalancePanel','SpinPlayfield','SpinButton','JackpotMeters','DownWinText','CollectEntry',
         'JackpotPopupArt/ef_long','JackpotPopupArt/ef_slyinhang','JackpotPopupArt/ef_shoucangicon']
sources=[ROOT/f'Assets/Resources/RecoveredUI/{n}.prefab' for n in prefabs]
sources += list((ROOT/'Assets/Resources/RecoveredUI/SpinButton').glob('*.asset'))
sources += [ROOT/'Assets/Resources/RecoveredArt/Res/UI/zhujiemian/zjm_bg_shang.asset',
            ROOT/'Assets/Resources/RecoveredUI/CoinRewardText/Green.asset',
            ROOT/'Assets/Resources/RecoveredUI/CoinRewardText/Green_Material.mat',
            ROOT/INDEX['71fce5925b6d578448b06d587acf8e25'],
            ROOT/'Assets/Resources/RecoveredSymbols/OriginalSymbolCatalog.asset',
            ROOT/'Assets/Resources/RecoveredSymbols/Sprites/scatter.asset',
            ROOT/'Assets/Resources/RecoveredSymbols/Sprites/yu.asset',
            ROOT/'Assets/Resources/RecoveredSymbols/Sprites/scatter_m.asset',
            ROOT/'Assets/Resources/RecoveredSymbols/Sprites/yu_m.asset']
for src in sources:
    relative=src.relative_to(ROOT/'Assets/Resources');dst=OUT/'Assets'/relative
    dst.parent.mkdir(parents=True,exist_ok=True);meta(src,dst);mapping[guid(src)]=fresh(dst)
    # Only main catalog paths point at new sharp artwork. Source/free catalog stays intact.
    # Scatter keeps the original blue sharp/blur sprites to match its animated rig.
    if src.name in ('yu.asset','yu_m.asset'):
        paths[str(relative.with_suffix('')).replace('\\','/')]=str(dst.relative_to(ROOT/'Assets/Resources').with_suffix('')).replace('\\','/')
    copies.append(dict(source=str(src.relative_to(ROOT)).replace('\\','/'),output=str(dst.relative_to(ROOT)).replace('\\','/'),kind='serialized'))

def remap(text):
    for before,after in mapping.items():text=text.replace(before,after)
    # Match complete field values; prefix substitution would corrupt blur names
    # and duplicate MainSkin when the destination contains the original path.
    def resource(match):
        value=match[2]
        decoded=json.loads(value) if value.startswith('"') else value
        return match[1]+paths.get(decoded, value)
    text=re.sub(r'((?:atlasPath|spritePath|blurSpritePath): )(.+)',resource,text)
    return text
for item in copies:
    if item['kind']!='serialized':continue
    src=ROOT/item['source'];dst=ROOT/item['output'];text=remap(read(src))
    if src.name=='#003815_3.mat':
        text=re.sub(r'_FaceColor: \{[^}]+\}','_FaceColor: {r: 1, g: 0.88, b: 0.58, a: 1}',text)
        text=re.sub(r'_OutlineColor: \{[^}]+\}','_OutlineColor: {r: 0.2, g: 0.035, b: 0.012, a: 1}',text)
    if src.name=='SpinPlayfield.prefab':
        original_catalog=guid(ROOT/'Assets/Resources/RecoveredSymbols/OriginalSymbolCatalog.asset')
        text=text.replace('  symbols: {fileID: 11400000, guid: '+mapping[original_catalog]+', type: 2}',
            '  symbols: {fileID: 11400000, guid: '+original_catalog+', type: 2}\n  baseSymbols: {fileID: 11400000, guid: '+mapping[original_catalog]+', type: 2}')
    if src.suffix=='.prefab':
        font_guid=mapping[guid(ROOT/'Assets/Resources/RecoveredUI/CoinRewardText/Green.asset')]
        mat_guid=mapping[guid(ROOT/'Assets/Resources/RecoveredUI/CoinRewardText/Green_Material.mat')]
        def text_material(match):
            body=match[0]
            if 'm_Font: {fileID: 12800000, guid: '+font_guid in body:
                body=body.replace('m_Material: {fileID: 0}',f'm_Material: {{fileID: 2100000, guid: {mat_guid}, type: 2}}')
            if 'm_fontColorGradient:' in body:
                def warm_color(c):
                    r,g,b=map(float,c.group(2,3,4))
                    if g>r and g>b:
                        return c[1]+('{r: 1, g: 0.96, b: 0.72, a: 1}' if 'top' in c[1] else '{r: 1, g: 0.72, b: 0.28, a: 1}')
                    return c[0]
                body=re.sub(r'((?:topLeft|topRight|bottomLeft|bottomRight): )\{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+), a: 1\}',warm_color,body)
            return body
        text=re.sub(r'^--- !u!114 &[^\n]+\n.*?(?=^--- !u!|\Z)',text_material,text,flags=re.M|re.S)
    dst.write_text(text,encoding='utf8')

# Explicit font material uses a cheap configurable UI palette shader. Metrics,
# glyph texture, font geometry and dynamic text remain identical to the source.
shader=OUT/'Rendering/WarmBitmap.shader';shader.parent.mkdir(parents=True,exist_ok=True)
shader_guid=fresh(shader)
Path(str(shader)+'.meta').write_text(f'fileFormatVersion: 2\nguid: {shader_guid}\nShaderImporter:\n  externalObjects: {{}}\n',encoding='utf8')
material=OUT/'Assets/RecoveredUI/CoinRewardText/Green_Material.mat'
t=read(material)
t=re.sub(r'm_Shader: \{[^}]+\}',f'm_Shader: {{fileID: 4800000, guid: {shader_guid}, type: 3}}',t)
t=t.replace('m_Colors: {}','m_Colors:\n      _WarmTint: {r: 1, g: 0.87, b: 0.48, a: 1}')
material.write_text(t,encoding='utf8')

(EVIDENCE/'manifest.json').write_text(json.dumps(dict(copies=copies,guidMap=mapping,pathMap=paths),indent=2,ensure_ascii=False),encoding='utf8')
shutil.copy2('C:/Users/pc/AppData/Local/Temp/codex-clipboard-d4b549dc-6368-42e4-b2aa-b48684de0bde.png',EVIDENCE/'approved-reference.png')
print(f'Prepared {len(specs)} texture imports and {len(sources)} isolated asset copies. Entry not switched yet.')
