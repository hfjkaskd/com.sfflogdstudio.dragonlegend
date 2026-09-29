# Native task audit and corrections

## 2026-09-16 user-requested task state correction

The historical native visibility behavior described below is superseded for daily
task presentation. Missing or incomplete progress cannot be claimed and shows a
disabled CLAIM button, without a tick or received overlay. Reaching the configured
goal enables CLAIM. Only an existing isRecieve receipt shows the tick and hides
CLAIM. The prefab uses the standard Button color transition for its disabled state.
Receipt reservation, reward amounts, ad callbacks and existing saves are unchanged;
older legitimate receipts are retained to prevent duplicate payouts.


Configuration remains the user-selected non-organic cp_test (Ripg=default).
No goal/reward values were invented or replaced with the organic snapshot.

## Verified differences corrected

- TaskItem.ClaimTaskReward 23ad164 calls GameData.ClaimTaskReward 236f210 before
  choosing the jump branch. It saves isRecieve without crediting GreenCount.
  The previous implementation marked and paid at ad success. Now reservation
  precedes the reward popup; duplicate claims are rejected even before cash lands.
- Relocation 4f1d2e0 -> 5038a40 resolves to UIManager.ShowWindow<UIRewardView>.
  The separate simplified task reward screen has been replaced with the existing
  recovered UIRewardView presenter/claim logic, including entrance, delayed plain
  option, amount animation, exit, cash-out tip and cash-flight settlement.
  That popup uses the original lucky/lucky rewarded placement and iv_close/lucky
  plain interstitial. The direct jump=0 branch uses iv_close/task (literal at
  4f1dc78 -> 50640d0). Rewards use configured base amount with 2 / .5 only inside
  UIRewardView; direct claims use the full configured amount.
- GameStart callback 23879fc increments login task 6 before checking LoginTime.
  LoadScene 2389e50 clears and re-adds login only when the local date advanced AND
  Ripg equals default. Organic retains its records across startup dates. Removed
  the synthetic DailyTaskDay field and global periodic reset.
- UIDailyTaskView.OnBeforeShow 23adce0 hides the timer for non-default. For default,
  CountDown 23ada40 computes seconds to the next local midnight; WaitTime uses
  scaled tween time. Callback 23ae59c clears all task records at zero, hides the
  timer and refreshes rows, without granting login or immediately saving. The
  countdown continues after closing the window, like the original global tween;
  reopening recomputes the deadline. Progress mutations retain normal saves.
- TaskItem.Init 23acdf8 uses GetClaimTask 236ee6c for claim/tick/black visibility.
  Its missing-record branch returns true and clicking shows the exact incomplete
  task tip (literal slot 4f1dc70). Existing incomplete or received records hide CLAIM
  and show tick/black. This surprising branch is preserved, not redesigned.
- Task list cell spacing is 250 (23ae138, float 437a0000), replacing invented 260.

## Already matched

Task IDs 1–5 are fed by recovered Big Win, Jackpot, Free Game, Slots mini-game and
Bonus entry events. ID 4 specifically advances in CheckSmallGame 23cfe28; ordinary
main-reel spins do not advance it. Login is ID 6. Existing native first-increment,
clamping, and claimed-record behavior in SetTaskData is unchanged.

## Validation and boundary

Artifacts/task-native.xml: 73 PlayMode cases passed, including receipt persistence
before payment, duplicate rejection, default/organic startup dates, ad failure and
retry, double and half cash-flight payouts, and scaled countdown clearing while
the window is hidden. Visual capture: Artifacts/daily-tasks-native.png.

Ad transport remains the existing local simulator. This audit restores the task
call sites and callbacks, not an unavailable third-party SDK or live server state.
TASK entry currently uses static original atlas regions; the original icon's
Spine animation is not part of this behavioral alignment.

Validation for the 2026-09-16 correction: the real runtime and PlayMode test
assemblies compiled successfully with Unity 2022.3.62f3's compiler (two existing
unrelated member-hiding warnings). An isolated C# behavior harness ran the actual
progress, task item Refresh, window Claim/timer and task reward window methods:
789 assertions passed across current hybrid and original high-reward task settings.
The previous implementation fails the same harness at missing-record claimability.
Checks cover incomplete progress, reaching each goal, receipt persistence,
duplicate claims, reopening, serialized reload, midnight reset and retained legacy
receipts. See daily-task-state-validation.json. UI/SDK peripherals were memory
stubs; this is behavior validation, not a Unity rendered-screen or device test.
No APK was rebuilt and no user save was changed.
