# Bank flight fix: full regression and Android update

Runtime baseline `b5a1ae0`, Unity 2022.3.62f3. Full PlayMode run `Artifacts/bank-fix-full-tests.xml`: 475/475 passed, 181.7584639 seconds; process 27100 exited. Includes the new overlapping-bank-flight regression and previous core suites. This does not establish whole-game 1:1 completion.

Android ARM64 IL2CPP Development build completed with 0 errors and 0 warnings in 38.2924335 seconds; process 23168 exited successfully. APK: `Artifacts/DragonLegend-current-arm64.apk`, 129320354 bytes, SHA256 `33807eb171e11aa7084ab2092c263e79557bf831b8c26c23c532f31798ce877c`. BuildReport totalSize is a different metric from APK file length. Local build log: `Artifacts/bank-fix-android-build.log`.

ADB install -r succeeded on MuMu Android Device-1 for `com.sfflogdstudio.dragonlegend.reconstruction`. Existing data was not reset or replaced. Read-only snapshots `bank-fix-preinstall-prefs.xml` and `bank-fix-postinstall-prefs.xml` retain GreenCount 868.6666870117188, SpinCount 9, BankCount 1, LimitSpinCount 31 and Level 9. Initial capture was taken during blank startup; a subsequent fresh capture confirmed the main board ready (`bank-fix-android-ready.png`).

One actual Spin button tap on the updated app completed round 32, with SpinCount 8, BankCount 2, level 9 experience 2, unchanged GreenCount and GOOD LUCK. Evidence: `Artifacts/bank-fix-android-next-spin.png` and `bank-fix-next-prefs.xml`. This is Android installation/startup/save-continuity/paid-Spin verification. The specific rapid bank-overlap path is verified by the Unity scene tests; it has not yet been naturally reached again on this updated Android session.

Build-generated PlayerSettings materialization and URP prefilter serialization were restored after Unity exited; no intended rendering settings changed. SDK behavior remains unchanged.
