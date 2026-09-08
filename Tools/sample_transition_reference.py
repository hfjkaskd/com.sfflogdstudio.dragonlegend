"""Independent source poses at transition callback and animation boundaries."""
import json
import struct
import sys
from pathlib import Path

sys.dont_write_bytecode = True
from sample_wild_reference import sample

folder = Path(__file__).resolve().parent / 'Evidence'
data = json.loads((folder / 'ef_slzhuanchang.json').read_text(encoding='utf8'))
animation = data['animations'][0]
assert animation['name'] == 'animation'
assert len(data['bones']) == 114 and len(data['attachments']) == 100
assert sum(a['kind'] == 2 for a in data['attachments']) == 4
assert {b['mode'] for b in data['bones']} == {0, 1, 3}
assert {s['blend'] for s in data['slots']} == {0, 1}
times = (0, .033333335, .4, .799, .8, .801, 1.8, 2.999, 3, 3.001,
         animation['duration'])
frames = [sample(data, struct.unpack('<f', struct.pack('<f', t))[0]) for t in times]
(folder / 'transition-poses.json').write_text(json.dumps({
    'rigs': [{'name': 'ef_slzhuanchang', 'animation': 0, 'frames': frames}]
}, indent=2), encoding='utf8')
print(f'Transition source geometry frames: {len(frames)}')
