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

## Authored prompt window increment

BuildCashPromptWindow imports the original UICashOutTipView subtree, retaining RectTransforms and official Image/Button/Text/TMP/layout components. The CashTxt reference is Content/Layout/Text (Legacy), using the recovered Gold bitmap font; GreenTxt is a separate static TMP label. ClaimFinger is the ClaimBtn transform itself. The title retains UIShiny's .5/.25/135/1/1/1 values and two-second loop. Show sound GOT 4f1dbe8 resolves `congrats`.

The source ef_tixiantc skeleton consumes 74417 bytes exactly and contains one idle animation of approximately three seconds with 106 timelines. Its twelve mesh attachments retain original bone influence counts, vertices and weights. Tools/prepare_cash_prompt.py supplies these to the existing official Unity Animation/RecoveredRegionRig author; no Spine runtime is added. The atlas uses uncompressed pixels with alphaIsTransparency disabled, preserving premultiplied color data. The original art rect is 1015 x 1032.2754 with pivot (.49442595,.38436967).

RecoveredCashPromptWindow binds code-driven Claim and an explicit withdrawal callback. Claim emits click, hides the finger, starts scaled popup hiding, invokes the core continuation immediately, then invokes withdrawal; the animation completion does not invoke the continuation again. Reuse resets the amount and finger; cancellation deactivates and suppresses continuations. The production core/withdrawal binding is still pending. This window increment does not silently substitute a no-op payout or issue a cash request.

RecoveredCashPromptWindowTests loads current GameEntry, instantiates the actual authored prompt, verifies amount and finger parenting, raycasts and clicks the real Button, tests callback order with timeScale zero, resumes hiding, and checks cancellation/reuse. It captures Artifacts/current-cash-prompt.png from a fresh 1080x1920 render. Final focused PlayMode test passed 1/1 (Artifacts/cash-prompt-window-fixed.xml); Unity PID 47736 exited. The fresh image was inspected: congratulations title, PayPal icon, amount, dragon art and Cash Out button/finger render correctly. Initial inspection exposed two IndividualSprites still imported as textures, producing a white title and missing icon; authoring now imports them as single uncompressed sprites and the test rejects missing Image sprites. This verifies the current project, not fresh-original pixel equivalence.
