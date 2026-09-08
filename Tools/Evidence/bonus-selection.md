# Bonus selection and advertisement gate

RecoveredBonusSelection implements the window's selection controller through
IRecoveredBonusSelectionView. It uses the existing deck/target model and ad
facade. The actual window presenter still needs to bind authored cards, hints,
labels, effects and exit transition; the interface does not establish full UI
or lifecycle completion.

Native sources: 23994bc OnInit, 23997b4 BeforeShow, 239bcac card Button callback,
239afc4 ClickBonus, 239bfdc accept, 239a810 chance refresh,
239c364 ad success, 239c3f4 failure, 239bc14 item release, 239b5cc AfterHide.

OnInit captures BonusNode.childCount into BonusCount (+100). Completion compares
accepted count with this authored count, not the generated reward list's length.
The controller therefore receives visibleCardCount from its future presenter.

Selection checks isClick, hides the pooled finger, kills its sequence, latches,
disables the selected Button, then rejects an already recorded index. A free
selection accepts immediately. An ad selection uses bonusCoin placement and bonus
scene; it does not consume/record the card until success. Success hides its ad
marker before acceptance. Acceptance refreshes the chance display before playing
the card continuation. The card's real release callback is required.

InitChanceCount switches to ads only when selected == configured free count.
It shows Close and ad markers on all unselected items, then formats selected/free
as `LUCKY DRAW CHANCES(<gradient="spin">{0}/{1}</gradient>)`. Counts beyond the free
threshold continue to be displayed; this is not a clamped remaining counter.
Zero free count activates this branch during BeforeShow.

Source quirks intentionally retained: 239c3f4 re-enables the Button and restarts
the finger hint but never clears isClick; the hint callbacks do not clear it
either. BeforeShow clears isEnd/isClose, hides Close and initializes items, but
does not clear isClick/isNeedPlayAd. A reused controller therefore retains the
ad flag after reaching the threshold, even though item ad markers initialize
hidden. This describes object reuse, not a claim that every window is reused by
the UI manager. No synthetic retry-unlock or ad-flag reset is added.

On item release, equality with BonusCount sets isEnd and starts HideBonusView;
only then is isClick cleared and ShowFinger called. Recording the final card
does not complete the window's UniTask source. Exit/close source completion is
still a separate pending integration.

ELF RELA/script.json resolves 0501d490 = bonusCoin, 0501d4a0 = bonus and 0501d438
to the chance format above. Tests cover exact threshold ordering, an outstanding
card blocking more selection, ad success, all three failure outcomes, deferred
last-card completion and retained state on reuse.

Full PlayMode run `Artifacts/bonus-selection-tests.xml`: 246 passed, zero failed.
Five new controller cases pass. The view is a test double here; authored full
window interaction, hint animation and source completion remain to be verified.
