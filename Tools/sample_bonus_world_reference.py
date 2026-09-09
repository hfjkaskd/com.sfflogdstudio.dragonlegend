"""Sample Bonus geometry from extracted source timelines, without Unity assets."""
import hashlib
import json
from pathlib import Path
from sample_wild_reference import sample

root = Path(__file__).resolve().parents[1]
source = root / 'Assets/Whitebox/Editor/RecoveredCoinEffect.json'
binary = root / 'ReferenceOriginal/Res/Spine/棋子/jinbi/ef_jinbi.skel.bytes'
data = json.loads(source.read_text(encoding='utf8'))
assert hashlib.sha256(binary.read_bytes()).hexdigest() == data['sha256']
rigs = []
for clip in data['animations']:
    selected = dict(data, animations=[clip])
    frames = [sample(selected, clip['duration'] * t) for t in (0, .173, .333, .5, .731, .917, 1)]
    rigs.append({'name': clip['name'], 'frames': frames})
output = root / 'Tools/Evidence/bonus-world-samples.json'
output.write_text(json.dumps({'source_sha256': hashlib.sha256(source.read_bytes()).hexdigest(),
                              'rigs': rigs}, separators=(',', ':')) + '\n', encoding='utf8')
print(f'{len(rigs)} clips, {sum(len(r["frames"]) for r in rigs)} source geometry samples')
