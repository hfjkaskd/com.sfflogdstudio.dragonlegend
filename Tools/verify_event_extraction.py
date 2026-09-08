"""Exercise event defaults, signed values, string overrides and audio payloads."""
import json
import struct
import subprocess
import sys
import tempfile
from pathlib import Path

def var(n):
    result = bytearray()
    while n > 127:
        result.append((n & 127) | 128)
        n >>= 7
    return bytes(result + bytes([n]))

def signed(n): return var((n << 1) ^ (n >> 31))
def string(s):
    if s is None: return var(0)
    b = s.encode('utf8')
    return var(len(b) + 1) + b
def num(n): return struct.pack('>f', n)

root = Path(__file__).resolve().parents[1]
with tempfile.TemporaryDirectory(prefix='dragon-events-') as temp:
    folder = Path(temp)
    source_folder = folder / 'source'
    source_folder.mkdir()
    source = source_folder / 'events.skel.bytes'
    # Minimal valid 4.1.24 skeleton with one bone and two event definitions.
    b = bytes(8) + string('4.1.24') + bytes(16) + b'\x01' + num(30) + string('') + string('')
    b += var(2) + string('silent') + string('audio') + var(1) + string('root')
    b += b''.join(num(v) for v in (0,0,0,1,1,0,0,0)) + bytes(2) + b'\xff'*4
    b += bytes(6)  # slots, IK, transform, path, default skin, extra skins
    b += var(2)
    b += var(1) + signed(-2147483648) + num(-.25) + string('default') + string(None)
    b += var(2) + signed(2147483647) + num(1.5) + string('sound') + string('tone') + num(.7) + num(-.2)
    b += var(1) + string('events') + var(1) + bytes(7) + var(2)
    b += num(.5) + var(0) + signed(-7) + num(-2) + b'\x00'
    b += num(1) + var(1) + signed(42) + num(3) + b'\x01' + string('override') + num(.4) + num(.8)
    source.write_bytes(b)
    source.with_name('events.atlas.txt').write_text('events.png\n', encoding='utf8')
    output = folder / 'events.json'
    subprocess.run([sys.executable, '-B', str(root/'Tools/extract_coin_effect.py'), str(source), str(output)], check=True)
    data = json.loads(output.read_text(encoding='utf8'))
    assert [e['int'] for e in data['events']] == [-2147483648, 2147483647]
    assert data['events'][0]['audio'] is None
    assert abs(data['events'][1]['volume'] - .7) < 1e-6
    frames = data['animations'][0]['timelines'][0]['frames']
    assert frames[0] == {'time': .5, 'event': 0, 'int': -7, 'float': -2., 'string': 'default'}
    assert frames[1]['string'] == 'override' and frames[1]['int'] == 42
    assert abs(frames[1]['volume'] - .4) < 1e-6 and abs(frames[1]['balance'] - .8) < 1e-6
    assert data['animations'][0]['duration'] == 1
    assert source.read_bytes() == b
print('PASS: signed event integers, defaults, overrides, optional audio and complete input consumption')
