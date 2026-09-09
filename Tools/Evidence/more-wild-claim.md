# Extra Wild claim logic

Native source: current `mumu-current/native/game` ARM64 disassembly and ELF relocation-backed literals. This increment restores the claim model and configuration accessor. Production window, main entry, and second guide integration remain pending; this is not a completed playable Extra Wild branch.

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
