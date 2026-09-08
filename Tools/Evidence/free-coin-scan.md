# Free coin scan and reward accounting

CheckPlayBonusAnim MoveNext 0x23cb808 reads FreeSlotGameResult live in
column-major order. For each id 9 it draws GetCoinReward, increments the
scan subtotal and PlayerData.BonusArea[column], then resolves GetUnSelect
using that updated count. It invokes FreeRoll.GetRollReel(row) and
RollReel.PlayFreeBonusAnim, then waits .5 scaled seconds including after the
last coin. The existing RecoveredBonusCoinSequence implements this ordering.

PlayFreeBonusAnim 0x23763cc visits the current freeBonusList, so initial or
rolling pooled coins must not generate its callback. RecoveredFreeSpecials
now exposes the existing separate current-stop reference for that purpose.
The Free callback 0x23c1c54 first starts PlayGameBonusAnim, then Dictionary.Add
using the mini-reel GameObject as key, then adds the integer reward converted
to float to GameData.TotalFreeSpinWin at +0x20. No save or balance credit
occurs in this callback. Dictionary duplicate-key behavior is retained.

RecoveredFreeCoinScan is authored on FreeReels, with interval .5 and a
serialized board reference. Binding supplies the real result, rules, player,
collection view and current language provider. It subscribes to the reel
controller's start and stopped events: start clears the per-spin dictionary
(AutoFreeSpin 0x23c4758), while all-five-stopped starts the actual scan.
Each coin launches the previously implemented native reward and lamp
presentation. Completed is the continuation boundary for CheckBonusGame.
The accumulated Free total survives per-spin dictionary clearing.

The integration tests run two actual five-column spins and verify reveal
before dictionary/total mutation, live collection counts, all 15 key/value
entries, cumulative totals across spins, no saves or balance writes, and scan
completion while the final coin is still presenting. A separate initial-pool
test verifies the native distinction: counts still increment and delays run,
but no stopped callback means no dictionary entry or Free total increment.
It also pauses the scan using timeScale zero.

Validation: Unity 2022.3.62f3 full PlayMode suite 295/295 passed in
Artifacts/free-coin-scan-tests.xml. Prefab generation completed with exit code
zero in Artifacts/free-coin-scan-author.log.

Pending: production FreeEntry/main-view context binding, counters and full
CheckBonusGame -> CheckPlayLongzhuAnim -> CheckRewardCollect -> CheckFreeSpinEnd
continuations, plus the previously documented inactive tween lifetimes and
actual audio consumers. This component extends the real Free reel chain but
does not establish complete player-lifecycle parity. SDK behavior is unchanged.
