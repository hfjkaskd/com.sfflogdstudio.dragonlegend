# Original withdrawal icon animation

The missing Main CashOutb icon is ef_tixianicon, from ReferenceOriginal/Res/Spine/按钮/tixian. Tools/extract_coin_effect.py consumed the complete 1366-byte original skeleton, version 4.1.24: one idle animation, duration 2 seconds, six timelines. The checked-in ef_tixianicon.json retains the source hash, setup, atlas regions and animation data.

The original Main icon RectTransform 224486456068359781 has size (187.14287, 187.14285), pivot/anchors (0.5, 0.5), anchored position (5, -8), unit scale and rotation. Its SkeletonGraphic declares startingAnimation=idle, startingLoop=1, timeScale=1, unscaledTime=0. BuildCashOutEntryArt.Save authors the reusable icon at the original rectangle size using the existing RecoveredRegionRig, Unity Animation and RecoveredRegionAnimator path. The parent button's position remains the responsibility of the forthcoming entry prefab. No Spine runtime or other plugin assembly is introduced; the atlas is loaded by the existing Resources-based rig.

Tools/prepare_cashout_entry.py rejects non-region attachments and independently samples the extracted source at all animation keyframes, near-keyframe boundaries, zero, .173 seconds and the final frame. It produced 15 geometry samples in cashout-entry-poses.json. The existing popup-art regression now compares the Unity rig vertices against these samples and checks zero allocation across 1000 warmed samples. Unity author PID 38004 exited successfully.

This is a reusable animated resource, not yet a connected Main CashOutb button. Button hierarchy, visibility initialization and the local withdrawal route remain the next integration work. See main-cashout-entry-audit.md for the NeedWithDrawOpne versus IsA distinction and SDK exclusion.

Final validation: Artifacts/cashout-entry-art-tests.xml passed all 14 popup-art tests in 2.8626907 seconds, including the new original-icon geometry case and existing animation/rendering cases. Unity PID 50144 exited. No new original-APK screenshot comparison was performed.
