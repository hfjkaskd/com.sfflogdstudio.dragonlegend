# Missing Base-mode cash-out status loop

The current `RecoveredMainModeView.ApplyCurrent` switches the board/background/bottom but has no consumer for native Main.CashOutTipTxt (+0x1b0) or CashOutTipSeq (+0x208). This is a confirmed missing main-game presentation, separate from the cash-out entry finger, UICashOutTipView claim prompt, and reward-window CashOutTip component. Runtime implementation remains required.

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

Next implementation must author the source subtree as a prefab, drive the lifecycle from every actual SetInitShow-equivalent mode application, use the shared native Unity animation timing infrastructure, and validate first versus repeated reveal, stale text during the initial delay, all-recorded stop, Free cancellation, Base restart, and GM teardown. No Unity runtime change or new visual-parity claim is made by this audit. Region/A/B visibility remains separately unproven; SDK handling stays unchanged.
