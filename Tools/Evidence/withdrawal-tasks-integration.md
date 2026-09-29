# Withdrawal review tasks

The project's `cp_test.json` Rgpggm section matches the original bundled
`GoldenDragon_default.json` exactly. Keep these values in configuration.

| Stage | $500 | $1,000 | $3,000 | $5,000 | $10,000 |
| --- | --- | --- | --- | --- | --- |
| Spin | 20 | 40 | 60 | 80 | 100 |
| Watch ads | 10 | 15 | 20 | 25 | 30 |
| Claim Jackpots | 10 | 15 | 20 | 25 | 30 |
| Claim BigWins | 10 | 15 | 20 | 25 | 30 |
| Claim Treasures | 10 | 15 | 20 | 25 | 30 |
| Play FreeGames | 4 | 6 | 8 | 10 | 15 |

Each stage has an independent 86,400-second wait. The failure/review branch then
enters stage 6, which reads the collected treasure records and configured collection
size. It does not turn completion into a successful payment.

Native evidence: UIAccountView continuation 0x239fd44..0x239fe28 selects the first
positive task; its callback 0x239e57c writes step/count/time/isCashout. CashOutBottom
0x23a0eb8 reads task and wait conditions; 0x23a41f8..0x23a4460 selects subsequent
steps. The existing success-branch argument-order quirk is preserved.

The former local submission adapter wrote step 1000 immediately and hid the task
panel. It now records `task_review` and starts the configured review branch after
local acceptance. Request IDs remain stable through transport retry. Review state
is persisted separately from payment approval. Legacy unversioned local pending
orders with step 1000 and isCashout=false migrate once, retaining their original
start timestamp, payment type and balance. Versioned terminal orders are untouched.

Accepted base spins and treasure presentations now reach the existing task
receiver. Successful rewarded ads also count; failed or duplicate callbacks do not.
Existing Jackpot, BigWin, FreeGame and interstitial routes remain connected.

The original prefab provides the task/checkmark/countdown/CONTINUE layout. Returning
from background refreshes the bottom's UTC-derived remaining time. Card countdown initialization
uses remaining time, and recycled cards restore their task labels after status mode.
ScrollRect is suspended during window scale animations so it cannot clamp content
against the collapsed viewport and displace the first tier when reopening.

Validation covers all five tiers through all six counted stages, collection-stage
entry, reload, timer boundary, legacy migration, transport retry, active-scene Spin
and rewarded-ad callbacks, and rendered account/task UI. The production adapter
is still LocalCashFacade; no live payment service was introduced.

Final verification: Unity 2022.3.62f3 PlayMode, 38/38 passed. Results: Artifacts/withdrawal-complete-tests.xml. Portrait capture: Artifacts/cash-withdrawal-tasks.png.
