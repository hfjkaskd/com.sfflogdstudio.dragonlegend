# Bonus character reward sequence

Native business sources: `native-functions/game/2396d54.c`, `2396060.c`,
`2395cf0.c`, `2396104.c`, `239674c.c`, `239692c.c`, `23964bc.c`, `2396714.c`
under the current reverse root. `Tools/Evidence/bonus-item-turn.md` covers the
opening turn and text branch.

`RecoveredBonusCharacterRewards` invokes the actual authored BonusItemTurn and
implements the character branch after it. It keeps independent jobs, rather than
cancelling earlier flights when a new card is accepted.

1. If this card completes a jackpot, SetTaskData(2,1) occurs before sound/turn.
2. Sound `coinReveal`, then the original type's turn/hold/glow sequence.
3. With no target, invoke input release and finish. With a target and an incomplete
   jackpot, invoke input release **before** vibration and flight. With a completing
   jackpot, retain input release for the popup callback.
4. Vibrate 200, spawn the original trail under the source item, preserving its
   original local position (-1.87,-2.1,0), rotation and unit scale. Set sorting from
   the Bonus canvas, not Main canvas. FlyWithPool (`238ce40`) uses Fly (`238c74c`)
   with default automatic arc and Ease 4: 0.3 seconds, distance*0.3 upward quadratic
   control offset, InOutSine. Destination world position is captured at start.
5. Arrival: sound `exp`, despawn trail, hide target child 0, spawn the existing
   native Ef_sltongbi conversion under target at anchored position zero; dispatch
   jackpot animation request; play `dianliang` once.
6. Effect completion: despawn effect. For a completing jackpot, read the current
   Grand/Major/Minor reward now, then wait scaled 1.5 seconds. Open jackpot popup
   with this captured amount. Its float return is ignored; invoke input release.

`BonusFlight.prefab` is an authored variant of the existing converted native
LampFlight particles/materials. It restores the original unit UI scale and local
offset. The reel-specific .01 scale belongs to the world reel conversion and must
not be applied to Bonus's UI-item parent. `BonusCharacterRewards.prefab` binds the
flight and existing LampFlash prefabs; runtime code only instantiates pooled
authored objects. Pools use UnityEngine.Pool.ObjectPool.

ELF RELA resolution plus `analysis/script.json` identifies Ghidra pointer slots:

- 0501cfa8: `coinReveal`
- 0501d230: `exp`
- 0501d220: event string `3`
- 0501d228: `dianliang`
- 0501d1d0: `UIManager.GetWindow<UIBonusView>()` for render sorting.

## Verification scope

The serialized binding is independently resolved from
`delivery/Assets/RecoveredMonoBehaviours/data_level0_85.json`: `JinBi_Lizi`
has PPtr (file 3, path 149). Reading the original data.unity3d with UnityPy
resolves level0 external 3 to `sharedassets0.assets`; GameObject 149 is
`tuowei`. This verifies the flight asset identity rather than inferring it
from a similar effect name. The exported Loading.unity omits these custom
fields and is insufficient evidence by itself.

`RecoveredBonusCharacterRewardsTests` starts two real item turns concurrently,
checks the early-vs-late input callbacks, original spawn offset/scale, renderer
order, world arc, pause, target child visibility and effect ordering. It changes
the jackpot meter before the effect completes and again during the 1.5-second
wait, proving the popup receives the effect-completion snapshot. It checks the
popup callback causes no additional direct money credit, no-target cards do not
fly, and cancellation stops active jobs and deferred popups.

Popup/audio/vibration/jackpot-animation requests remain explicit binding points
for the full Bonus window, which is still required. The ordinary cash and jump
reward branches are separate and remain unfinished, as does main Bonus entry/exit.
This component does not claim the complete lifecycle is implemented.

Final full PlayMode run: `Artifacts/bonus-character-verified-tests.xml`,
234 passed, 0 failed. Both item failure events and the sequence failure event
are observed by the test, including the final cancellation scenario.
Fresh captures `current-bonus-character-flight.png` and
`current-bonus-character-arrival.png` exercise the current authored components.
The trail is faint in this isolated setup and the arrival capture is the first
effect frame; these images do not establish complete flight/arrival visual
fidelity. Full-window camera, scaling, target art and intermediate effect frames
still need comparison when the Bonus window is connected.
