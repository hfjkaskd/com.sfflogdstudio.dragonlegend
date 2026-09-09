# Overfilled collection is valid and still triggers Bonus

The current Android Free-return snapshot contained BonusArea `[1,2,1,4,2]`. A count above the two authored visual slots is not by itself an error: presentation collection directly increments the stored value, whereas the separate GameData.SetBonusArea method stops at two. Native `23bf8d8.c` explicitly returns `1 < param_2` for CheckBonusGame's column predicate. Readiness requires every column to meet that predicate, not equality to two.

Added `RecoveredBonusFlowTests.OverfilledSavedCollectionTriggersWithoutReadyOverride`. Within the real GameEntry fixture it supplies `[2,2,2,4,2]`, persists through the actual PlayerStore, reads a separate store to verify exact retention, and verifies the runtime readiness flag is initially false. The authored collection exposes no fourth visual target. The fixture then invokes the real Spin Button callback and follows the existing production Bonus transition/card/exit sequence, including reset to five zeros, busy gating and return.

This is a deliberately seeded ready-collection fixture with a save round trip, not a natural collection session or a scene reload test. Other existing cases still use the readiness override. Its purpose is to prevent an incorrect saturation or equality-only fix in response to the observed Android values.

Unity 2022.3.62f3 targeted PlayMode run `Artifacts/bonus-overfilled-tests.xml`: 4/4 passed, 23.6483026 seconds, process 5940 exited. No runtime or SDK change was needed, so the current installed Android APK remains valid. Full whole-game fidelity is not established by this check.
