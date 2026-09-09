# Main cash-out finger lifecycle

Native evidence in `C:/Projects/Nut Sort Relax/reconstruction/mumu-current`:

- `native-functions/game/23b9ee4.c`: Main.ShowCashOutFinger scans for the first tier without a record of that ID, regardless of record state. A balance below its threshold returns without hiding the existing finger. When every tier has a record, the cached finger is hidden.
- `native/game/023b9ee0.asm`: OnGreenCountChanged branches directly to ShowCashOutFinger; the event arguments are not the balance source.
- `native-functions/game/23bb0ec.c`: OnBeforeShow performs the same initial check.
- GameData.set_GreenCount, RVA `236d788`, dispatches the change event before assigning the new balance. Preserve the existing setter ordering.
- PoolManager.ShowFinger, RVA `238b954`, reuses the cached object, reparents to the source target, resets local scale and anchored position, activates it, and restarts its looping animation.

`RecoveredCashOutEntry` now binds/unbinds the balance event and performs the initial check. The authored entry references the existing native Unity Finger prefab; its rig and Animation playback are reused. The original `CashOutBtn` field identifies the child `finger` target, not the clickable button.

The real GameEntry regression verifies the old-balance threshold timing, unchanged visibility after decreasing balance, all-recorded hide, cached-object reuse, parent/position/scale, real button raycasts, cached CashOut opening, and GM teardown. A fresh active-finger render is written to `Artifacts/current-main-cashout-finger.png`.

Validation: author PID 43564 exited successfully. Full PlayMode run PID 42644 terminated with 459/459 passing in 95.7685791 seconds (`Artifacts/cashout-finger-tests.xml`). The fresh active-finger screenshot was inspected: the finger points at the right-hand cash-out entry. This change does not establish complete region/A/B visibility parity or implement SDK payout routes.
