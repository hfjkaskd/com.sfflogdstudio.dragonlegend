"""Independent per-vertex poses for all eight original winning effects and their shared frame."""
import json
from pathlib import Path
from sample_wild_reference import sample
root=Path(__file__).resolve().parents[1]
rigs=[]
for name,file,clip in [('A','ef_qizizimu','a_idle'),('10','ef_qizizimu','10_idle'),('J','ef_qizizimu','j_idle'),('Q','ef_qizizimu','q_idle'),('K','ef_qizizimu','k_idle'),('Yu','ef_qizijinli','idle'),('Gui','ef_qizibixi','idle'),('Wild1','ef_wild1','idle'),('Win1','ef_slwin1','animation')]:
    data=json.loads((root/'Tools/Evidence/Symbols'/f'{file}.json').read_text('utf8'))
    animation=next(a for a in data['animations'] if a['name']==clip)
    data['animations']=[animation]
    rigs.append({'name':name,'frames':[sample(data,t) for t in (0,.173,.3333333432674408,.5,animation['duration'])]})
(root/'Tools/Evidence/symbol-world-samples.json').write_text(json.dumps({'rigs':rigs},indent=2),encoding='utf8')
