# CashOutBottom local initialization conditions

Native references: `CashOutBottom.InitUI` ELF RVA `23a0eb8`, ARM file
`reconstruction/mumu-current/native/game/023a0eb8.asm`, corresponding native-function C.
ARM boundaries and branches take precedence over inferred C types.

`RecoveredPlayerProgress.GetCashOutConditions` supplies the local initialization
snapshot for the future recovered bottom panel. It does not save, debit, create a
record, start a timer, submit an order, or authorize payment.

- Initialization resets completion flags and remaining time, finds the first record
  with the selected id, and hides all four lines.
- With no record, ARM `23a145c..23a1460` compares live GreenCount with the target and
  branches `b.ge` to the sufficient-balance path. NaN therefore takes the insufficient
  branch. Insufficient balance displays only Line4; sufficient balance displays
  Line4 and the Line3 action row. Both completion flags remain false.
- An ordinary record shows Line1 (task), Line2 (wait), and Line3 (action). The action
  row is visible even when a task or wait remains incomplete. This is visibility,
  not payout permission; click continuation still needs recovery.
- `isCashout` selects success/fail task goals. Step 6 instead uses the raw number
  of PlayerCollectDatas entries and configured CollectInfos count. Duplicate ids
  count separately; quantity within each entry does not enter this comparison.
- Task completion is count >= goal. Remaining time is the unchecked 32-bit result
  of configured wait + record.time - current Unix seconds. Wait completion is <= 0.
  These two flags are independent.
- Step 1000 enters native SDK order lookup. The snapshot reports RequiresOrderStatus
  and does not synthesize any order status or action-row availability. Existing SDK
  treatment is unchanged.

Important follow-up: `RefreshGoldTime` RVA `23a09fc` does not repeat full InitUI.
Its expired branch changes the text but does not set completion flags/checkmarks.
Do not automatically reapply this initialization snapshot on every clock refresh.
The original click listener `23a2034` plays click and invokes async
`OnClickCompeleTask` (`23a1d84`); its state-machine continuation remains to be recovered.

Validation: Unity 2022.3.62f3 PlayMode `RecoveredCashOutConditionsTests`,
`Artifacts/cash-bottom-conditions.xml`: 11/11 passed, process 38248 exited.
Coverage includes exact balance threshold/NaN, all four task/wait combinations,
over-target counts, negative remaining time, first duplicate selection, success
configuration, raw collection entries, SDK boundary, and integer overflow.

This is a tested model addition. The original bottom prefab, runtime timer,
click continuation, full withdrawal window, and core cash-prompt binding are
not yet connected by this change.
