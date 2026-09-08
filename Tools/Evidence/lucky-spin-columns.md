# Actual Slot reward display columns

UILuckySpinView.StartLuckySpin MoveNext 0x23b6960 uses its supplied reward / 100,
invariant float formatting and per-character digit parsing (non-digits become
10). It does not call the separate LuckySpinResult.GenerateResult class. The
new numeric columns support the actual window's path, not that unused generator.

RecoveredLuckySpinColumn ports the running column implementation:

- Init 0x23b3f1c cancels tracked motion, clears flags/speed, activates the five
  texts unless dot-only, sets the base number and restores anchored Y to zero.
  It does not overwrite TargetDigit. Dot-only Init returns before repositioning.
- Reposition 0x23b4074 uses modulo 11 only for columns 1/2 and renders 10 as a
  decimal point. Other columns use modulo 10. Each text is at (0,height*index).
- MoveOneFrame 0x23b45e8 subtracts distance and wraps while Y is strictly less
  than -height. Move uses modulo 10 for columns 0/3, modulo 11 otherwise.
- StartSpin 0x23b4268 accelerates with Ease.InSine=2. Setter 0x23b4830 both sets
  current speed and moves by speed*deltaTime. Its completion starts the constant
  coroutine immediately, including that coroutine's first movement step.
- StopRoll 0x23b4490 records the bounce consumer, then dot-only columns complete
  synchronously. Others set the stopping flag and wait while IsSpinning.
- Constant loop 0x23b4940 checks the stop flag before moving, then starts
  DecelerateAndSnap 0x23b4a44. Distance is Y+height*steps+.5. A matching digit
  with abs(Y)>.5 needs a full cycle. Distances below 3 reset immediately.
- Other snap distances use duration Clamp(distance*1.5/currentSpeed,.05,.3),
  Ease.OutCubic=9 and incremental distance movement. IsStopped is set before
  this tween, while IsSpinning remains true. Snap completion 0x23b48dc resets
  Y=0. The following coroutine continuation starts an untracked bounce sequence,
  invokes bounce callback, clears speed and IsSpinning. Existing completion
  waiters therefore resume before the bounce settles.
- Bounce is Y=35 over .12 with OutQuad=6, then zero over .08 with InQuad=5.
  StopImmediate 0x23b45a4 cancels tracked motion and restores Y, while existing
  completion waiters and untracked bounce sequences retain their native roles.

ELF constants: 0xdbbd58=10000, 0xdbbd5c=.08, 0xdbc564=.05,
0xdbc4c8=.12, 0xdbc5f0=.08. The serialized item height overrides the constructor:
the actual UILuckySpinView prefab has 296, width 151 and five texts per column.
BuildLuckySpinColumns extracts all four original subtrees without recreating
their static layout. It remaps only component/font references and authors the
native motion parameters. Original RectMask2D, Green bitmap font, text sizing,
anchors, pivots and column positions remain in the resulting prefabs.

These are UI numeric reward displays; the main gameplay symbols remain world
objects. Runtime uses standard Unity components/coroutines and cached text
strings/RectTransforms. No external animation assembly or SDK change is added.

Pending: the complete Slot window and art, entry pulse, reward digit formatting,
window sequencing/shake/reward-popup integration and production Free flow.
Whole-window visual parity and inactive-window lifecycle behavior are not
established by the column tests.

Validation: Artifacts/lucky-spin-columns-tests.xml reports 309/309 PlayMode tests passed with graphics enabled in Unity 2022.3.62f3. All reachable digit targets, decimal columns, paused acceleration, exact first acceleration step and immediate-stop waiter behavior are exercised. Fresh Artifacts/current-lucky-spin-columns.png was inspected at original 1080x480 size: Green bitmap digits and interior decimal glyphs are visible. This is a column capture, not a full Slot window capture.
