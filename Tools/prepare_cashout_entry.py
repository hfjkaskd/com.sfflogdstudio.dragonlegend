"""Independently sample the original withdrawal icon for Unity geometry verification."""
import copy
import json
from pathlib import Path
import struct
import argparse
from sample_wild_reference import sample

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--name", default="ef_tixianicon")
parser.add_argument("--output", default="cashout-entry-poses.json")
args = parser.parse_args()
name = args.name
data = json.loads((root / f"Tools/Evidence/{name}.json").read_text(encoding="utf-8"))
assert all(a["kind"] == 0 for a in data["attachments"]), "Expected region-only icon"
rigs = []
for index, animation in enumerate(data["animations"]):
    times = {0, .173, animation["duration"]}
    for timeline in animation["timelines"]:
        for frame in timeline["frames"]:
            times.update((max(0, frame["time"] - .0001), frame["time"], frame["time"] + .0001))
    single = copy.deepcopy(data)
    single["animations"] = [animation]
    rigs.append({"name": name, "animation": index, "frames": [
        sample(single, struct.unpack("f", struct.pack("f", time))[0]) for time in sorted(times)]})
(root / "Tools/Evidence" / args.output).write_text(json.dumps({"rigs": rigs}), encoding="utf-8")
print(sum(len(rig["frames"]) for rig in rigs), "source geometry samples")
