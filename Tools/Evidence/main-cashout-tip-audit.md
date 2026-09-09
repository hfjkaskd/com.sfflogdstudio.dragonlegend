# Missing Base-mode cash-out status loop

The initial audit found that `RecoveredMainModeView.ApplyCurrent` had no consumer for native Main.CashOutTipTxt (+0x1b0) or CashOutTipSeq (+0x208). This is separate from the cash-out entry finger, UICashOutTipView claim prompt, and reward-window CashOutTip component. The subsequent implementation below restores this main-game presentation.

## Authoritative native sequence

Evidence root: `C:/Projects/Nut Sort Relax/reconstruction/mumu-current`.

- `native/game/023bca34.asm`: SetInitShow kills the old sequence without completing it, clears its reference, hides the text's parent, and on the Base path tail-calls ShowCashOutTip at 23bce84. The Free path hides/cancels without restarting.
- `native-functions/game/23bc108.c` and `native/game/023bc108.asm`: select the first tier for which no PlayerCashOutData record has the same ID. Record status is irrelevant. No tier means return without starting a sequence. There is no IsA branch in this function.
- Text is computed immediately when the sequence is created, before its delay. It is not recomputed at the reveal callback. At/above threshold, use `You Can Cash Out Now!`. Otherwise format threshold minus balance with 2 decimal places and threshold with 0 decimal places, then insert both into the rich text template in `main-cashout-tip-references.json`.
- Append 12 seconds; activate the text's parent; scale it to one over 0.5 seconds; append 5 seconds; scale to zero over 0.5 seconds; hide on that tween's completion; append callback to invoke ShowCashOutTip again. Each new cycle re-evaluates records and balance. No balance-change event restarts this sequence.
- Callback `23bf500` only activates; `23bf544` only hides. Neither resets scale. `native/game/023bf588.asm` contains a single branch to `23bc108`, proving recursive rescheduling instead of a fixed repeated tween with stale text.
- Source CashOutTip starts with unit local scale. Therefore the first scale-to-one must retain that starting scale; do not invent an initial zero-scale reset. After a completed cycle the next starts from zero. Cancelling midway also preserves the current scale.

## Prefab source

`ReferenceOriginal/Res/ViewPrefabs/UIMainView.prefab`:

- Node RectTransform 224901155395665742 is full stretch, zero anchored position/size delta, unit scale.
- CashOutTip GameObject 1187454977326620, RectTransform 224440584996359664, direct child of Node. Top-center anchors and pivot (0.5,1), anchored position (251,-116.04309), size (359.5136,113.80737), unit scale.
- Background Image 114163714622165790 uses sprite GUID 17c564eadab8a3940aac81ae943b3097, sliced mode. Preserve its source visual properties.
- Child TMP component 114416982030819627 is the exact serialized CashOutTipTxt. Font GUID 31628181d58311344bb283c127fc9aba and material GUID 56487477c1a58f648b2fdc3f2134f214; use the original child layout/font settings and resolved rich text resources.

## Reproducible reference resolution

`Tools/audit_main_click_references.py` now accepts `--function`. It resolves actual ELF relative relocations into script metadata instead of guessing string pointers. `--function 23bc108` resolves 16 references, including all three callbacks and both messages. The generated JSON records ELF/source SHA-256 hashes. Re-running the default Main click audit generated a byte-identical result to its committed evidence (SHA-256 A127C190446A6DA210E4204F3390A525CCBAA68EF42CCBFE31AC6B97D8BEC254).

## Runtime implementation

`BuildMainCashOutStatus.Save` extracts the original Node transform with only the CashOutTip subtree, maps standard Image/TMP components, and retains the original layout and text styling. The main background sprite maps to the recovered `zjm_s9g_spin_bg.png` with its existing sliced border. Timings and text templates are serialized on the prefab. The full CoreRound author also includes this prefab.

`RecoveredMainCashOutStatus` uses the existing `RecoveredTreasureCardRunner` to keep the hidden-panel delay alive. A generation token cancels old enumerators on each mode application or teardown. The runtime preserves scale across cancellation, computes text at scheduling time, and recursively schedules a fresh cycle after hiding. Each cycle scans record IDs without filtering record status. No IsA guard was invented.

`RecoveredMainModeView.Applied` is emitted after every mode application; CoreRound subscribes after binding the status component and unsubscribes during GM teardown. Initial binding explicitly applies the current mode because the initial ModeView binding precedes CoreRound binding.

The initial full run (PID 3804) exposed a queued enumerator reading its panel before checking teardown, and an imported PNG reference retaining the original native-asset reference type. The first was fixed by checking cancellation before the iterator's first transform access; the second by authoring the PNG sprite reference as type 3. The initial and intermediate full runs are retained in `Artifacts/main-status-tests.xml` and `Artifacts/main-status-fixed-tests.xml`; neither is a passing validation claim. Author PID 51528 then regenerated the corrected prefab successfully.

Final validation: PID 35068 terminated with 460/460 PlayMode tests passing in 97.3139827 seconds (`Artifacts/main-status-complete-tests.xml`). The real-scene test covers first/repeated reveal, frozen text during the delay, fresh next-cycle text, scale preservation on interruption, Free cancellation lasting beyond one full cycle, Base restart, all-recorded stop, and GM teardown. The newly rendered `Artifacts/current-main-cashout-status.png` was inspected: the source red sliced background now renders correctly with the rich text. This is current reconstruction evidence, not proof of complete visual parity against the original application. Region/A/B visibility remains separately unproven; SDK handling stays unchanged.
