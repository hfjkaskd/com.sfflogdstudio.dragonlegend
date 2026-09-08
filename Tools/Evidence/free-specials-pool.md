# Free reel special generation and recycling

RecoveredFreeSpecials is now authored into FreeReels.prefab. The normal
Initialize(catalog,result) path uses it for IDs 9 and 11; the explicit callback
overload remains for controlled initialization tests. The component owns native
Unity ObjectPools and a prefab-authored inactive storage child. It subscribes
each mini reel's coin-clear, ball-clear and fake-effect refresh events once.

Native source:

- PoolManager.CreateJinBi 0x238b040 and CreateLongzhu 0x238b480 parent the
  effect under the chosen SymbolItem, set scale .7/.8, hide its first child,
  and reset anchored position. In world coordinates the centered anchor is
  .86 units above the SymbolItem's bottom pivot.
- Initial coins keep their authored initial state; rolling coins call
  PlayShow. Balls use RandomBallInfos(shared index), reset index to zero only
  on true, read GetBall, call Init and then increment. Clearing a reel does
  not reset the shared index.
- ClearJinBi 0x238b2c4 and ClearLongzhu 0x238b798 iterate the supplied slot
  objects. They find one active child effect per slot, return it to its pool
  and restore the first child. They do not clear other reels. If a slot has
  multiple active effects, only the first is removed in that pass.
- CheckFakeCoin(false) 0x23757dc obtains a fresh GetRandomEffectShow board on
  each request. Its three-iteration flag loop reads the same [column,row]
  cell each time. Thus coin and ball flags are mutually exclusive, and a
  special performs exactly one Random.Range(0,4) slot draw. It does not scan
  all three result rows or reuse one random board across the fifteen reels.

The implementation caches active effects by slot instead of repeatedly
searching the hierarchy. Native private tracking lists retain stale effect
references because Clear removes the parent slot rather than the effect;
their externally relevant membership check is represented by pool ownership.
No growing duplicate history is needed for the same active-child behavior.
Static storage, prefab references, placement and scales are authored assets.
Bindings and per-slot lists are allocated at initialization/first use and
reused; rendering remains native world geometry with UI only for labels.

Tests initialize the actual FreeReels prefab with six coins and seven balls,
check exact positions, hidden source sprites, ball-type traversal, isolation
of reel clearing and object reuse. A fully coin-filled result verifies the
first and second wrap events, fresh display-board consumption, next RNG value,
and reuse of the same coin in the selected rolling slot. The current mixed
board preview is Artifacts/current-free-board-specials.png.

Per-mini-reel mesh and label clipping is now connected; see
free-specials-clipping.md. Independent landing/return motion is connected
(see free-reel-motion.md). Column stop coordination and result-layer placement
are connected (free-columns.md). Remaining: collection/reward motion, complete mode view
switch and production FreeEntry binding after Bonus. The prefab can now
initialize and refresh actual specials, but this does not establish a working
production Free lifecycle. Reuse behavior beyond the exercised initial and
rolling paths also remains subject to source comparison. SDKs are unchanged.

Validation: full Unity 2022.3.62f3 PlayMode suite **280/280 passed** in
Artifacts/free-specials-full-tests.xml. The fresh 1080x720 actual initialized
FreeReels mixed-board preview was inspected at original resolution.
