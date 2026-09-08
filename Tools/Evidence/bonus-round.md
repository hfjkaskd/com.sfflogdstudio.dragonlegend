# Bonus deck and jackpot consumption

Source root: `C:/Projects/Nut Sort Relax/reconstruction/mumu-current`.
Business bodies below are in `native-functions/game`, schema/enum names in `analysis/dump.cs`.

## Configuration and hidden deck

- `236b9dc` (`ConfigManager.GetBonusAllReward`) clears its scratch list, draws one
  `UnityEngine.Random.Range(0, Ronig.Ltoo.Count)` index, then reads the **same index**
  from Ltoo, Qoi, Jin, Roo, Rgkorp. The returned list is reused. These are correlated
  deck compositions, not independent symbol weights.
- `2399e84` (`UIBonusView.InitBonusResult`) expands those counts in numeric order
  **1,2,3,4,0**, corresponding to ZHAO, CAI, JIN, BAO, Reward. Nonpositive counts
  contribute nothing. It then swaps each index i with Random.Range(i,count),
  including the final index. No amount is drawn for the Reward cards here.
- `236bd18` returns `Ronig.RrggRimgg[0]`.
- `236bd7c` selects a weighted row from RgkorpKgiitr, gets Jimp[row] != 0, and draws
  integer amount from RgokrpMin[row] through RgkorpMoj[row] **inclusive**. Upper bound
  addition is signed unchecked int32. This is a separate item-animation operation.

## Matching and destinations

`239b698` and `23997b4` construct the reference and mutable patterns in this order:

| Jackpot enum | Pattern | Target indices |
| --- | --- | --- |
| Grand 1 | ZHAO, CAI, JIN, BAO | 0,1,2,3 |
| Major 2 | BAO, BAO, BAO | 0,1,2 |
| Minor 3 | CAI, CAI, CAI | 0,1,2 |

`239bfdc` selects the first mutable pattern still containing the card's type.
It records the clicked index and refreshes the chance display before launching
CheckJackPotReward. `239c4bc` picks the first unused matching position in the
original reference pattern, marks it used, removes one occurrence from the mutable
pattern, and passes `(card type, jackpot type, remaining.Count == 0, target,
callback)` to BonusItem.PlayBonusAnim. One card consumes **one** target in **one**
jackpot. A duplicate CAI/BAO can still qualify for another jackpot after Grand has
used its occurrence; after all matching occurrences are exhausted it has no jackpot.
Reward cards have no jackpot destination. The deck is not redrawn on click.

## Presentation boundaries still to implement

- `239bcac` checks isClick, hides/kills the finger hint, sets isClick true and disables
  the selected native Button before calling ClickBonus.
- `239afc4` rejects already recorded indices. Free clicks invoke the acceptance
  callback directly; ad-gated clicks invoke it only on ad success.
- `239c364` ad success hides that item's ad marker, then invokes acceptance.
  `239c3f4` failure re-enables the Button and calls ShowFinger; it does not record
  the card. Its interaction with the finger/input latch still needs complete tracing.
- `239a810` changes to ads and shows Close **when clicked count equals** configured
  free count. All unselected items show the ad marker. This is a sticky flag, not
  a `>=` expression evaluated on each click. OnBeforeShow hides Close; the full
  reuse/reset lifecycle of isNeedPlayAd is not yet verified.
- `239bc14`, called by the item animation, checks clicked count against BonusCount,
  marks isEnd and launches HideBonusView when equal, then clears isClick and shows
  the finger. Recording the final index must not itself complete the window source.

`RecoveredBonusRound` implements the hidden deck and target consumption only;
it intentionally has no money credit, ad gate, animation completion or source
completion. Actual Bonus prefab/item animation, cash claim, exit transition and
the main-flow connection remain required. SDK handling remains the existing facade.

## Verification

`RecoveredBonusRoundTests` exercises all six shipped configuration snapshots,
correlated rows, card multiplicities, Unity RNG consumption through the final
shuffle call, Grand priority, ordered duplicate targets, independent jackpot
completion, exhausted matches, duplicate clicks and reinitialization. It checks
that accepting all cards consumes no additional RNG and that cash bounds are
inclusive with any nonzero Jimp treated as true.

Unity 2022.3.62f3 PlayMode run `Artifacts/bonus-round-tests.xml`: **230/230 passed**.
This validates the data addition and existing regression suite; it does not prove
the still-unimplemented Bonus presentation or full-game fidelity.
