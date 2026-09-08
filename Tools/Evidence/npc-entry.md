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
using the cached ShakeInitPos x/y fields (+0x1c8/+0x1cc). The native arguments are
duration 1.5, intensity 30 and frequency 30. DelayPlayDragonSound state machine 23d39a8
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

## Authored NPC artwork

BuildNpc.Save now creates RecoveredUI/Npc and the shared ef_long native rig prefab.
It uses the verified constraint program, weighted source vertices, all four native
Animation clips and the unchanged 1981x1891 atlas. Texture preprocessing/compression
is disabled to preserve PMA source pixels. UI presentation uses the existing native
RecoveredRegionRig; no Spine assembly is present.

The Npc root retains QiPan's bottom-center anchor, position (-.003418,652), size
(1080,770.28). LongSpine retains position (0,355), size (990,1245.0002), pivot
(.47777772,.3566265). FireSpine retains its PlayFire parent stretched with position
(.0034179688,335.14), sizeDelta (0,1149.72), and its child position
(-.0034179688,19.859985), original size/pivot. Both original layers are 0. Fire is
inactive initially, matching InitNpc(0). This authored fragment still needs insertion
at the corresponding main-view hierarchy and sorting location.

The source atlas sequences use mode 1 (play once, hold final frame). Independent
sample_npc_geometry.py combines the source constraint matrices, weighted meshes and
that frame-selection rule to generate 32 full geometry references. It passes these
matrices to sample_wild_reference without changing its existing default behavior.

Named animation event fields are retained in RecoveredRigAnimation.events. The
visual sampler does not dispatch gameplay callbacks: InitNpc's identified calls do
not provide an event handler. Nonempty event audio paths remain rejected by this
authoring path pending explicit support. JsonUtility represents the source null audio
path as empty, so the check accepts that empty representation. Full audio event data
remains preserved in the offline source conversion.

RecoveredNpcArtTests renders fresh idle/wind/fire captures from the actual prefab,
checks its two-layer layout and preserved huo event, and exercises native one-shot
completion with scaled pause. The existing independent geometry test now includes
npc-geometry.json and checks every vertex in all four clips. NPC state-controller,
delayed dragon sound/shake and actual Bonus integration are still pending.

Validation: Artifacts/npc-art-tests.xml passed 226/226 PlayMode tests. Latest
current-npc-idle.png, current-npc-wind.png and current-npc-fire.png were inspected
at original resolution. All 32 full vertex fixtures pass. Warmed-up animation and
constraint sampling takes 31.5-39.4 ms per 1000 poses with 0 managed bytes allocated;
this measurement does not include Canvas mesh rebuild/render costs.

## NPC state controller

RecoveredNpcPresentation now configures the two authored layers for native states
0/1/2; other values are no-ops. State 0 loops the body idle and hides fire. States
1 and 2 play body win once alongside fire/wind respectively. Body completion hides
the effect, restores idle, then calls the supplied continuation. Replacing the body
animation clears the previous completion callback through RecoveredRegionAnimator.

Every state 1/2 call independently schedules the scaled .2-second dragon sound and
another .6-second wait. Only state 2 requests the shake after that second wait. A
subsequent Show(0) does not cancel these jobs: the original DelayPlayDragonSound
UniTask is fire-and-forget and has no InitNpc cancellation token. Overlapping calls
preserve their separate delays while only the latest body-animation callback survives.
Full component disable or explicit Cancel does cancel pending lifetime work, stops
its shake, and clears effect presentation. Sound is currently exposed through the
existing-style SoundRequested boundary; actual common audio playback remains pending.

BuildNpc authors the existing RecoveredBoardShake against the NPC's QiPan fragment
with duration 1.5, intensity/frequency 30 and the recovered falloff. Integration must
initialize its cached origin at main-view initialization and target the same full
QiPan used by Wild shakes. The component uses the existing shared shake owner, so a
new shake supersedes an older shake without an intermediate position reset.

RecoveredNpcPresentationTests checks scaled pause, .2-second sound, .8-second shake,
position restoration, callback-after-idle, invalid states, interrupted completion,
surviving delayed work, overlapping calls, disable and explicit cancellation. This
does not yet connect NPC playback to actual CheckBonusGame or instantiate UIBonusView.

Validation: Artifacts/npc-controller-tests.xml passed 227/227 PlayMode tests.
The full suite includes current NPC rendering and all source geometry checks.
