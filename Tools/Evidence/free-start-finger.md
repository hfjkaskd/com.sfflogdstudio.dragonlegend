# Free start hand and current regression baseline

Before this change, the complete current PlayMode suite on revision 10c7bbc
passed 449/449: Artifacts/core-current-regression.xml, 87.2747094 seconds,
Unity 2022.3.62f3 with graphics enabled. Process 43828 exited normally. This
verifies the implemented test scope after the core audio and Bonus hint changes;
it does not establish all lifecycle, country or visual parity requirements.

The audit found another missing hint consumer. UIFreeSpinStart.BeforeShow
23b1478 calls PoolManager.ShowFinger with AdTxt.transform (+80) and retained
FingerObj (+b0). RecoveredFreeStartPopup previously emitted FingerRequested only.
It now holds a prefab reference, creates/reuses the actual Finger under AdvertisedText,
sets scale one and anchoredPosition zero, activates it and restarts its loop.
The original shared ef_shouzhi art and native Unity animation remain unchanged.

Both accepted button handlers hide the hand before click sound and their existing
ad/plain paths. Failure 23b1b38 writes only isClick=false; it does not restore the
hand. The recovered handler preserves that behavior. Reopening reuses the same
hand and restarts its animation. No new SDK operation, save or count adjustment is
introduced. Static visual hierarchy remains prefab-authored, and input remains
on the original standard Buttons.

The full authoring method and incremental SaveFinger both configure the reference.
The existing popup fixture now checks actual parent, visibility, transform, hide
on claim, retained hidden state after ad failure and reuse on reopen, alongside
its existing ad success/count/timing/plain-close checks. Fresh popup renders are
generated from the current project, not historical screenshots.

Post-change validation: Artifacts/free-start-finger.xml passes 3/3 (14.6053942
seconds): popup behavior, actual Spin/Free/return and all four actual minigame
routes. Process 35368 exited. The fresh current-free-start-popup-initial.png was
inspected and shows the animated hand on the FREE+4 button. The 449-test baseline
preceded this isolated change; it was not rerun afterward. Neither this image nor
these tests establish complete original-app pixel/device or full lifecycle parity.
