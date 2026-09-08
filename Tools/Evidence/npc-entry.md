# NPC entry dependency

UIMainView.InitNpc (23bc788) references LongSpine at +0xe0 and FireSpine at +0xe8.
The original UIMainView prefab references wrapper fileIDs 114129116114662800 and
114714397710475260 respectively. Their GameObjects are 1634108722742392 and
1354824845356902. This is a SkeletonGraphic presentation, distinct from the world
transition recovered in bonus-transition.md.

Native branches:

- 0: play LongSpine idle looping; hide FireSpine.
- 1: show FireSpine and play win_penhuo once; schedule DelayPlayDragonSound(null);
  play LongSpine win once with completion callback 23c35d8.
- 2 (Bonus): show FireSpine and play win_feng once; schedule DelayPlayDragonSound
  with callback 23c368c; play LongSpine win once with completion callback 23c371c.
- Other values return without changing presentation.

Callback 23c371c hides FireSpine, returns LongSpine to looping idle, then invokes
the caller's completion. Callback 23c368c requests a shake of ShakeNode (+0x78),
using the existing native strength/duration fields (+0x1c8/+0x1cc). Its exact shake
implementation still needs integration. DelayPlayDragonSound state machine 23d39a8
waits scaled .2 seconds, plays sound "dragon", waits another scaled .6 seconds,
then invokes its optional callback. These waits are independent of NPC completion.

Verified ELF relocation mappings:

| Pointer | Resolved metadata/string | Meaning |
| --- | --- | --- |
| 0501e230 | 05046330 | 23c35d8 |
| 0501e238 | 05046338 | 23c368c |
| 0501e240 | 05046340 | 23c371c |
| 0501cf58 | 05060c30 | idle |
| 0501e248 | 05064ff8 | win_penhuo |
| 0501d328 | 05064fe0 | win |
| 0501e250 | 05064ff0 | win_feng |

The complete ef_long.skel.bytes conversion consumes 168,272 bytes: 156 bones,
402 region/mesh attachments, and four clips each lasting 3.000000238 seconds.
Timeline counts are idle 68, win 73, win_feng 228 and win_penhuo 244. A single
event definition "huo" has zero integer/float defaults, empty string and no audio.
win_penhuo contains it at .766666710 seconds. Parsing this event does not imply
InitNpc registers a gameplay handler for it; that must be established separately.

The extractor now preserves event definitions and event timelines, including signed
integers, string-default selection and optional audio fields. Format layout was
checked against [SkeletonBinary 4.1](https://raw.githubusercontent.com/EsotericSoftware/spine-runtimes/4.1/spine-csharp/src/SkeletonBinary.cs).
No external runtime code or assembly is added to Unity. verify_event_extraction.py
checks signed extrema, default/override strings and audio values using a minimal
binary fixture. verify_wild_extraction.py passes all 14 complete original binary
conversions, including ef_long, and verifies source bytes are unchanged.

## Required constraint work before native rendering

The source contains six transform constraints, ordered 0 through 5. Constraint 0
targets bone 36 from bone 37 in absolute-local mode, with x offset -1120 and
translation/scale mix -.5. Constraints 1 through 5 target bone 37 from bones
25,26,27,28,24 in absolute-world mode, translation mixes .02,.04,.05,.06,-.02.
Exact offsets and all values are retained in ef_long.json. The existing native
relative-local fish constraint implementation does not cover this graph.

Preserve ordered constraint and descendant updates with reusable runtime buffers;
do not silently drop the constraints or substitute unconstrained poses. Validate
against an independent source sampler before authoring the NPC prefab. Baking every
mesh frame would raise asset size/loading memory and would not preserve arbitrary
sample times. NPC prefab and actual Bonus entry remain unimplemented at this point.

## Ordered native constraint evaluation

RecoveredNpcConstraints now evaluates the source's absolute-local translation/scale
and absolute-world translation operations from authored steps. RecoveredRegionRig
can select this ordered path when supplied with the NPC program; other rigs retain
their existing path. Animation input bone values remain unchanged between samples.
The implementation deliberately covers this verified graph, not arbitrary transform
constraints: offsets for scale, rotation mix and shear mix are all zero here, and
the only local operation reads previously unconstrained siblings.

Tools/sample_npc_constraints.py independently constructs the update schedule from
source parents and ordered constraints, following native SortTransformConstraint
3f9af04 and SortReset 3f9b3e0. It produces 166 operations for 156 bones and six
constraints. In particular, the final constraint on bone 24 invalidates descendant
bones 25..28; these are recomputed later from animation input, rather than retaining
all four earlier hair translations. A simple one-pass apply-all-constraints approach
would differ. The script rejects unhandled graph/constraint parameters.

32 reference frames cover every matrix of all 156 bones across all four clips.
The native scale expression is (current + (target-current)*mix)/current when both
current and mix are nonzero. It is not ordinary scale interpolation. Zero current
scale skips the division; negative mix is not clamped or skipped. Exact instruction
evidence from original ELF RVAs 3fb4cd0 and 3fb538c, with ELF SHA256 and first bytes,
is recorded in npc-constraint-arm64.txt. The upstream format/algorithm comparison
uses [TransformConstraint 4.1](https://raw.githubusercontent.com/EsotericSoftware/spine-runtimes/4.1/spine-csharp/src/TransformConstraint.cs)
and [Skeleton 4.1](https://raw.githubusercontent.com/EsotericSoftware/spine-runtimes/4.1/spine-csharp/src/Skeleton.cs).

RecoveredNpcConstraintTests compares 32 complete matrix sets within .003 source
pixels, checks scale division/zero guard explicitly, and measures allocations after
warmup. This verifies constraint arithmetic and order, not the NPC's rendered
attachments, atlas sequences, layout or full Bonus integration. Those still require
prefab authoring and fresh runtime visual checks.

Validation: Artifacts/npc-constraint-tests.xml passed 224/224 PlayMode tests.
1000 warmed-up NPC constraint poses took 31.14 ms with 0 allocated managed bytes.
No NPC visual-equivalence claim is made by this arithmetic-only validation.
