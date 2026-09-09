# Bank selection flight continuations

The selection controller now supplies an `Action<float>` to the presentation owner, intended to pass directly to `RecoveredBankItem.Play`. The recovered item calls it only through flight arrival. This restores the missing continuation contract; production Bank window authoring and end-flow suspension are still pending. No SDK changes.

## First selection

ARM `02393454.asm`, native callback MoveNext 2393454:

- 23934c8 calls ShowAd using the current selected list.
- 23934d4 loads BtnRect +98; 23934d8/23934dc set target scale 1 and duration 0.5, then DOScale at 23934e4. There is no explicit SetEase in this callback; the eventual view must reproduce the original tween default.
- 2393500 sets a 1-second scaled WaitForSeconds with Update timing 8. This delay begins alongside the button tween, not after its completion.
- After resumption it obtains BtnRect.transform.GetChild(1), calls PoolManager.ShowFinger with the existing bankFinger, and stores the result back to bankFinger. The view contract exposes this as ShowContinueFinger; it must target that actual child when the Bank prefab is integrated.

## Later selection

ARM `02393870.asm`, native callback MoveNext 2393870:

- 23938dc and 23938e4 load the selected list +C0 and WinList +A8; 23938ec..23938f8 compare their Count fields. Unequal counts immediately finish the callback.
- Equal counts start a scaled 0.5-second wait (2393914..239392c, Update timing 8), then call BaseWindow.Hide after resuming.
- This is a selected-count check at flight arrival, not a completed-flight counter. It is not checked again after the wait. Preserve this even when another ad request is pending or subsequently fails.

The controller guards all ad, flight and delayed continuations by its lifetime. Reset changes the lifetime as well as Cancel, preventing a reused controller from accepting discarded GM callbacks merely because its cancelled flag is false again. The native in-window concurrency remains unrestricted.

## Verification

The PlayMode tests exercise delayed arrival, immediate ad-marker/button updates, scaled-time pauses, the one-second finger delay, partial selection remaining open, final half-second close, and stale ad/flight/delay rejection across Cancel plus Reset. A second asynchronous case verifies the selected-count behavior while the third ad is pending, including its failure during the close delay.

Unity 2022.3.62f3 focused PlayMode result: `Artifacts/bank-continuation-final.xml`, **6/6 passed**; process 25148 exited. The earlier five-case run also passed before adding the concurrent selection case. Full production regression was not repeated because the selection model still has no production window caller; the last full result is 384/384 in `bank-item-regression.xml`. This increment does not claim an integrated or visually verified Bank popup.
