"""Verify real Dragon Legend binary extraction, including the Wild clipping attachment.

Usage: python Tools/verify_wild_extraction.py REVERSE_PROJECT_ROOT
No Unity runtime is involved; this checks the offline source-data conversion only.
"""
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile

project = Path(__file__).resolve().parents[1]
reverse = Path(sys.argv[1]).resolve() / 'reconstruction/mumu-current'
source = reverse / 'reference-unity/ExportedProject/Assets/Res/Spine'
extractor = project / 'Tools/extract_coin_effect.py'
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
files = list(source.rglob('*.skel.bytes'))
by_hash = {sha(p): p for p in files}
goldens = list((project / 'Tools/Evidence/Wild').glob('*.json'))
goldens += list((project / 'Tools/Evidence/Symbols').glob('*.json'))
goldens += [project / ('Assets/Whitebox/Editor/' + name + '.json') for name in
            ('RecoveredCoinEffect', 'RecoveredLampFlash', 'RecoveredWinBurst')]
with tempfile.TemporaryDirectory(prefix='dragon-wild-extraction-') as temp:
    for golden in goldens:
        expected = json.loads(golden.read_text(encoding='utf-8-sig'))
        binary = by_hash[expected['sha256']]
        atlas = binary.with_name(binary.name.removesuffix('.skel.bytes') + '.atlas.txt')
        before = sha(binary), sha(atlas)
        output = Path(temp) / golden.name
        subprocess.run([sys.executable, '-X', 'utf8', str(extractor), str(binary), str(output)], check=True)
        assert json.loads(output.read_text(encoding='utf8')) == expected, golden
        assert before == (sha(binary), sha(atlas)), 'Input assets were modified'
    # A mistaken third positional argument must fail before reading or writing an atlas.
    invalid = subprocess.run([sys.executable, str(extractor), str(binary), str(atlas), str(output)], capture_output=True)
    assert invalid.returncode != 0 and b'Usage:' in invalid.stderr
    invalid = subprocess.run([sys.executable, str(extractor), str(binary), str(atlas)], capture_output=True)
    assert invalid.returncode != 0 and b'Output must' in invalid.stderr
    assert before == (sha(binary), sha(atlas))

light = json.loads((project / 'Tools/Evidence/Wild/ef_wild1_3.json').read_text(encoding='utf8'))
clip = next(a for a in light['attachments'] if a['kind'] == 6)
assert (clip['slot'], clip['endSlot'], clip['weighted'], len(clip['vertices'])) == (1, 24, False, 8)
deform = next(t for t in light['animations'][0]['timelines'] if t['domain'] == 'deform' and t['attachment'] == 'qty')
assert len(deform['frames']) == 1 and len(deform['frames'][0]['values']) == 8
assert abs(light['animations'][0]['duration'] - .8) < .000001

# Cross-check the reference atlases with the separately exported original TextAssets.
for name in ('ef_wild3', 'ef_slwin3'):
    atlas = next(source.rglob(name + '.atlas.txt'))
    raw = next((reverse / 'assets/exported/TextAsset').glob(name + '.atlas__*.bytes'))
    assert atlas.read_bytes() == raw.read_bytes(), 'Reference atlas differs from raw TextAsset'
print('PASS: eleven full binary conversions, clipping/deform payload, input guards, original atlas bytes')
