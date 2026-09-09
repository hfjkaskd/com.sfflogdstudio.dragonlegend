# Review branch after Bank settlement

The core now consumes the existing ReviewRequested level-up event, opens the authored ReviewWindow after Bank, and waits for its close callback before the existing ExtraWild/unlock/save continuation. Cashout checks between review and ExtraWild remain pending; this is not a complete lifecycle claim. SDK facades are unchanged. Five-star navigation uses the original Unity Application.OpenURL API rather than a new SDK integration.

## Native evidence

- Main.ReviewPop 23bacb4 sets +218 to true and returns. GameData.SetExperience 236de70 emits review only when the newly incremented Level equals ConfigManager.GetReview. The current US profile's recovered threshold is 9999, so the integration test prepares level 9998 and performs one real SetExperience level-up; it does not change the runtime configuration.
- CheckBaseEnd 23c5df8 tests +218 after the Bank wait. It constructs a close Action, shows the review window, then calls UniTask.WaitUntil in Update timing 8 at 23c5ee8. Callback 23c2f4c clears +218. The Bank and review WaitUntil calls are outside their conditional ShowWindow branches; the recovered core now passes through both waits even when no window is pending.
- Review.OnBeforeShow 23d7700 plays `remind` (GOT 4f1e958), stores the show Action, and calls CheckStarIndex(0). CheckStarIndex 23d77f0 stores the count and activates each star's child `S` iff its zero-based list index is less than the selected count. GOT 4f1ea10 resolves `S`.
- OnInit callback 23d7b90 plays click, looks up the button's index in Stars, adds one and calls CheckStarIndex. No selection persistence is introduced.
- OnClickButton 23d79a8 handles ClaimBtn (4f1d180) and CloseBtn (4f1cf30). Both play click. Claim with star<=4 calls Hide, including zero stars. Claim with star>4 opens `market://details?id=` (4f1ea18) plus Application.identifier and calls virtual Close(true). Vtable offset 0x328 is BaseWindow slot 31, Close(bool), not Hide. The recovered high-rating path therefore destroys the window after its close animation/callback; ordinary Hide keeps it reusable.
- OnAfterHide 23d7b54 invokes the stored Action. No reward, advertisement or player-data write occurs in the review window.

## Prefab and ownership

`BuildReviewWindow.Save` imports the original UIReViewUsView hierarchy, official Button/Image/TMP/HorizontalLayoutGroup/LayoutElement/GraphicRaycaster components, original fonts/materials and six popup sprites. Five serialized star Button references own their highlight children; events are code-bound. The title uses original UIShiny values (.5 factor, .25 width, 135 degrees, softness/brightness/gloss 1, two-second loop) through the existing official Unity implementation. Popup canvas/background and scale curves use the recovered common popup authoring convention.

CoreRoundFlow has a serialized prefab reference and lazily instantiates the window on its first pending review. After a five-star Close destroys it, a subsequent pending event can instantiate it again. GM teardown cancels the wait and discards its continuation. URL dispatch is injected when binding: production supplies Application.OpenURL and Application.identifier; tests supply a recording function, so no real store is opened by automation. The same runtime path runs in Editor and players.

## Verification

`RecoveredReviewWindowTests` checks all 0–5 selections, reset on reuse, lowering selection, highlight ranges, scaled pause during hide, callback after animation, reusable low-rating/close-button hides, exact five-star URI and destruction, and cancellation. The actual BankWindow integration test now triggers both Bank and review, verifies the busy flag and completion stay blocked through both, raycasts/clicks a real fourth star and Submit, then verifies ExtraWild opens only after review closes. It renders `Artifacts/current-review-window.png` from current GameEntry at 1080x1920; the image has been inspected, showing four highlighted stars, original text/layout and Submit/close controls.

Final full PlayMode regression passed 391/391 (Artifacts/review-final.xml); Unity exited successfully. Two existing reward-stage tests were updated to distinguish reward completion from the subsequent unconditional Bank/review Update waits before core input unlock. The final run covers these waits and the single-level-up fixture. Current-project visual inspection does not establish fresh-original pixel equivalence or all country routing.
