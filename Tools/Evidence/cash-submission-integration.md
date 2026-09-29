# Cash window submission integration

The cash window previously published account and continuation events without consumers.
RecoveredCashOutWindow now owns a reusable AccountWindow prefab and TipsWindow, and
subscribes/unsubscribes the payment-header and bottom-panel routes with its binding.
The account prefab is authored from ReferenceOriginal/Res/ViewPrefabs/UIAccountView.prefab;
its static layout and controls remain in the prefab. All Button callbacks are runtime listeners.

Account editing saves the selected provider's existing record or adds one record. Blank
name/account input stays on the form. Closing discards unsaved edits. Cash submission
rechecks balance, saves a stable request id before calling ICashFacade, then persists
the returned pending state. Repeated pending submissions do not call the facade again.
A failed/lost response retains the submitting request id for retry.

The installed implementation is LocalCashFacade, a mock, and this integration is new
local behavior rather than a recovered payment service. New submissions now enter
the configured withdrawal task-review sequence, as documented in
`withdrawal-tasks-integration.md`. They do not manufacture a successful payment,
decrement currency or set isCashout. Existing nonterminal task records retain their
configured task transitions. A live service's
approval/rejection/status updates remain outside the existing ICashFacade contract.

RecoveredCashOutSubmissionTests covers response-loss retry, stable ids, persisted
pending state, insufficient balance, account upsert, and actual prefab Button routes
including account validation, submission, editing, reopen, and task continuation.

Validation: Unity 2022.3.62f3 PlayMode, 42/42 cash-out tests passed in
Artifacts/cash-flow-final.xml. Inspected Artifacts/cash-account-connected.png;
account background, input-field backgrounds, close icon and submit button render
with the restored original sprites. The final source, generated prefabs and four
sprite import settings were copied back from the isolated validation project.
