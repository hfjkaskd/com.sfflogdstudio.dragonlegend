# Symbol-win stage: recovered selection and remaining presentation

The actual paid Spin now captures `RecoveredSymbolWinSelection` at the start of the
symbol stage, immediately after CheckJackPot's completion predicate. This is the initial
selection portion of UIMainView.CheckPlaySymbolAnim (`23cce68`), not completion of that
method. Symbol effects, temporary-label flight/counting and the Big Win window are still
required before this stage can continue to CheckBonusGame.

## Selection order

Native state-machine entry `23cd2b8` first calls SlotGameResult.GetWinTotalLine
(`2386504`). The existing recovered settlement getter raises an award strictly between
zero and one to one, updating the stored line award. Other fractions remain unchanged;
this is not general rounding. Only after that read does it add UIMainView.bonusCount
(+0x224). It does not add the earlier jackpot award to this total.

For an exactly zero total it returns without clearing the persistent list of visited
positions at UIMainView +0x240. A fresh closure still has bigWinType=none. For any nonzero
total, including a negative value, it calculates GetBigWin, obtains the existing winning
line list, clears visited positions and requests line sound only when linesWinCount>0.

It traverses winning lines in stored order, then each line's coordinate list in order.
Each coordinate reads the symbol at that position from the current board, finds that
symbol's definition, and requests the first effect prefab only for a previously unseen
coordinate. A Wild substitute therefore uses Wild's effect, not the line's regular
symbol ID. A shared cell is requested only once; all-Wild 243-path boards yield at most
15 distinct positions. RecoveredSymbolWinSelection keeps fixed 15-cell/visited arrays
instead of allocating native LINQ lists and predicates for every coordinate.

The selected first effect paths already exist as provenance in OriginalSymbolCatalog:
0 A, 1 10, 2 J, 3 Q, 4 K, 5 Yu, 6 Gui, 7 Wild1, under Res/Prefabs. These paths are not
yet converted winning-effect prefabs. Wild3 and Wild3Light are separate effects for the
preceding full-column Wild sequence and must not replace Wild1 for ordinary winning cells.

## Big Win classification

ConfigManager.GetBigWin (`236ab70`) reads GoldenDragonAutoGenConfig.Qonrii (+0x30),
then Qonrii.Riikin (+0x88). It scans from the final configured index toward zero, without
sorting. For each threshold it reads current GameData.Bet (+0x14), multiplies signed
32-bit integers, converts that result to float, and compares the reward inclusively.

Direct ELF disassembly proves the arithmetic and index handling:

```text
236ac5c ldr w8, [x0, #0x14]
236ac60 mul w8, w8, w21
236ac64 scvtf s0, w8
236ac68 fcmp s8, s0
236ac6c b.lt #0x236ac04
236ac70 cmp w20, #3
236ac74 csinc w0, wzr, w20, hs
```

For a matching index 0/1/2 it returns Big=1/Mega=2/Super=3. A matching index >=3
returns None immediately, without trying smaller thresholds. Empty lists return None.
The port preserves integer overflow before float conversion and the special zero-bet
case; it does not reinterpret these as division by bet or sorted reward bands.

## Required following presentation

RollReel.ShowSymbolEffect (`2376750`) has its own occupied-row guard, independent of
the symbol-stage visited-coordinate list. A row already occupied by an earlier effect
returns immediately. Otherwise it adds the row to the occupied list and disables the
row's first child. A null effect prefab returns after this hide; a non-null prefab is
pooled under the reel's effect parent (+0x70), moved to the hidden child's world position
and stored in the row-to-effect dictionary (+0x68). The supplied callback is null for
ordinary winning-symbol requests. The new presenter must honor existing Wild ownership.

If line award is nonzero, the next native tween targets TempWinTxt (+0x88), not the
existing DownWinText TMP label (+0x90). Its getter `23c2558` reads bonusCount; setter
`23c2570` only formats text. The target is linesWinCount, the duration is .3 seconds,
and the method waits .5 scaled seconds. Do not substitute a tween of combined total.
The method then captures TempWinTxt's world position and initializes tempWin=totalWin.
For a nonzero Big Win type it marks isBigWinPop and advances task (1,1) at this later
point, after symbol requests and the optional initial wait, not during classification.

Additional native branches must be implemented in order, after resolving their full
window and presentation context: line-label flight (.3 seconds, height 1, ease InQuad),
Big Win popup, its callback/predicate wait, any adjusted tempWin count, bottom label
update/flight and final wait. ELF constants dbc474=.8 and dbc4a4=1.1 are the two longer
scaled waits in this method. Big Win callback `23c2ac8` only clears isBigWinPop and assigns
the returned value to tempWin; getter/setter callbacks do not all mutate the same field.
Use the native bodies rather than assuming this is the jackpot cash-flight path.

In the no-Big-Win branch after the optional initial count wait, the state machine reads
current GreenCount and directly applies GreenCount + totalWin. It does not request the
ten-cash-item flight used by jackpot. This setter appears before the later tempWin/bottom
label flight portion. The Big Win branch instead opens its own window and waits for its
callback. Keep the two paths distinct to avoid double credit or delaying an ordinary
line payout until a cash animation that the original does not request.

The currently captured selection does not advance task 1, credit the line/bonus reward,
open a Big Win window, or release Spin busy state prematurely. Existing actual jackpot
flight/credit remains independent. The next work is native world-space conversion of
the winning-symbol prefabs, occupied-row lifecycle, authored temporary label presentation,
and the complete Big Win/non-Big-Win continuation, followed by Bonus, Free and BaseEnd.

## Verification scope

RecoveredSymbolWinSelectionTests checks inclusive boundaries, reverse unsorted order,
extra threshold behavior, signed multiplication overflow, zero bet, duplicate/shared
cells, live Wild identity, encounter order, minimum positive line award plus fractional bonus,
zero-total early return and bonus-only selection. The actual jackpot integration test
also verifies the captured input at the real symbol boundary. These assertions do not
claim that the remaining visual presentation or later game branches have been completed.

Validation: `Artifacts/symbol-selection-all-tests.xml` ran 197 tests with 196 passing;
all previous 188 tests, including the updated actual-entry assertion, passed. The only
failure was a new test incorrectly expecting general integer rounding of 72.9. Reading
the existing getter and native `2386504` established the strict (0,1) minimum behavior.
Only the new test and evidence wording changed. `Artifacts/symbol-selection-tests-2.xml`
then passed all nine new cases, including preserved fractional rewards and the .1->1
stored-award mutation before adding the bonus. No runtime workaround was added.
