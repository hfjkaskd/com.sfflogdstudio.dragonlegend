# Full twelve-card Bonus through scene raycasts

Baseline fa55e85. RecoveredBonusFlowTests now shares its existing production
scene setup between manual free-card exit and a new twelve-card automatic-exit
case. Initial Bonus readiness is deliberately supplied by the fixture. This is
an integration test of the full selection/exit path, not natural collection.

The new case clicks every card through the scene's top raycast hit. For each
card after the configured six free choices, it asserts the local ad is pending,
placement is bonusCoin and selected count has not advanced. It then waits for
the actual GM reward Button to be exposed and delivers its click through the
same scene raycast path. Exactly six successful GM clicks are required.
Selection advances only afterward. Reward/Jackpot plain Buttons also use the
top scene hit and their existing delayed reveal and cash-flight callbacks.

After the twelfth card releases input, selection must be ended. No Close click
is delivered in this case. The existing Bonus exit, transition and caller
continuation must produce one completion, hide Bonus, clear IsRunning and unlock
the Base round. The measured interval from observing selection end to Bonus
completion must fall within 5.65--5.95 scaled seconds. The manual case retains
its separate 3.7--3.9-second close-to-completion assertion.

Rechecked original 0239d1e8.asm: 239d268 loads 2.0 and 239d280 calls
UniTask.WaitForSeconds before automatic closure. Current RecoveredBonusExit
supplies the corresponding serialized two-second wait; its existing transition
and completion ordering are documented in bonus-exit.md. No timing or runtime
code was changed to make this test pass.

Unity 2022.3.62f3 process 24280 exited. The targeted PlayMode run reports
2/2 passing in 13.0538873 seconds (Artifacts/bonus-twelve-scene-tests.xml):
the new twelve-card case and the existing six-card manual-exit case. No full
suite run or Android rebuild was performed for this test-only change. The
Android natural six-card path remains recorded separately in
android-natural-bonus.md. This result does not prove real ad SDK behavior,
all random payouts, physical touch handling, full lifecycle or visual parity.
