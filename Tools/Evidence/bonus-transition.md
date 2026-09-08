# Bonus entry and transition: native evidence

Source root: reconstruction/mumu-current. This records newly verified dependencies
for the actual post-symbol branch; it does not claim runtime integration is done.

## CheckBonusGame (23c6d54)

- Predicate 23bf8d8 is x > 1. Compare the filtered count to the entire BonusArea
  count, including empty lists; an already-set IsBonusGame flag also triggers.
- SetTaskData(5,1), clear IsBonusGame, replace BonusArea with five zeroes, save.
  RecoveredPlayerProgress.PrepareBonusGame already implements this data portion.
- InitNpc(2), PauseMusic, PlaySound1("ring"), scaled wait 1.8 seconds.
- StopSound1, PlaySound("transform"), create a completion source, and launch
  GManager.PlayZhuanChang with two distinct callbacks without awaiting its task.
- First callback 23c2e30 opens UIBonusView with the completion source in window
  context +0x18. CheckBonusGame awaits this source, not transition completion.
- Second callback 23bf6c8 refreshes bonus coins and selects/plays "bonusBg".
- After the Bonus window resolves the source, wait another scaled .5 seconds.
  Only then can this stage return to the next main-flow stage.

SDK Track calls remain outside the reconstruction scope, using the current policy.

## GManager.PlayZhuanChang (23715b4)

The entry wrapper is 2371398. The state machine stores event_action (+0x40) and
compele_action (+0x48), initializes its SkeletonAnimation, clears tracks, starts
"animation" on track 0 with loop=false, activates it and subscribes Complete.

It independently waits .8 seconds, invokes event_action, waits another 2.2 seconds,
then invokes compele_action. These are scaled waits. The Complete handler 2371468
deactivates the animation object and removes its subscription; it does not invoke
either of those callbacks. The source animation lasts 3.333333492 seconds, so its
visual lifetime and the callback at elapsed 3 seconds must remain separate.

Resolved using ELF RELA addends and analysis/script.json (Ghidra pointer addresses):

| Pointer | Metadata/string address | Meaning |
| --- | --- | --- |
| 0501e640 | 05046120 | predicate 23bf8d8 |
| 0501e648 | 050462a8 | open-window callback 23c2e30 |
| 0501e658 | 05038898 | refresh/music callback 23bf6c8 |
| 0501e520 | 050389a8 | ShowWindow<UIBonusView> |
| 0501d258 | 05063170 | ring |
| 0501d5b8 | 050643f8 | transform |
| 0501e400 | 0505e670 | bonusBg |
| 0501be48 | 0505e010 | animation |

Native float constants at Ghidra 00ebc554, 00ebc474 and 00ebc51c decode as
1.799999952, .800000012 and 2.200000048 respectively (ELF RVA subtract 0x100000).

## Visual source and verification

Scenes/Main.unity GManager references component fileID 26 on GameObject 2. This is
a world SkeletonAnimation, not SkeletonGraphic/UI. Its skeleton asset GUID is
5b746973f4cd8a14baa03177e83ba9b6, with scale .01. The component uses scaled time,
timeScale 1, PMA vertex colors, zSpacing 0 and multiple submeshes. Its serialized
loop=true is overridden by PlayZhuanChang's explicit non-looping SetAnimation call.

Res/Spine/zhuanchang/ef_slzhuanchang.skel.bytes converts completely: 33,937 bytes,
114 bones, 100 attachments (96 regions and 4 meshes), one animation, 285 timelines.
Bone inheritance modes are 0/1/3, setup shear is zero, and slots use normal/additive
blending. Native world Mesh/Animation conversion must preserve these distinctions.

Tools/sample_transition_reference.py produces 11 independent geometry samples,
including both sides of .8 and 3 seconds and the exact float32 clip endpoint.
Tools/verify_wild_extraction.py now passes 13 full source conversions and verifies
input bytes stay unchanged. These are offline conversion checks, not Unity visual
tests. The converted data and fixtures are ready for a world-prefab implementation;
NPC animation, transition prefab and actual Bonus window integration remain pending.
