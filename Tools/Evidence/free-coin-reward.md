# Free coin reward reveal and collection flight

JinBiEffectItem.PlayAnim MoveNext 0x23da0c0 hides glow and text, requests
coinReveal, and plays zcjb_b_chun at speed 3. Completion 0x23d9b48 resets
speed to 1, loops idle_chun, and plays the separate glow once; 0x23d9cbc
hides that glow again. Independently, a scaled .2s wait reveals the formatted
reward, followed by .2s OutQuad scaling to 1.2 and .2s back to 1
(completion 0x23d9cf0). A further .4s scaled wait starts the optional flight
and invokes the presentation callback without waiting for arrival.

FreeCoin now authors RecoveredFreeCoinReward and the shared reward-text
timing component on its existing world Canvas. The original Green font,
initial 96.3 label, and existing world mesh animation assets are retained.
BuildFreeCoinReward configures these serialized references and regenerates
the Free reel prefab without regenerating source art.

Flight-start callback 0x23d9d28 spawns the lamp under the coin with prefab
local transform. Arrival 0x23d9dcc requests exp, despawns the flight, activates
the target's first child, then spawns the flash at anchored position zero.
RecoveredFreeLampFlights uses Unity ObjectPool and the existing native
LampFlight/LampFlash prefabs for this path. The .3s flight and flash continue
independently of the coin presentation callback. Pool wiring lives on the
FreeReels Specials object and is bound to every created coin in code.

Validation: full Unity 2022.3.62f3 PlayMode suite 293/293 passed in
Artifacts/free-coin-reward-tests.xml. New tests exercise actual stopped Free
coins, both collection targets, paused reveal and flight, callback before
arrival, target activation, flash completion, flight reuse, sound ordering,
null target completion and original font retention. Fresh visual artifacts:
current-free-coin-reward-reveal.png and current-free-coin-reward-arrival.png.
The collection test harness uses a world Canvas so its actual UI prefab can
render alongside the world symbols.
The focused visual rerun passed 2/2 in free-coin-reward-visual-tests.xml;
the fresh 1080x840 arrival capture was inspected at original resolution and
shows the first collection lamp's flash .1s after arrival.

The column-major scan, FreeSpinRewards accounting and TotalFreeSpinWin
accumulation are now connected to the Free reel controller; see free-coin-scan.md.
Still pending: the subsequent bonus/ball/collection/end branches; production FreeEntry and
main UI integration; actual sound consumers and sorting against that UI.
Concurrent replay and inactive pooled-object coroutine/tween lifetimes still
need source alignment. This verifies the active reward presentation path,
not the full Free lifecycle. SDK handling remains unchanged.
