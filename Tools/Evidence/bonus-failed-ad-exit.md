# Failed extra-card ad and core exit

Baseline 516aa33. Rechecked original UIBonusView failure callback
0239c3f4.asm: 239c4a0 enables the selected card's Button and 239c4b4 calls
ShowFinger. It does not clear isClick. Close handler 0239b3a4.asm checks
isEnd at +0x110 (239b418), sets it at 239b438 and calls CloseBonusView at
239b474. It does not gate this path on isClick. Current selection and exit
implementations preserve that distinction; no retry-unlock was introduced.

Added a production-scene integration case in RecoveredBonusFlowTests. After
the six free cards settle, it scene-raycasts the next card, observes pending
bonusCoin advertisement without advancing selected count, then clicks the
actual GM failure Button through the scene's top raycast hit. Exactly one
failure click must complete the pending request. The selected Button is
enabled, selection isClick remains set, count and cash do not change, and
another physical-handler delivery to that Button produces no new ad or reveal.

The same test then reaches the actual Close Button through the scene raycast.
The existing transition completes once in the manual-exit timing range,
Bonus becomes inactive, IsRunning clears and the Base round unlocks. This
connects the previously unit-tested native failure behavior to the production
window and core continuation.

Unity 2022.3.62f3 process 10660 exited. Targeted PlayMode result:
Artifacts/bonus-failed-ad-exit-tests.xml, 3/3 passing in 16.2570994 seconds.
The run includes manual six-card exit, full twelve-card automatic exit and
this failed extra-card exit. The fixture supplies initial Bonus readiness and
uses the existing local SDK facade; no real ad is served. Runtime and SDK
code remain unchanged. This does not establish every repeat-Bonus lifecycle,
all payout/region combinations or complete visual parity.
