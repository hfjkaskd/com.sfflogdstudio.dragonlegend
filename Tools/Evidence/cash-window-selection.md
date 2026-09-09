# Withdrawal window initial selection

UICashOutView.CheckItemKuang (23a714c) must not reuse Main.CheckBaseEnd's cash prompt selector. The former chooses an unfinished record or absent id; the latter ignores all recorded ids.

## Authoritative behavior

- ARM 23a721c reads PlayerCashOutDatas, and 23a725c..23a7260 compares its raw Count with ConfigManager.GetCashOutCount. Count >= configured count branches to 23a7344..23a7360, deactivates ItemKuang and returns -1. This is a count test, not an all-records-completed test. Duplicate, unrelated or unfinished records still contribute to Count.
- Otherwise the function searches ids from zero. List.Find returns the first matching id, including when duplicates exist. ARM 23a7328..23a733c returns an absent id or an id whose first record has step != 1000; only step == 1000 advances the search.
- It does not inspect balance, CashOutTipIndexs, type or isCashout, and performs no saves or repairs. An empty configuration takes the Count >= count path and hides the frame.
- InitCashOutItems 23a7390 calls this function and stores the result in selectIndex (+0xe4) before creating or refreshing its list.
- CashOutItem.RefreshKuang 23a63f0 only acts when its own id equals the requested selection: it reparents the shared frame with worldPositionStays=false and activates it. The nonmatching branch is a no-op; each item does not own a separate selection frame.
- The native RefreshCashOutItemSelectKuang handler 23a7a8c is short: it reads index from the event (empty array defaults to zero) and calls CashOutBottom.InitUI(index, current type). Its local decompiled C contains a very large inlined tail, so the bounded ARM function is the authoritative boundary. Do not reconstruct all that tail as additional handler logic.

## Implementation and validation

RecoveredPlayerProgress.FindCashOutWindowSelection exposes the original index and explicit hide-frame result for subsequent native-window binding. It remains separate from PrepareCashOutPrompt. Three new tests verify the distinction, step-1000 skipping, duplicate first-match semantics, raw count behavior, empty configuration and no persistence side effects.

Focused RecoveredPlayerProgressTests passed 38/38 (Artifacts/cash-window-selection.xml); Unity PID 48216 exited. The withdrawal window/list/bottom presenter is still pending; this selection method alone does not complete the prompt-to-window route. SDK behavior is unchanged.
