# Four production Free ball branches: failed ad and retry

Baseline d74653c. RecoveredCoreBallBranchesTests retains its existing plain
claim case and adds an advertised claim case. Both load current GameEntry at
1080x1920 and run Slot, Wheel, Treasure and Lucky serially through actual Free
ball flight, NPC activation, branch routing, window presentation, cash arrival,
reward scan, total window and return to Base.

The existing fixture forces Free entry, shortens each session to one generated
round and selects a seed for one ball and the requested branch. It does not
replace branch consumers, generated reward values or cash-flight callbacks.
It opens MoreSpins behind the branch to retain existing popup-depth assertions.
This remains deterministic integration coverage, not naturally encountered
four-branch Android gameplay.

For each branch the advertised case scene-raycasts the actual Claim Button,
the existing GM failure Button, retry Claim, then GM reward Button. Each click
must hit the expected Button as the top pointer-click handler. Before success,
balance stays unchanged and the ball scan stays pending; failure leaves no
recorded ball reward. Exactly two claim clicks and both ad outcomes are needed
before proceeding. The SDK facade itself is unchanged.

Expected reward is captured independently from original offer and advertised
multiplier before completing either ad. Treasure uses matching CollectInfo
worth and its configured multiplier. The eventual recorded ball reward must
equal that amount, FreeReward and TotalFreeSpinWin; balance must increase by
exactly that amount once. Each branch returns to Base, unlocks the paid Spin,
and the fixture accepts another paid Spin after all four branches.

Current raw native source rechecked:
023dd4b0.asm clears Treasure isClick at 23dd4b8;
023d8d7c.asm clears UIRewardView isClick at 23d8d84.
These failures do not themselves dispatch cash or complete the ball caller.
Existing claim implementations match these inspected failure paths.

First run PID 18528 exited, 2/2 passed, 33.5611541 seconds.
After independent advertised-amount assertions, PID 12172 exited, 2/2 passed,
33.3212943 seconds: Artifacts/core-ball-ad-retry-ledger-tests.xml.
No runtime/assets/SDK mutation or full-suite rerun was needed this round.
This evidence covers the selected outcome of each branch; it does not prove
every Wheel outcome, every profile, lifecycle interruption or visual parity.
