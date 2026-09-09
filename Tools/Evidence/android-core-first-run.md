# Current Android ARM64 player and process restart

Baseline 06e2b6f, plus BuildCurrentAndroidPlayer.cs in this commit. The build
entry uses enabled EditorBuildSettings scenes, official Unity IL2CPP/ARM64,
Development build and a separate verification application ID:
com.sfflogdstudio.dragonlegend.reconstruction. Production runtime/SDK logic
is unchanged; the original installed package is not overwritten.

Unity 2022.3.62f3 process 39360 exited successfully. BuildReport:
Succeeded, zero errors, zero warnings, 00:04:36.3276476. Its reported total
size was 750183784 bytes; the actual compressed APK is 121559130 bytes.
Artifacts/DragonLegend-current-arm64.apk SHA256:
b9a1b1a612d956b2a8c23f7e99efce2be98e9a5f5b383467e4a50663b07bd134.
An initial compilation attempt ended on a missing UnityEditor.Build import;
the import was corrected before this successful build.

The entry restores effective identifier, backend, architecture and bundle
settings in finally. Unity can materialize previously implicit settings and
serialize URP shader prefilter fields during platform builds. After exit, this
run inspected and restored its generated changes to the six pipeline assets,
ProjectSettings.asset and ShaderGraphSettings.asset. No user changes existed
at the start. Those generated settings are not part of this commit.

## Observed device path

MuMu emulator-5556, SM_A156E, 1440x2560. Installation succeeded; process 30538
started the production GameEntry and displayed the initial Spin guide.
ADB input taps targeted visible buttons using freshly captured device frames:

1. Spin: (1280,2420). The first round reached a MAJOR $1.00 popup.
2. Its CLAIM: (720,1925). The balance became $1.00 and SUPER WIN $3.24 appeared.
3. SUPER WIN CLAIM: (720,1725). Balance became $4.24; the cash-out prompt
   appeared with Spin 9 visible behind it. No withdrawal was submitted.
4. Captured actual app-private PlayerPrefs, force-stopped the verification app,
   confirmed pidof returned no process, and launched again. New PID: 30815.
5. Captured app-private PlayerPrefs again. URL-decoded playerData.d JSON was
   exactly equal before/after restart: SpinCount=9, GreenCount=424,
   GuideStep=2, Level=2, LimitSpinCount=1. The new scene showed the idle Base
   board, $4.24, Spin 9 and no first-Spin guide overlay.
6. One further tap at (1280,2420) was accepted. The immediate external read
   still saw the prior disk value; a later read without another tap showed
   SpinCount=8, GreenCount=424, GuideStep=2, LimitSpinCount=2.

The Unity:E/AndroidRuntime:E log queries scoped to the observed app processes
returned no lines. This is a bounded observation, not proof of no platform bugs.
Fresh captures are in Artifacts/android-current-start.png,
android-current-first-spin.png, android-current-after-claim.png,
android-current-core-end.png and android-current-restarted.png. Prefs snapshots
are android-before-stop-prefs.xml, android-after-restart-prefs.xml and
android-second-spin-prefs.xml. No save/RNG override was applied on the device.

This validates an actual Android process stop at the cash-prompt stage after
the two rewards were credited, followed by startup and another accepted Spin.
It does not establish interruption behavior during a reward flight, all Bonus
or Free branches on-device, release stripping, other phones, physical finger
input latency, or original-APK visual parity. Bonus renderer selection remains
pending separately; SDK handling was not modified.
