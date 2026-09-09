# Bonus scene input and independent Android continuation

Baseline 20080f9. RecoveredBonusFlowTests now delivers the Bonus card, reward
plain-claim and Close clicks through EventSystem.RaycastAll and the top hit's
IPointerClickHandler. It no longer invokes those Buttons' onClick delegates
directly. Each free card must be the top hit and increment the selected count
once. Reward clicks wait for the authored reveal to make the Button reachable;
covered controls are never bypassed. The existing stacked MORE SPINS window,
native transition timings, reward arrival, Bonus completion and Base unlock
assertions remain in the same test.

Unity 2022.3.62f3 process 3852 exited. Targeted PlayMode result:
Artifacts/bonus-scene-input-tests.xml, 1/1 passed in 10.9938818 seconds.
This fixture deliberately sets Bonus readiness; it does not establish natural
collection or physical Android touch coverage. No runtime/SDK changes or APK
rebuild were needed. The earlier 469/469 full result predates this test change.

## Device session boundary

The observed twelfth paid round had SpinCount=8, bank=12, level=6, exp=1,
GreenCount=570.6666870117188 and BonusArea=[2,1,2,3,2]. After MuMu stopped,
the attempted thirteenth tap failed with device not found. MuMuManager confirmed
instance 1 was stopped; its configTab records this reconstruction package.
Launching that instance and reading the existing app save recovered the above
twelfth-round state. No reset or save replacement was performed.

Subsequent additional rounds and cash-window navigation were not attributable to
the assistant's single-step inputs. The user clarified that they were clicking
the game, then offered an independent session. Those intervening captures must
not be counted as controlled Bonus or automatic-repeat evidence.

At the independent restart, MuMuManager still reported instance 1 running
(process 15412), so only the reconstruction activity was opened. The observed
starting screen had SPIN 0, bank 20/30 and $6.70. Original user progress was
preserved. The current ADB endpoint is 127.0.0.1:16416; reconnects were needed
when the server lost this transport, without replaying already delivered taps.

Using actual visible controls: Spin opened MORE SPINS, GET NOW requested the
unchanged local ad facade, and GM: Ad reward granted ten spins. One subsequent
Spin tap settled with SPIN 9, bank 21/30, level 7 exp 5/6, unchanged displayed
$6.70 and GOOD LUCK. The settled save has LimitSpinCount=21,
GreenCount=670.3333740234375 and BonusArea=[2,1,3,5,4]. Current screenshots and
save snapshots use the Artifacts/android-independent-* prefix. These are
reconstruction observations, not original-game visual parity references.

With no further input, the settled and idle snapshots were taken 68.55 seconds
apart (host file timestamps). Their complete playerData.d values are identical.
No extra paid round or balance change occurred during this observation. This
does not prove every input condition; the natural device Bonus remains pending.
