"""Recover compiled GLES source and blend-state evidence from original serialized Shader objects.
Usage: python Tools/extract_symbol_shader.py REVERSE_ROOT
"""
import hashlib,json,re,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
reverse=Path(sys.argv[1]).resolve()/'reconstruction/mumu-current'
sys.path.insert(0,str(reverse/'tools/python-libs'))
from UnityPy.helpers.CompressionHelper import decompress_lz4
source=reverse/'assets/objects/Shader/7b327b34e12c9f468cf54bbd7d1b2b5e_CAB-014000ed2538cb87d1aa4612be612002_-2506585663452162707.json'
data=json.loads(source.read_text('utf8'));blob=bytes(data['compressedBlob']);i=data['platforms'].index(9)
o=data['offsets'][i][0];n=data['compressedLengths'][i][0]
raw=decompress_lz4(blob[o:o+n],data['decompressedLengths'][i][0])
programs=[s.decode('ascii') for s in re.findall(rb'[\x09\x0a\x0d\x20-\x7e]{50,}',raw) if s.startswith(b'#ifdef VERTEX')]
assert len(programs)==2 and 'discard' not in programs[0] and 'discard' in programs[1]
assert all('u_xlat16_1 = u_xlat16_1 * vs_COLOR0;' in s for s in programs)
folder=root/'Tools/Evidence/SymbolShader';folder.mkdir(parents=True,exist_ok=True)
for index,program in enumerate(programs):(folder/f'gles-{index}.glsl').write_text(program,encoding='utf8')
state=data['m_ParsedForm']['m_SubShaders'][0]['m_Passes'][0]['m_State']
assert state['rtBlend0']['srcBlend']['val']==1 and state['rtBlend0']['destBlend']['val']==10
(folder/'provenance.json').write_text(json.dumps({'source':str(source.relative_to(reverse)),
    'sourceSha256':hashlib.sha256(source.read_bytes()).hexdigest(),'glesBlobSha256':hashlib.sha256(raw).hexdigest(),
    'shader':data['m_ParsedForm']['m_Name'],'state':state},indent=2),encoding='utf8')
print('PASS: original compiled GLES variants and One/OneMinusSrcAlpha blending recovered')
