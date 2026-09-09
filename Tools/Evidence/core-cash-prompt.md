# Core cash prompt selection and continuation audit

Source: current local arm64 libil2cpp ELF and mumu-current/native/game disassembly. This is the branch after review in Main.CheckBaseEnd, before ExtraWild and input unlock. It is not a new payout SDK implementation.

## Verified native decisions

- 23c6058..23c6068 clears the closure's cash-prompt pending byte (+0x18).
- 23c60b4..23c6148 searches tier ids from zero up to GetCashOutCount. For each id, List<PlayerCashOutData>.Find uses predicate 23c2fa4, which compares only record.id (+0x10); isCashout, step and other status fields are irrelevant. The loop stops at the first absent record.
- Only when an absent tier exists, 23c616c..23c6204 initializes a null PlayerData.CashOutTipIndexs (+0x88) and immediately saves the empty list. No initialization or save occurs when all tiers have records.
- 23c6244 fcmp GreenCount,target / b.lt rejects balance below the threshold (and unordered NaN). Equality qualifies. 23c6288 checks Contains on the chosen id; an already prompted id exits the branch, rather than searching a later id.
- 23c6298..23c6308 sets pending and adds the tier to the prompt list before showing any UI. There is no immediate save after this append: the outer core saves at its existing end.
- GOT relocation 4f1e618 resolves Method$Game.UI.UIManager.ShowWindow<UICashOutTipView>(); context is object[]{tier, Action}. 4f1e5f0 resolves callback 23c2f84, which clears the closure's pending byte. 4f1e5d8 resolves predicate 23c2f8c, which returns pending == 0. WaitUntil at the branch tail is unconditional, including when no prompt qualifies.
- UICashOutTipView.OnBeforeShow 23abcb8 reads the tier and Action, formats GetCashOutCash(tier) with zero decimals, sets the authored CashTxt, and shows its finger at ClaimFinger.
- OnClickButton 23abed8 handles ClaimBtn only. ARM 23abfa0..23abfe4 orders click sound, HideFinger, Hide(), then immediate Action invocation. 23ac020 tail-calls GameDataManager.OpenWithDrawWindow (23700f0). The callback does not wait for OnAfterHide. Applying the review window's after-animation callback convention here would be wrong.

## Implemented and remaining

RecoveredPlayerProgress.PrepareCashOutPrompt implements the verified first-absent-tier selection, threshold, one-time prompt marker, and null-list save order. It returns the selected tier or -1. Six new test cases cover threshold equality, high balances, NaN, repeat checks, incomplete/complete records, and null-list initialization both below threshold and after every tier has a record.

The method is a prerequisite for the pending core integration; CoreRoundFlow does not call it yet. UICashOutTipView's native prefab, immediate claim callback, unconditional wait and withdrawal entry still need to be connected together. Existing LocalCashFacade and all SDK processing remain unchanged. This audit and selection implementation do not prove the whole branch or lifecycle complete.

Validation: focused RecoveredPlayerProgressTests passed 30/30, including all six new cases (Artifacts/cash-prompt-selection.xml and .log). Unity PID 48272 exited. No full-scene claim or visual parity claim is made by this model-only regression.
