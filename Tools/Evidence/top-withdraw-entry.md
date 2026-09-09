# Main top withdrawal entry

The recovered BalancePanel is the original Main/Node/Top rectangle
(224738892719839630). Its CashOut subtree (224293198502218400) was absent.
BuildTopWithdrawEntry.Save now extracts that original subtree and inserts it
at the source sibling index 2, retaining size (430.15,109.55), top-center
anchors/pivot and position (5.45,0). Original font, material, 39-point CASH OUT
label, zjm_tx_01 sprite, and the recovered ef_sltxan animation are used.
See top-withdraw-art.md for the independently checked animation data.

WithdrawBtn retains its full-stretch source hit rectangle, position
(-4.1029053,0), and size delta (-94.3672,0). In the original it is an empty
graphic sibling of three visuals. To meet the user's explicit prefab/Button
hierarchy rule, the authoring operation reparents those visuals beneath the
standard Button while preserving their world geometry. Only the transparent
target Image receives raycasts; visual children cannot enlarge the hit area.
This is a deliberate structural adjustment required by the user, not a claim
that the source used this exact child hierarchy. Runtime creates no static UI.

CoreRound binds this Button to the same OpenCashOutFromMain method used by
CashOutb, matching the common native OnClickButton branch (23bd4e8). It caches
the Button reference and removes the listener on teardown, because GameEntry
clears its BalancePanel property before releasing CoreRound. Existing cash-out
window caching, popup depth and SDK handling are preserved. No IsA-based
destination branch has been invented; full NeedWithDrawOpne/SDK routing remains
outside this change under the user's SDK exclusion.

The local GM toggle previously occupied the new native entry. Its authored
position is moved to the left background strip, top-left anchor/pivot with
position (8,-125), retaining size (96,64) and its debug sorting depth. A first
attempt at the top right intercepted the More Wild close area and was corrected.

The production entry test exercises top-button pointer clicks at the normal
layout and simulated safe inset, cached-window return, original geometry/text,
both entries sharing the existing destination, and old listener removal during
GM profile changes. Balance resource checks include the added original sprite
and label, with an explicit transparent-button exception. The GM fixture now
raycasts at the rectangle center rather than a pivot boundary.

Initial full PlayMode process 11952 exited: 458/461 passed, 97.3965388 seconds.
The three failures were the old resource counts, the GM fixture pivot-edge hit,
and the actual More Wild close-area overlap. Final validation follows below.

Second full process 43212 exited: 460/461 passed, 104.0266647 seconds.
All three corrected cases passed, including main entry, GM and More Wild.
The remaining failure was the Base BigWin fixture's end-state assertion.
That fixture replaced Board cells after actual Spin but left the independently
generated ScatterCount uncontrolled; CoreRound passes that count to Free entry.
Its intended Base-only premise was therefore not guaranteed. The fixture now
finds a non-Free seed with the existing result model, applies it to the actual
Spin, and asserts actual ScatterCount < 3 before its existing BigWin board
selection. Production result generation and continuation are unchanged.

Fresh current-main-cashout-entry.png from the second run was inspected: the
original green top button, icon and text are visible, and GM occupies the left
background strip. This is a current-engine render inspection, not proof of
full-screen original APK parity.

Final graphics-enabled PlayMode process 17436 exited. The full suite in
Artifacts/top-withdraw-final-tests.xml passed 461/461, 98.3128073 seconds,
including the actual Base seed assertion, cash entry, balance resources,
GM controls, More Wild guide, Free routes and reward-return integration.
This establishes covered behavior only; complete game lifecycle, regional
routing and visual equivalence remain unproven.
