# Base and Free stop shake binding

The reel controllers already emitted ShakeRequested, but no production subscriber
started a shake. CoreRoundFlow now connects both Base and Free controllers to
the existing QiPan RecoveredBoardShake, exposed by Wilds.Shake. This component
already lives on the always-active board root, not the Base-only effects object.
No runtime hierarchy or new parameter defaults are introduced.

Native evidence:
- Normal Base callback 23c01e0, at 23c027c..23c02b8, uses Main.QiPan (+78),
  initial position (+1c8/+1cc), .3 seconds (ELF dbc664), intensity 20, frequency 20.
  It shakes after stop visuals and before count increment/reelstop audio.
- Anticipation callback 23c03e4 also uses ShakeUtils after stop visuals and before
  its 200ms vibration and continuation to the next column.
- Free callback 23c3cc8, at 23c3d74..23c3dd0, uses the same target/position and
  .3/20/20 arguments, after count increment, column presentation and stop sound.
- SpinPlayfield.prefab QiPan already serializes .3/20/20 and the recovered
  falloff; the existing shared shake implementation supplies scaled timing,
  Perlin displacement, interruption and restoration.

Core Unbind removes both handlers and cancels/restores the shared component before
tearing down the old graph. Existing Wild/NPC/shared shake behavior is retained.
The change leaves generation, reward arithmetic and SDK facades unchanged.

The accompanying Wheel route audit found that both Cash and Jackpot already
call HideWheel before opening their reward popups; no speculative additional
close behavior was added to that path.

Validation: Artifacts/core-stop-shake.xml passes 4/4 in Unity 2022.3.62f3 with
graphics enabled, 25.363929 seconds, process 22352 exited. The actual core scene
asserts all ten Base and five Free stop events start the active board component,
measures nonzero anchored displacement, and checks stopped state/exact original
position after return. All four actual minigames and repeated Free rounds pass.
An additional GM assertion verifies a partially stopped Free board is shaking
before SelectUS and is stopped/restored immediately afterward;
Artifacts/core-stop-shake-gm.xml passes 1/1, process 31544 exited. This verifies
the real runtime transform motion and lifetime, not complete original-app visual
parity or every overlapping effect sequence.
