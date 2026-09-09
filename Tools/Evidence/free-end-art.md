# Free end presentation: source artwork and missing window consumer

The next missing Main continuation is CheckFreeSpinEnd 23ca10c. Its existing
RecoveredFreeSpinExit state object is not a window implementation. The actual
source window is UIFreeSpinEndView (BaseWindow, not PopupWindow), and its art is
`ReferenceOriginal/Res/Spine/overtc/ef_overtc.skel.bytes` plus the matching atlas.

The binary parser initially rejected bone timeline kind 7. This was an actual
missing shear channel, not corrupt source data. The parser now consumes all
10,811 bytes: 24 bones, 19 slots, 17 attachments; idle 2 seconds/27 timelines and
start 1 second/25 timelines. The one shear timeline is on normal-inheritance bone
`suiziy4` during start, with shear Y 0 -> -8.598007 -> 18.422997 -> -8.598007 -> 0.
Its original component Beziers must be retained. Removing the channel changes
the visible ornament deformation.

Format identity was checked against the official 4.1
[SkeletonBinary constants and parser](https://raw.githubusercontent.com/EsotericSoftware/spine-runtimes/4.1/spine-csharp/src/SkeletonBinary.cs).
The independent X/Y axis angles follow the affine transform described by the
[original Bone transform implementation](https://raw.githubusercontent.com/EsotericSoftware/spine-runtimes/4.1/spine-csharp/src/Bone.cs).
No Spine assembly or third-party runtime was added to Unity.

The native Unity rig now has shear X/Y pose fields, AnimationCurve channels 5/6,
and affine axes for shear. The animator snapshots/restores seven setup values
when sampling, so switching from start to idle cannot retain the last shear.
Existing zero-shear assets keep their existing transform path. The source's
three NoScale bones continue using the existing inherited rotation/reflection
behavior. BuildFreeEndArt authors the source Rect size 1090.0714 x 1273.1414 and
pivot (.50049144,.49966273), original atlas regions, and both Animation clips.

`prepare_free_end.py` derives mesh data and 112 independent source geometry frames,
including key boundaries and nearby samples. The shared geometry test compares
every active vertex and checks 1,000 warmed-up samples allocate zero managed bytes.
The Free end artwork PlayMode test captures newly rendered start/idle frames,
checks scaled pause and one-shot/loop behavior, and checks setup shear restoration.
These tests cover artwork, not complete window layout or Main Free lifecycle.

Validation: `Artifacts/free-end-art-all.xml` passed 346/346 PlayMode tests with
graphics enabled. ef_overtc idle/start took 6.9971/7.3815 ms per 1,000 warmed-up
pose samples and allocated 0 managed bytes (pose sampling only, not full rendering).
Both newly generated `current-free-end-art-start.png` and
`current-free-end-art-idle.png` were visually inspected in this run.

## Window behavior to connect next

- OnInitProperty 23afcdc: ELF dbb670 contains (300,2).
- OnBeforeShow 23afcfc: play `fsend`, retain completion source and initial spin
  count, hide TipTxt, clear TotalTxt to empty, hide CtnBtn, start PlayEndAnim,
  then format the tip with initial count. ELF string relocation 04f1dd68 gives
  `IN <material="#003815_3"><gradient="cash">{0}</gradient></material> FREE SPINS`.
- PlayEndAnim 23b04d4: play `start` once, wait .8 scaled seconds (ELF dbc474,
  timing 8), then play `idle` looping. This deliberately switches before the
  one-second start clip ends. Set SpineRect active: that field references the
  separate ef_xjpl confetti node, not ef_overtc. Append a callback to a new sequence.
- Callback 23b011c: play `count`, read current TotalFreeSpinWin at this time,
  start a .5 default-OutQuad tween from constant zero, show TipTxt immediately.
  Setter 23b03ac formats two currency decimals. This does not credit any money.
- Tween completion 23b03e4 invokes PlayBtnAnim(CtnBtn,.5). Native 2370318 hides
  the button, appends .5 interval, then its activation/reset-scale callback and
  a .3 default-OutQuad scale-to-one tween. Audit callback 2370c6c before wiring.
- OnClickButton 23b0020 recognizes `ContinueBtn`, plays click and calls BaseWindow
  Hide. OnAfterHide 23b00ec completes the retained source. Do not substitute the
  FreeStart PopupWindow's Back scale animation without BaseWindow evidence.
- Main checks remaining FreeSpinCount for exact zero. Zero changes GameSlotType
  to Base before pausing music and showing this window with the initial count.
  It awaits the window source, plays transform, then launches the transition.
  Transition callbacks separately reset view/init Base reels and clear the Free
  end flag/change music. Nonzero delegates to FreeAutoSpin.

The complete window prefab/controller, collector-to-exit connection and production
GameEntry board/mode/entry/exit consumers remain required. SDK handling is unchanged.
