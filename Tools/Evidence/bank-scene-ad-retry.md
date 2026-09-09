# Bank scene input: failed advertisements and sequential collection

The first bank window sequence in `RecoveredBankWindowTests` now routes actual scene raycasts to the authored ball, OPEN, and GM ad controls. It waits until each requested Button is the top `IPointerClickHandler`; there is no direct callback fallback for these actions.

Covered sequence: first free ball, second ball advertisement failure, retry and successful second reward, OPEN failure, retry and final reward, automatic close. Failed advertisements preserve the current balance and flight count. The individual failure removes its selected index, allowing the same visible ball to be retried. The OPEN failure releases its continuation latch. Successful sequential completion produces three flights and the balance increase equals their total.

Native cross-check: `reconstruction/mumu-current/native-functions/game/23933f8.c` removes the failed manual selection index. `2393870.c` checks selected-count against item-count before its half-second closing delay. This is not a completed-flight counter.

The initial strengthened test failed with expected 39 versus balance 23 because it advanced from the second flight's launch using a fixed frame delay, then checked the total when the window closed. That overlap is not evidence that the missing amount was permanently lost. The corrected sequential test waits for `CashFlight.ActiveCashCount == 0` before selecting the final ball. No production timing, closing rule, or SDK behavior was changed. Rapid overlapping flight completion after window close remains a separate verification boundary.

Unity 2022.3.62f3 PlayMode targeted result: `Artifacts/bank-scene-ad-arrival-tests.xml`, 1/1 passed, 5.5240899 seconds; process 25832 exited. This fixture still deliberately opens a bank window and seeds a later bank/review boundary. Natural Android triggering is separately recorded in `android-natural-bank-return.md`. The rest of this existing test includes direct callbacks; this result is not a claim that all its actions use scene input.
