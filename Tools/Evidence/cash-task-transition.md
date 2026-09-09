# CashOutBottom task continuation

Native state machine: `23a3724` (`CashOutBottom.<OnClickCompeleTask>d__16.MoveNext`).
Captured-record mutation callback: `23a278c`. Sources are current reconstructed
ELF ARM and native-function C under `reconstruction/mumu-current`.

Implemented:

- `RecoveredGameplayRules.GetNextCashOutTaskStep` retains the native success query
  argument order. ARM `23a41f8..23a4284` loads selected id into w2 and candidate
  1..5 into w1 before `GetSuccessTaskCount`. Its signature is (index, step).
  Failure calls at `23a42f0..23a4378` instead use (selected id, candidate).
  These must not be normalized into the same query.
- All five candidate counts are read before selecting. For current steps 0..4,
  select the first later candidate with count > 0, otherwise retain terminal
  1000 on success or 6 on failure. Other current steps retain that terminal.
- Failure current step 5 still calls native GetTime7 (236d170), a direct Rimg7
  index read with no TaskTier fallback. Both positive and nonpositive values
  retain nextStep 6. The read and its bounds behavior are preserved.
- `RecoveredPlayerProgress.ApplyCashOutTaskStep` mutates the captured record
  reference: assign next step, zero count, assign Unix seconds, save once.
  Native DateTimeOffset.Now.ToUnixTimeSeconds is an absolute timestamp despite
  using Now rather than UtcNow. No currency debit, type change, record lookup,
  or payout approval occurs in this callback.

Native click routing identified for the upcoming prefab integration:

1. Find first record for selected id again at click time.
2. No record: compare current balance with target. Enough opens UIAccountView
   with object[]{bottom_type,id}; insufficient opens UIWithDrawTipView with
   ["Withdraw", formatted missing-cash message, "OK", null].
3. Existing record: inspect cached compelete1 before cached compelete2; these
   are not recomputed from live counters at click time. Incomplete task shows
   UITipsView "Please complete the task first". Incomplete wait shows
   "Please wait for " + formatted Pending Review time from remainTime.
4. Step 6 also shows "Please complete the task first", even when both flags
   are true. It does not enter the next-step selector in the native click flow.
5. Other stages calculate nextStep, show waiting, and enter account/order
   handling. nextStep 1000 reaches SDK Submit and remains excluded from local
   automatic success. Ordinary progression invokes the captured mutation,
   waits 1.5 scaled seconds, hides waiting, and dispatches refresh.

Resolved ELF GOT strings/methods through script metadata relocations:
4f1d900=UIAccountView; 4f1d908="You might just earn <color=#32b555>{0}</color> more to withdraw";
4f1d910="Please wait for "; 4f1d918="Withdraw";
4f1d920="Please complete the task first"; 4f1bd70=UITipsView;
4f1d6b0=UIWithDrawTipView; 4f1d6c0="OK".

Validation: Unity 2022.3.62f3 PlayMode, final
`Artifacts/cash-task-transition-final.xml`, 13/13 passed; PID16788 exited.
Tests cover all current-stage branches, transposed success lookup, zero/negative
task skipping, tier fallback, direct Time7 indexing, captured duplicates and
removed records, mutation-before-save, and unchanged balance/type/paid flag.

Scope: these are runtime model operations. The bottom prefab and actual button
continuation are not yet wired; this change does not establish an end-to-end
withdrawal route or alter the existing SDK facade.
