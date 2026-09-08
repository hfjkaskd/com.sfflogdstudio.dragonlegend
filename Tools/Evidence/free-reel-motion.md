# Free mini reel landing and return motion

Source: RollReel.ConstantSpeedRoll MoveNext 0x2377bf4, Free branch after
0x23780c8; StopSlotRoll 0x2375fbc; StarSpin MoveNext 0x23789bc;
SetStop MoveNext 0x2378388. The Free speed is ELF float 0xdbc64c = 5000.

Every FreeMiniReel now authors RecoveredFreeReelMotion and the existing shared
RecoveredBaseReelMotion movement engine. The latter's historical name is retained
to preserve its existing Base prefab/script references. Free result application
uses an explicit action; Base result lookup and motion behavior remain unchanged.
FreeReels serializes all fifteen motion references in the same column/row order
as its reels and binds each to the shared special pool during initialization.

StartSlotSpin resets presentation and starts the 5000 pixel/second sine-out
acceleration. A stop requested during acceleration does not cut acceleration
short. Constant speed detects the flag, yields one frame, then performs landing:

1. Clear active rolling coins from this reel's seven slots.
2. Clear active rolling balls from these slots.
3. Read the current actual result at this column and row.
4. ID 9 creates a coin at slot zero with isInit=false (appearance and scale pulse).
   ID 11 creates a ball there with isInit=false and the shared shuffled type index.
   An ordinary ID updates only slot zero with SetImg(..., true, false): sharp Free
   image with the cover active. Other six definitions remain unchanged.
5. Return the moving node to zero over 2*abs(offset)/maxSpeed using OutBack.
   The return completion clears both spinning and stop-requested flags.

The landed special is registered separately from incidental rolling effects,
matching the native effects dictionary's first-slot entry. Native Dictionary.Add
rejects a second special landing before Clear; this is not silently overwritten.
On the next wrap, EffectsClearRequested returns the registered effect first,
then the existing coin/ball cleanup runs. Active pool ownership is removed at
return so this ordered cleanup cannot release the same object twice.

SetStop uses the existing native Unity Update-loop scheduler: scaled delay,
flag assignment, a separately queued flag wait, and callback after the return.
No parallel coroutine/DOTween/UniTask replacement scheduler was introduced.

Tests exercise all three landing branches on the actual FreeReels prefab,
5000-speed acceleration samples, the one-frame result wait, unchanged six tail
definitions, reused coin/ball instances, shared ball-index wrap, OutBack
overshoot, final flags, next-wrap pool cleanup and paused delayed callbacks.

Scope: the independent mini reel can now spin, land its actual result and return.
FreeRoll three-row callback aggregation and the Free StarSpin async wrapper are
now connected (see free-columns.md). Stop presentation is connected
(free-stop-presentation.md). Still pending: reward collection/flight, full FreeAutoSpin,
mode view switching or production FreeEntry binding. Source Free StarSpin waits
for !isStop, not !isStartSpin; that unusual startup completion must be preserved
when integrating the higher-level wrapper. SDK behavior remains unchanged.

Validation: Unity 2022.3.62f3 full PlayMode suite **285/285 passed** in
Artifacts/free-motion-final-tests.xml. The fresh 1080x720
Artifacts/current-free-actual-stop.png was inspected at original resolution:
the bottom-left reel has landed its appearance coin with the reward label
hidden and the scale pulse still active; the other fourteen cells retain their
initial presentation. The visible 96.3 labels are authored initial text, not
calculated gameplay rewards.
