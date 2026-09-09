# Free intro and total-window scene input

Baseline db53726. RecoveredBigWinFreeChainTests now uses the current GameEntry
camera and EventSystem raycasts for Free intro Claim, the existing GM failed
and rewarded ad Buttons, retry Claim, and the Free total Continue Button.
Each operation waits at most five real seconds for an active/interactable
button whose GameObject is the first raycast hit's pointer-click handler.
Only that actual top hit receives the pointer-click event; there is no fallback
to direct onClick invocation for these operations.

The first run (PID 13688) failed to hit ClaimBtn. This older chain fixture had
no portrait rendering target, unlike the existing core/Bonus scene input
fixtures. It now assigns a 1080x1920 target to the authored camera and releases
it on cleanup. No production layout, camera configuration or input code was
changed to satisfy the fixture. Later failures in the initial run also logged
duplicate scene EventSystems/listeners after its early nested failure; that
failed run is not evidence of production duplicate scene initialization.

The portrait run, PID 27408, exited with 4/4 passed in 36.8574898 seconds:
Artifacts/free-chain-scene-input-portrait-tests.xml. It covers direct
BigWin-to-Free, ready Bonus between them, all awarded Free rounds after ready
Bonus, and actual final Base collection followed by Bonus and all Free rounds.
The existing full-session reward ledger assertions remain enabled.

At Free intro, failed ad leaves initial FreeSpinCount and the active window
unchanged. A scene-clicked retry remains pending without extra spins until
the GM rewarded outcome. Success assigns initial + configured extra spins.
Actual Continue remains reachable and keeps the paid Spin busy until return
transition and core completion. Next paid Spin is still accepted.

Rechecked native/game/023b17f0.asm (click latch read at 23b18ac and zero write
at 23b18cc) and 023b1bf8.asm (initial + extra added at 23b1cf4 and assigned to
FreeSpinCount at 23b1cf8). The current popup retains these source behaviors;
no SDK or runtime mutation was justified.

Scope: scene pointer-click routing at portrait resolution, not Android physical
touch or pixel parity. Other actions in this mixed-chain fixture still use
direct Button callbacks. Existing deterministic ForceFreeSpin/collection and
shortened-case assumptions remain documented in mixed-bigwin-free.md. No full
suite rerun or claim of complete lifecycle equivalence accompanies this change.
