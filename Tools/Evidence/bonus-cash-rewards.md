# Bonus cash continuation

Source: current reverse `native-functions/game/2396d54.c`,
`2396730.c`, `23d87e0.c`, `23d89c4.c`.

After the existing cash card turn, delayed reward draw, hold and glow startup,
BonusItem branches on the configured Jump flag (any nonzero configuration value).
For a normal cash card it invokes input release first, then dispatches event 1
with three arguments: float reward, null completion callback, item transform.
There is no direct balance mutation here. The source transform must survive the
binding into the shared cash flight; the current main-only GameEntry helper
does not yet accept this extra origin and must not be reused unchanged.

For Jump, another scaled 0.2-second wait starts after turn readiness, independently
of the text scale animation. ShowWindow<UIRewardView> receives the reward and a
float callback. Callback 2396730 ignores the float and releases input. Merely
showing or hiding the popup does not release input: UIRewardView.OnAfterHide
dispatches a cash flight, and its arrival callback 23d89c4 invokes the supplied
float callback with winCount. The shared flight then resets the title and credits
the current balance, as documented in the existing cash flight evidence.

ELF RELA plus script.json resolves pointer 0501cfb8 to string `1`, 0501d2e0 to
UIManager.ShowWindow<UIRewardView>, and 0501d288 to callback 2396730.

RecoveredBonusCashRewards drives the actual authored BonusItem turn. Its prefab
stores the post-turn popup delay; runtime contains no static UI construction.
It exposes the exact fly origin and completion slot, retains input while the
popup is pending, and cancels delayed work on disable. It forwards item failures
and unsubscribes completed jobs. No SDK or balance behavior is replaced.

The PlayMode test uses the actual BonusItem prefab and deterministic reward
configuration to verify early release-before-flight, negative nonzero Jump,
scaled post-turn delay, pause, deferred release, duplicate callback suppression,
and cancellation before RNG and before popup opening.

Full Bonus window binding, UIRewardView presentation/claim flow and the origin-aware
cash-flight connection are still required. This is the card continuation, not a
claim that the complete cash lifecycle or visual comparison is finished.

Verification: full PlayMode suite `Artifacts/bonus-cash-tests.xml` passed
235/235, zero failures. Authoring run `bonus-cash-author.log` exited 0.
The shared RecoveredCashFlightPresenter already accepts an optional source;
the pending full-window binding needs to pass it through, along with the actual
top window and its main-window flag.
