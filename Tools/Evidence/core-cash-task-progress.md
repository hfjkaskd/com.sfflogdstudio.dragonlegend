# Core reward events and cash task progress

The live main flow had unconsumed CashOutTaskRefreshRequested events from Base big-win/jackpot settlement, Bonus jackpot, Free wheel jackpot and Free entry. This prevented gameplay from updating existing cash-task records even though the producers emitted the recovered event.

## Native rule

Main.RefreshCashOutTask 23ba71c reads two integer arguments. Predicate 23c3044 compares PlayerCashOutData.step (+0x18) with the first argument, not its type (+0x14), id or payout status. Every matching record gets count += amount at 23ba960..23ba968. There is no cap, completion check, positive-amount filter or isCashout filter. ARM integer addition wraps. SavePlayerData at 23ba99c follows all updates and also runs when records are null or no records match.

RecoveredPlayerProgress.RefreshCashOutTask implements this rule without allocating a FindAll list. CoreRoundFlow subscribes the existing four producers at bind and unsubscribes them before profile teardown. Free entry forwards RewardBranches' event; the core subscribes only the forwarding event to avoid double counting.

Model tests cover multiple matching steps with different types/statuses, negative/zero amounts, integer overflow, unrelated steps, null/empty records and save-after-mutation timing. Full PlayMode regression passed 403/403 (Artifacts/cash-task-regression.xml; Unity PID 48600 exited). The subsequently strengthened actual Free-entry test passed 1/1 (Artifacts/cash-task-core.xml; PID 53096 exited): a step-5 record with unrelated type and isCashout=true advances from 7 to 8 exactly once before the intro closes, and the persisted PlayerData contains count 8. The scene continues through the actual Free end window and returns to Base.

## Withdrawal entry audit, still pending integration

OpenWithDrawWindow 23700f0 reads GameData.NeedWithDrawOpne at +0x58. It does not inspect GameData.IsA (+0x10). When true, it first shows UIWaitingView (GOT 4f1bcf8), then calls Publish.Payout.MyOrders with callback from GOT 4f1bce0. When false it creates a new empty List<PayoutOrder> (4f1bcd8) and immediately shows UICashOutView (4f1bcf0), passing that list in the window context. Mapping this branch directly to country/A mode would be unsupported by the disassembly.

SDK behavior remains unchanged. The prompt's connection to the complete withdrawal view is not yet implemented; no placeholder window or simulated approval is substituted. The task-progress fix supports real core gameplay independently of that pending UI work.
