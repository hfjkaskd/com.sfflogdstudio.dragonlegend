# Free rolling effect distribution

Native evidence: `FreeSlotGameResult.GetRandomEffectShow` ELF RVA `0x2382f08`
and `CheckSingleSymbol` `0x2382b24` in the current recovered native functions.

Each request clears the shared placement reservations and creates a fresh 5x3
zero-filled board. It places the existing CoinAmount as symbol 9 first, then the
existing BallAmount as symbol 11. Each placement draws a column in [0,5), retries
full columns, then draws among unoccupied rows in ascending row order. Coin and
ball placements share reservations and cannot overlap. It does not sample new
amounts, normal symbols, or ball types, and does not replace the actual result.

RecoveredFreeSpinResult now exposes this operation and shares its single-column
placement implementation with the actual result generator. The existing
incremental generator remains unchanged in ordering and completion behavior.
Display requests require actual generation to have finished; this prevents
interrupting its shared reservations. Completed generated results have at most
15 explicitly placed specials. Invalid excessive result counts still remain
pending under the existing frame-budgeted generation policy.

The display operation preserves the native fresh board allocation while reusing
the existing fixed reservation/row buffers instead of temporary native lists.
It is intended for reel effect refresh events, not per-frame calls.

Tests compare eight consecutive display requests against a separate list-based
native reference for empty, partially filled and full boards. They check exact
random consumption, fresh board identity, unchanged actual symbols and reward
types, and reject a request during unfinished generation.

This restores the missing data operation only. The fifteen Free reel prefab
layout, initial special effects, rolling effect consumer, Free view switch and
production FreeEntry binding remain incomplete. SDK handling is unchanged.

Validation: Unity 2022.3.62f3 full PlayMode suite passed **259/259** in
`Artifacts/free-effects-render-full-tests.xml`. An initial `-nographics` run
crashed in native Sprite rendering during the suite's camera-render tests; the
completed run used the normal graphics device. No visual change is claimed.
