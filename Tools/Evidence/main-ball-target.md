# Native free-ball destination hierarchy

Original `UIMainView.prefab` serializes LongzhuPos to GameObject 1770005084341472, RectTransform 224539572024531195. Its parent is QiPan (224422702022971346), not the window root. Anchors and pivot are (0.5,0.5), anchored position (-1.6201172,100), size (100,100), unit scale. QiPan is bottom-center anchored at (-0.003418,652) beneath the adapted Node. Thus its child destination follows both Node safe-area changes and QiPan shake.

The previous CoreRoundFlow builder created a root-level destination by adding QiPan's authored position to the child offset. That reproduced one static unshifted position but lost the parent relationship. `BuildMainBallDestination` now extracts the original target subtree directly into SpinPlayfield/QiPan. `RecoveredSpinPlayfield.BallDestination` holds the authored reference; CoreRound binds BallScan to it. The obsolete CoreRound target/reference are removed. The full SpinPlayfield author includes the extraction, and the full CoreRound author no longer synthesizes a flattened coordinate.

The actual four-branch core test now verifies original parent/local data, equal world displacement of board and target after a simulated safe-area change, and target movement when the board is displaced. It restores actual screen and board coordinates before traversing the existing Slot/Wheel/Treasure/Lucky routes and reward settlement. No runtime static structure or coordinate constants are introduced. This check is not a whole-game visual or device parity claim. SDK handling remains unchanged.

Author PID 20064 exited successfully. Full PlayMode validation pending.

Validation completed: full PlayMode PID 44376 terminated with 460/460 passing in 97.9499178 seconds (Artifacts/main-ball-target-tests.xml). The original hierarchy and target-displacement assertions passed together with all four actual reward routes. No full-fidelity completion claim is made.
