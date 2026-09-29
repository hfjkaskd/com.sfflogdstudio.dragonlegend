# Android export build repair

Export: C:/Users/pc/Desktop/Build2/Gilded Dragon Odyssey

- AGP 7.4.2 bundled D8/R8 failed to dex kotlin-stdlib 2.1.21. Added a pinned R8 8.6.17 buildscript dependency to the export root and Unity baseProjectTemplate.gradle; enabled the custom base template.
- Manifest merger reported minSdk 22 below Moloco 4.12.0 requirement 23; rebuilding exposed AppLovin 13.6.4 requirement 24. Set minSdk 24 in launcher, unityLibrary, and Unity PlayerSettings.
- Kept installed ad SDKs and existing signing settings.
- Compatibility reference: https://developer.android.com/build/kotlin-support
- Validation log: C:/Users/pc/Desktop/Build2/Gilded Dragon Odyssey/codex-build-validation.log

Validation: :launcher:assembleRelease BUILD SUCCESSFUL (77 tasks). Existing release configuration uses the debug signing config; this was not changed. Non-blocking lint/JDK and compileSdk warnings remain.
