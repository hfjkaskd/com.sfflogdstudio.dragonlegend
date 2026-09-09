# Current full core regression and original APK launch check

Baseline aa8dae241790a172726484ae739ec8a30a023119, verified on 2026-09-09.
Unity 2022.3.62f3 process 34908 exited after the complete PlayMode suite:
Artifacts/core-current-full-tests.xml reports 468/468 passed, zero failed,
duration 126.7447918 seconds. XML SHA256:
9bc7db47a24e54cd7673a4a2469751944c42f00f7d1bbced470c3fbb7e89c91b.

This includes the current intact-guide and post-guide continuous sessions,
natural collection into Bonus, completed-session scene reload, elapsed Spin
recovery through the bundled-default loader, mixed BigWin/Bonus/Free branches,
GM selection and existing visual fixtures. Their fixture limitations remain
applicable; a full suite pass is not proof of complete original-game parity.
The run regenerated current scene captures, including current-main-free-mode.png
at 16:21:20 UTC. No runtime, prefab, SDK or antivirus setting changed this round.

## Fresh original-app capture attempt

The available MuMu ADB device was emulator-5556, SM_A156E, 1440x2560.
The old export endpoint 127.0.0.1:16384 refused connection. The available device
did not have the original package installed. Installation of both original
exported APKs succeeded, and Android resolved and launched:
com.sfflogdstudio.dragonlegend/com.unity3d.player.UnityPlayerActivity.

Original APK SHA256 values:
- base.apk: 5ca3e87c8724509294e272d99406a6adfef8e2903a118451b2c928eb9abec405
- split_config.arm64_v8a.apk: 0016945b4c573f10f001d4eddaeda185af6029305777ff03e65d821c21fc522e

The freshly captured Artifacts/original-current-start.png at 16:20:34 UTC
shows the Google Play page headed "Get this game from Play", naming Golden
Dragon Legend, with a Get game button. It does not show the gameplay board.
Screenshot SHA256:
cc80781c440859f45bcd45af91824d8b2ebe75eb1dc3200e30c9e870dd47b581.
No original app data was cleared and no installation-check/SDK code was patched.
This records the observed launch outcome without asserting its internal cause.

Therefore a same-stage original-APK versus current-scene visual comparison
remains outstanding. Old screenshots were not substituted. Core verification
can continue using native code, authored assets and the current production scene;
the unavailable original gameplay capture does not block all implementation work.
