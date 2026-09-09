# Extra Wild claim logic

Native source: current `mumu-current/native/game` ARM64 disassembly and ELF relocation-backed literals. The initial increment restored the claim model and configuration accessor. The subsequent increment below connects the production window and free second guide. The main paid entry and progress display are now connected in the later `more-wild-entry.md` increment.

- `ConfigManager.GetMoreWild` 236b7a0: configuration +18, Gimrol +18, list +C0, first integer. Recovered field is `Gimrol.MorgKilpRimgg[0]`.
- `OnBeforeShow` 23d5c54 reads the free flag and clears isClick. Free claim text is `CLAIM`; paid text is `<sprite name="tc_btn_bofang">CLAIM`.
- `OnClickButton` 23d5ee8: rejects isClick, but 23d5fec explicitly stores **zero**. Do not copy the More Spin latch. `ClaimBtn` plays click and hides the finger before branching.
- Free claim 23d606c–23d6110 increments PlayerData +80 (`GuideStep`) before calling the MoreWild setter with the live configuration value; then hides. The setter replaces the balance, not adds to it. Its existing notification-before-save order is preserved.
- Paid claim calls the unchanged ad facade with placement and scene `morewild` (ELF GOT 4f1e990). Success 23d639c tail-calls PlayAd 23d6230: reads live GetMoreWild, sets balance, hides; no guide increment. Failure 23d63a0 clears isClick.
- Close plays click then hides; it does not advance the guide or grant a reward. Unknown names do nothing.
- OnHide 23d631c calls PoolManager.GuideHide. OnAfterShow 23d5e04 creates the masked guide only for the free variant, and shows the finger at the claim text transform. These view behaviors still need production integration.
- Cancellation is the existing reconstruction lifecycle boundary for GM rebinding: old callbacks cannot grant into a discarded session. No SDK implementation changed. The native lack of a click latch is retained; existing LocalAdFacade still rejects concurrent ad requests, as before.

Validation: `RecoveredMoreWildClaimTests` covers free reward replacement and guide/event/save order, three ad failure outcomes and retry with live configuration, close while ad pending, unknown button, and GM cancellation. Result recorded after the test run below.

Unity 2022.3.62f3 PlayMode focused result: `Artifacts/more-wild-claim.xml`, **5/5 passed**, process 52384 exited. Prior production regression remains 373/373 from the first Spin guide increment; this increment has not run the full suite and does not yet wire the new claim model into production UI.

## Production window and second guide integration

Subsequent increment imports UIMoreWildView's native RectTransforms, Image/TMP/Button/HorizontalLayoutGroup properties into `MoreWildWindow.prefab`. Its native popup property uses the same dbb670 literal (300, Black) as More Spin, with the same base 0.65 black Image/Button mask and 0.3-second OutBack/InBack content animation. The native title shine uses the already recovered effect with the original defaults (factor .5, width .25, angle 135, softness/brightness/gloss 1, duration 2, loop).

The popup dragon reuses the recovered four-clip NPC rig; five Wild previews import the original idle rig with weighted meshes, original layout sizes/pivots, and animation data through the existing Unity UI rig author. These are popup illustrations, not replacement UI implementations of the core board objects. `prepare_more_wild_art.py` flattens source mesh influences without changing positions/weights. TMP Wild icon metrics retain 76x69, bearing (14.4,54), advance 96, scale 1.5.

ARM 23d5e44..23d5ee0 corrects the earlier prose: **both guide and finger are free-only**, since the non-free branch jumps directly to return at 23d5ed8. GuideInfoCollect on the actual ClaimBtn has guideID 2. The shared guide now accepts the actual step and lifts/restores that Button; finger is attached to its TMP child. No background reveal listener was invented.

CheckBaseEnd 23c673c compares GuideStep with 2 and passes boxed true into ShowWindow. ELF GOT 4f1e2d0 resolves exactly to `Method$Game.UI.UIManager.ShowWindow<UIMoreWildView>()`. It then immediately starts the Spin hint, clears Main.isSpin at 23c67d4, and saves at 23c67f4; it does **not** await claim/close. CoreRound now follows that ordering. Earlier bank/review/cashout waits are still not fully recovered. The main paid MoreWild button/progress entry remains pending; the paid window itself is available to the existing local facade and tested independently.

Validation: `Artifacts/more-wild-window.xml` **1/1 passed** (process 50376 exited), then full PlayMode `Artifacts/more-wild-regression.xml` **379/379 passed** (process 46872 exited). The new integration test drives the real first Spin through reward windows into free Extra Wild, checks actual EventSystem hits (claim allowed, close/Spin blocked), verifies guide/button restoration and saved grant semantics, retries a failed paid claim without advancing the guide, and starts the following Spin with one Wild consumed. Latest `Artifacts/current-more-wild-guide.png` was captured from this production scene and inspected. No SDK code changed.
