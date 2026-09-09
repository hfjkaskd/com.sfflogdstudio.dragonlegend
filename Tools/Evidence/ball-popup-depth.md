# Free-ball branch window group integration

Current native ARM OnInitProperty listings identify these window properties at UIWindowProperty +0x10/+0x14:

- UILuckySpinView 23b60ec, UIWheelView 23dd4e8, UIRewardView 23d814c, UIJackpotView 23b2b0c load dbb670 = (300, 2): Popup, Black mask.
- UITreasureView 23dbd04 loads dbc1f0 = (300, 1): Popup, Normal transparent mask. The existing treasure author already uses Color.clear for its input blocker; its animated black background is separate.

The ELF literal mapping and BaseUIManager active-group depth algorithm are documented in core-popup-groups.md. The four branch flow roots are now authored beneath the same PopupRoot as Free entry/exit and the connected core settlement windows. Their owned prefab windows remain intact. Actual Show methods notify CoreRoundFlow before activation; Lucky Spin and Wheel pass their visual window Transform rather than their always-active controller wrapper. Shared Reward and Jackpot popup components expose the same boundary, with subscriptions limited here to these branch-owned instances. CoreRoundFlow removes all seven subscriptions during Unbind.

This also orders the Slot reward over its still-exiting machine window and either Wheel payout popup over its still-visible wheel. The native non-awaited transition timing remains unchanged. No extra reward, altered RNG draw, or new SDK behavior is introduced.

Before: Artifacts/ball-popup-depth-before.xml failed 0/1, LuckySpinSourceImport depth 300 versus More Spin 301; Unity PID 38704 exited. Author PID 37640 exited successfully. The actual four-branch core test now leaves More Spin open, checks displayed windows above it and payout popups above their underlying branch window, and claims through EventSystem raycasts in a 1080x1920 render target. Its existing exact-once cash credit, reward collection, Free end, busy-state and return-to-Base assertions remain active. Other separately owned windows, including Base reward/Bonus and collection, still require group audit.

Final validation: Artifacts/ball-popup-depth-regression.xml passed the full PlayMode suite, 457/457, zero failures, in 93.693176 seconds. Unity PID 48420 exited. This verifies the exercised four core branch routes and existing standalone payout suites; the four-branch fixture does not force every possible Wheel result. No new original-APK visual comparison was performed this round.
