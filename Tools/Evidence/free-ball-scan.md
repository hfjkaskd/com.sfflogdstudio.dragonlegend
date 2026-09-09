# Sequential Free dragon-ball processing

CheckPlayLongzhuAnim MoveNext 0x23cc684 returns synchronously when the generated
ball amount is not positive. Otherwise it visits all five Free columns and
three rows, resets isLongzhuEnd, invokes PlayLongZhuAnim, and awaits that flag
for each row. RollReel.PlayLongZhuAnim 0x237656c calls back with two null values
when its current result list is empty; this still goes through WaitUntil.
Initial/rolling pool objects are not the current result list.

Arrival callback MoveNext 0x23c3ec4 sets the cloned ball's parent to
FireAnimRect (+0x188, source field name verified in dump.cs), with
worldPositionStays=false, invokes InitNpc(1), waits one scaled second, then
invokes the clone's PlayAnim. The separately configured flight target is
LongzhuPos (+0xd8), not FireAnimRect. After the .7s activation callback,
0x23c4608 snapshots the clone's world position, returns it to the dragon-ball
pool and invokes CheckSmallGame(type, position, rewardCallback).

Reward callback 0x23c44cc first sets isLongzhuEnd=true, starts PlayRewardAnim
on the ORIGINAL stopped ball without awaiting it, adds its mini-reel GameObject
and float reward to FreeSpinRewards, then increments TotalFreeSpinWin. No save
or balance credit happens here. RecoveredFreeCoinScan.RecordReward now exposes
this shared dictionary/total operation; coin and ball paths use the same ledger.

RecoveredFreeBallScan is authored on FreeReels with npcDelay=1. Bind supplies
the result, player, actual NPC, target and arrival-parent transforms, language
provider and required small-game consumer. It can subscribe to the shared
Bonus flow's completion and dispatches only while the player is in Free mode.
It uses the existing stopped-ball reference, shared ball pool, flight and
activation/reward components. Each row remains pending until its consumer
returns a reward; there is no fallback reward or automatic completion.

The two-ball test uses actual stopped Free reels and actual NPC prefab,
verifies clone return before the small-game handoff, keeps the handoff pending
for twenty frames, supplies controlled reward callbacks and checks row order,
original-ball animation, shared ledger, total accumulation and no save/credit.
Unity 2022.3.62f3 full PlayMode suite **301/301 passed** in
Artifacts/free-ball-scan-tests.xml; prefab authoring exited successfully in
Artifacts/free-ball-scan-author.log.
The original protocol test did not prove real small-game UI. Subsequent work
added the Lucky/Slot/Wheel/Treasure prefabs and integrations. The two-ball test
now also verifies automatic reward collection and zero coin-credit behavior.
CheckRewardCollect and CheckFreeSpinEnd have connected components (see
free-reward-collect.md and free-end-window.md). Production FreeEntry context
binding, complete FireAnimRect/world-space integration and broader lifecycle
gaps remain. SDK unchanged.
