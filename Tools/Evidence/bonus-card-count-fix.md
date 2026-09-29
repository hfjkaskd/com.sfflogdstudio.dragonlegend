# Bonus reveal index correction

The recovered 12-position board indexed a shuffled reward list directly by clicked position. cp_test and the two bundled default snapshots contain correlated row totals 12, 12, 9, 12; selecting the nine-result row made positions 9..11 throw in RecoveredBonusRound.TryReveal. Native InitBonusResult (0x2399e84) expands the configured counts without padding, so blindly mirroring this configuration exposed the mismatch.

GetBonusAllReward now restores the native uniform choice across every configured row (0x236b9dc). The previous minimum-card-count filter excluded the nine-result row and biased cash outcomes. For non-organic rows, each combination again has probability 25%; the first card's cash probability is (5/12 + 5/12 + 2/9 + 6/12)/4 = 38.8889%, instead of 44.4444% with the filter. Cash amount selection is unchanged.

The presenter retains the authored card objects and hides slots beyond the generated pool size. Selection, ad markers, completion and finger hints use min(authored capacity, generated count). Thus a nine-card pool finishes after its ninth animation without indexing nonexistent rewards. Reopening with a twelve-card pool restores all twelve slots. Organic rows with sixteen results retain the existing shuffle and twelve-visible-slot behavior. This is a presentation accommodation for the recovered pool/slot mismatch; no extra rewards or row filtering are introduced.

TryReveal rejects out-of-range indices without mutating round state. Selection rejects invalid and repeated positions before disabling a Button or taking the click latch. Existing ad and payout behavior is otherwise unchanged.

Regression coverage runs all six snapshots with 64 seeds each against the native row draw, verifies exact pool sizes and reverse reveals, checks invalid indices, and verifies short-pool completion without ad markers on absent cards. Real-prefab coverage verifies nine-card slot hiding and twelve-card restoration, alongside existing BonusWindow interaction and payout tests.

Validation on 2026-09-10: Unity 2022.3.62f3 PlayMode passed all 14 tests in RecoveredBonusRoundTests, RecoveredBonusSelectionTests and RecoveredBonusWindowTests. Run in an isolated project copy; all seven edited C# files were hash-checked against that copy. Results: Artifacts/bonus-probability.xml. No installed Android build was updated.
