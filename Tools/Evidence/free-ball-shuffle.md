# Free ball type list recycling

Native FreeSlotGameResult.RandomBallInfos (0x2382e90) checks whether the supplied
shared pool index is at least the current ball-type list count. If not, it
returns false and consumes no randomness. Otherwise it calls
GameUtils.ShuffleUnity<Int32Enum> (0x2635804) and returns true; PoolManager then
resets its index to zero before reading GetBall and increments it after spawn.

The shuffle source is in the reverse project's
`native-functions/dependency/2635804.c`. It walks i=0 through Count-1, inclusive,
reads the current element, samples Random.Range(i,Count), and swaps with that
index. The last iteration is retained. It does not draw replacement ball types,
alter their multiplicities, or regenerate the Free result. Empty lists return
true for index >= 0 but perform no random draws; no placeholder ball is added.

RecoveredFreeSpinResult.RandomBallInfos now implements this exact operation.
Tests cover counts 0, 1, 7 and 15, in-range and negative indices, exact
exhaustion, oversized indices and repeated recycling. They compare native
list-based ordering and subsequent RNG values, and check that amounts and
the actual result board remain unchanged.

This is a required data dependency for the pending pooled Free ball creation
consumer. It does not itself spawn an effect or complete the Free gameplay loop.
SDK handling remains unchanged.

Validation: Unity 2022.3.62f3 full PlayMode suite **268/268 passed** in
`Artifacts/free-ball-shuffle-tests.xml`. No visual behavior was changed in
this data-only operation.
