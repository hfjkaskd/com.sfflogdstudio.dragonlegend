# Collection window joins core Popup group

UICollectView.OnInitProperty ARM 23ac2dc loads dbb670 into UIWindowProperty +0x10/+0x14: (300, 2), Popup with Black mask. BaseUIManager's group depth and already-shown short circuit are documented in core-popup-groups.md (AdjustWindowDepth 33be5f4, PrepareToShowWindow 33bdeb8).

The recovered entry previously instantiated the collection window under GameEntry and called Show on every entry invocation, including when already visible. It now receives the existing authored PopupRoot at GameEntry binding, instantiates the configured window there, and notifies CoreRoundFlow before a new show. Repeated entry activation retains the main button click sound but does not rebuild list presentation, reset the opening tween, or change depth. CoreRoundFlow removes the show subscription during teardown. Camera/scaler binding still uses GameEntry, preserving the existing native safe-area calculation.

The production collection test continues to use actual pointer input for initial opening, closing, reopening and GM switches. New assertions verify the parent group, stable scale/depth on a duplicate entry callback, and that More Spin observes collection when determining its own depth. Existing checks still cover raw collection progress, list reuse/scroll position, A/B presentation and destruction of the old profile's window. The before run collect-popup-before.xml failed 0/1 on the incorrect parent (Unity PID 27060 exited).

This closes the previously identified collection-group omission but is not proof of full game-wide UIManager semantics or visual parity with a fresh original-APK run.

Final validation: Artifacts/collect-popup-fixed.xml passed 5/5 in 16.396078 seconds: collection entry/GM ownership, both collection-window tests, actual four Free-ball branches, and core modal depth isolation. Unity PID 8040 exited. This round used the relevant regression suites; the prior full-suite result is recorded in base-popup-depth.md.
