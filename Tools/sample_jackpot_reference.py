"""Generate jackpot geometry references from source JSON, independently of Unity assets."""
import copy
import json
import struct
import sys
from pathlib import Path

sys.dont_write_bytecode = True
from sample_wild_reference import sample

root = Path(__file__).resolve().parents[1]
folder = root / 'Tools/Evidence/Jackpot'

def f32(value):
    return struct.unpack('f', struct.pack('f', value))[0]

rigs = []
for tier in ('grand', 'major', 'minor'):
    data = json.loads((folder / ('ef_' + tier + 'icon.json')).read_text())
    for kind in ('idle', 'win'):
        frames = []
        for time in (0, .049999, .05, .173, .55, .95, 1.2, 1.99, 2):
            current = copy.deepcopy(data)
            animation = next(a for a in current['animations'] if a['name'] == kind)
            current['animations'] = [animation]
            for attachment in current['attachments']:
                if 'sequence' not in attachment:
                    continue
                sequence = attachment['sequence']
                index = sequence['setupIndex']
                for timeline in animation['timelines']:
                    if (timeline['domain'] != 'sequence' or timeline['index'] != attachment['slot']
                            or timeline['attachment'] != attachment['key']):
                        continue
                    for frame in timeline['frames']:
                        if frame['time'] <= time:
                            assert frame['mode'] == 1, 'Unexpected jackpot sequence mode'
                            elapsed = f32(f32(f32(time - frame['time']) / frame['delay']) + .00001)
                            index = min(sequence['count'] - 1, frame['index'] + int(elapsed))
                attachment['path'] = ((attachment['path'] or attachment['name'])
                                      + str(sequence['start'] + index).zfill(sequence['digits']))
            frames.append(sample(current, time))
        rigs.append({'name': tier, 'win': kind == 'win', 'frames': frames})

(folder / 'poses.json').write_text(json.dumps({'rigs': rigs}, indent=2))
print('Jackpot source poses:', sum(len(r['frames']) for r in rigs))
