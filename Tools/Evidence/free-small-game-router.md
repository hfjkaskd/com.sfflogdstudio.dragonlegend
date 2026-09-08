# Free ball to small-game dispatch

Native UIMainView.CheckSmallGame MoveNext 0x23cfe28 maps ball type 0 to
configuration column 0, type 1 to column 1, and every other integer to column 2.
It then calls ConfigManager.GetFreeReward (0x236c01c) exactly once. The selected
enum dispatches Slot=0, Wheel=1, Treasure=2, Lucky=3. Ball color does not directly
select a game. In the Slot branch, GameData.SetTaskData(4,1) occurs before
SlotPrefab position/activation/scaling. Other branches do not perform this
task update. The float completion callback is passed onward unchanged.

RecoveredFreeSmallGameRouter implements this dispatch with required entry
consumers. It does not draw the game's reward, credit money, complete a row or
substitute another game when a consumer is missing. Slot/Wheel/Treasure entry
animations and windows remain separate unfinished work. Their callbacks in
the router unit test only record dispatch and are not implementations.

The dispatch test exercises all four results, color columns 0/1/2 and
out-of-range type mapping, verifies the next RNG value after the single
selection draw, and observes task 4/save before the Slot consumer. It also
checks callback identity and absence of an invented reward callback.

RecoveredFreeBallLuckyFlowTests binds actual stopped Free balls, pooled flight,
NPC activation, the router, authored FreeLuckyGame/UIRewardView, real cash flight
and the shared coin ledger. Two balls must each wait for actual plain-button
claim and cash arrival, then reveal their own reward and accumulate the paid
amount in column order before the scan finishes once. Only board/branch weights
are deterministic test configuration; the actual entry rules supply the Lucky
reward. Unexpected game selection fails the test instead of returning a reward.

This proves the composed Lucky path in a scene integration harness. Production
FreeEntry/board binding, main-world coordinate placement and the remaining
three games are still pending. It does not claim complete game lifecycle or
visual parity. SDK handling is unchanged.

Validation: Artifacts/free-small-game-router-tests.xml reports 307/307 PlayMode tests passed in Unity 2022.3.62f3 with graphics enabled, including both new dispatch and two-ball actual Lucky flow tests.
