# Bonus window and its payout group

Native UIBonusView.OnInitProperty ARM 239949c loads the eight-byte literal dbb670 into UIWindowProperty +0x10/+0x14: (300, 2), Popup with Black mask. UIRewardView 23d814c and UIJackpotView 23b2b0c use the same pair. The native active-window maximum is queried within the window-type root; see core-popup-groups.md for UIManager and ELF evidence.

BonusFlow previously instantiated directly under GameEntry, outside the recovered PopupRoot, and displayed its window at fixed depth 300. The real Spin-to-Bonus fixture now opens More Spin during the pre-Bonus wait and requires Bonus to open above it. Artifacts/bonus-popup-depth-before.xml failed 0/1: both were 300. Unity PID 15324 exited.

GameEntry now instantiates the existing CoreRoundFlow prefab before BonusFlow, allowing BonusFlow to instantiate directly beneath its authored PopupRoot. CoreRoundFlow still binds after the dependent Bonus/collection systems have bound. No static hierarchy is constructed at runtime. Bonus and its two payout windows notify the shared depth helper before activation, and CoreRoundFlow removes their subscriptions at teardown. The existing Bonus ring delay, independent transition callbacks, card selection, reward handling, and return wait are unchanged.

The regression retains its actual Spin trigger, Bonus reset, camera-stack captures, card selections, cash-flight completion, return timing, and no-Bonus synchronous branch. It now asserts Bonus above More Spin and any exercised cash/jackpot payout above Bonus. This does not integrate Base BigWin/Jackpot windows, which remain separately owned by the playfield, nor establish complete visual parity.

Final validation: Artifacts/bonus-popup-depth-regression.xml passed the full PlayMode suite, 457/457, zero failures, in 94.1880786 seconds. Unity PID 8088 exited. The fresh Artifacts/current-bonus-flow-window.png was visually inspected: Bonus fills the portrait view above the retained underlying popup, with the authored card grid and hint finger visible. This is current-project validation, not a fresh original-APK visual comparison.
