# Application focus audit and core scope

Runtime baseline: 1ba758f485e96aad786351efa1ffd67e12908556.
This is a source audit, not an Android background/resume execution test.

## Native entry

GManager.OnApplicationFocus, RVA 2370de4, logs loss of focus and returns
(tail branch 2370f64 to Debug.Log 492c0ec). On focus gained it logs, obtains
EventDispatcher and Array.Empty<object>(), and dispatches event string "13"
(tail branch 2370f3c to DispatchEvent 23f78c4).

The direct ARM body contains no timeScale write, audio-state adjustment,
SpinCount recovery, reward replay or save. This finding applies to this
method; it is not a claim about every SDK, OS or Unity lifecycle callback.
The decompiler follows tail calls into adjacent implementations, so appended
logging/dispatcher bodies must not be interpreted as additional GManager code.

## Known listeners

Searching the supplied native-functions/game corpus for PTR_DAT_0501bdb0
finds the dispatch above and these three registration sites:

| Registration | Delegate metadata pointer | Resolved callback |
| --- | --- | --- |
| CashOutBottom.Start 23a0910 | 0501d788 | CashOutBottom.RefreshGoldTime, 23a09fc |
| CashOutItem.Start 23a4bb8 | 0501d940 | CashOutItem.RefreshGoldTime, 23a4de8 |
| UIDailyTaskView.OnInit 23ad898 | 0501dc80 | UIDailyTaskView.RefreshGoldTime, 23ad990 |

Direct ARM confirms AddListener tail calls at 23a09f4 and 23ad988.
CashOutItem's third listener uses event "13" and pointer 0501d940;
its other two listeners have different event IDs. The daily task callback
checks the config type before refreshing its time display/countdown.

The four application-focus*-references.json files were generated with
Tools/audit_main_click_references.py against the supplied ELF and metadata.
They preserve the ELF/source hashes and resolved references. Because the
resolver scans decompiled bodies, extra references from followed tail calls
are retained and do not extend the original function boundary.

## Current difference and priority

The current Assets/Whitebox/Runtime tree has no OnApplicationFocus,
OnApplicationPause or Application.focusChanged hook. Consequently the native
focus-gained event and its known peripheral refresh callbacks are not wired.
This remains an explicit parity gap, deferred under the user's core-gameplay
priority; no speculative core reset, catch-up or reward replay was added.
SDK behavior is unchanged.

Current mixed-path coverage reaches BigWin, optional ready Bonus, Free intro,
one fixture-shortened actual Free round, total, Base return and another paid
Spin. RecoveredBigWinFreeChainTests deliberately sets FreeSpinCount to 1 on
ChangeMusicRequested. It therefore does not establish that every awarded
Free round completes in one continuous combined path. Expanding that core
coverage takes precedence over implementing these peripheral focus refreshes.

No production assets/code changed and no new Unity run was made for this
audit. The previous full 463/463 run recorded in main-ui-hierarchy.md remains
historical evidence, not new verification of background/resume behavior.

Follow-up: mixed-bigwin-free.md now records a third combined-path case that
preserves the full awarded count and completes six Free rounds, including
four coin and six ball rewards. The two earlier shortened cases remain.
This addresses the specific full-count coverage gap above for that session;
the peripheral focus-hook difference remains deferred.
