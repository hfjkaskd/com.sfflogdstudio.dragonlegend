"""Validate the Bonus skeleton's input requirements before renderer conversion."""
import hashlib
import json
from collections import Counter
from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = root / 'Assets/Whitebox/Editor/RecoveredCoinEffect.json'
binary = root / 'ReferenceOriginal/Res/Spine/棋子/jinbi/ef_jinbi.skel.bytes'
data = json.loads(source.read_text(encoding='utf8'))
digest = hashlib.sha256(binary.read_bytes()).hexdigest()
assert digest == data['sha256'], 'Extracted data refers to a different skeleton'
attachments = {(a['slot'], a['key']): a for a in data['attachments']}
assert len(attachments) == len(data['attachments']), 'Duplicate attachment key'
for index, bone in enumerate(data['bones']):
    assert -1 <= bone['parent'] < index, 'Bone order requires a different evaluator'
    assert bone['mode'] in (0, 1, 3, 4) and not any(bone['values'][5:7])
for index, slot in enumerate(data['slots']):
    assert 0 <= slot['bone'] < len(data['bones']) and slot['blend'] in (0, 1)
    assert not slot['attachment'] or (index, slot['attachment']) in attachments
meshes = []
for a in attachments.values():
    assert a['kind'] in (0, 2), 'Attachment requires additional conversion support'
    assert not a.get('sequence', {}).get('count', 0), 'Sequence needs world support'
    if a['kind'] == 2:
        assert not a['weighted'], 'Review weighted deformation before migration'
        assert len(a['vertices']) == len(a['uvs']) and len(a['vertices']) % 2 == 0
        count = len(a['vertices']) // 2
        assert all(0 <= i < count for i in a['triangles'])
        meshes.append({'slot': a['slot'], 'key': a['key'], 'vertices': count})
clips = []
for clip in data['animations']:
    domains = Counter()
    for timeline in clip['timelines']:
        domain, kind = timeline['domain'], timeline['kind']
        assert (domain, kind) in {('slot', 0), ('slot', 1), ('bone', 0),
                                 ('bone', 1), ('bone', 4), ('deform', 0)}
        domains[f'{domain}/{kind}'] += 1
        frames = timeline['frames']
        assert frames and all(a['time'] <= b['time'] for a, b in zip(frames, frames[1:]))
        assert all(0 <= frame['time'] <= clip['duration'] + 1e-6 for frame in frames)
        if domain == 'deform':
            attachment = attachments[timeline['index'], timeline['attachment']]
            assert all(len(f['values']) == len(attachment['vertices']) for f in frames)
        elif domain == 'slot' and kind == 0:
            assert all(not f['attachment'] or
                       (timeline['index'], f['attachment']) in attachments for f in frames)
    clips.append({'name': clip['name'], 'duration': clip['duration'],
                  'timelines': dict(sorted(domains.items()))})
report = {'binary_sha256': digest, 'extraction_sha256': hashlib.sha256(source.read_bytes()).hexdigest(),
          'bones': len(data['bones']), 'slots': len(data['slots']),
          'attachments': len(attachments), 'meshes': meshes, 'clips': clips,
          'scope': 'Input compatibility only; does not prove rendered parity or complete migration.'}
output = root / 'Tools/Evidence/bonus-rig-source-audit.json'
output.write_text(json.dumps(report, indent=2) + '\n', encoding='utf8')
print(f"Bonus source verified: {len(clips)} clips, {len(attachments)} attachments, {len(meshes)} meshes")
