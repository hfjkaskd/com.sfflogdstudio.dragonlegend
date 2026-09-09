# Android natural BigWin: failed reward ad, retry, next Spin

Project baseline 5636f71. Existing installed reconstruction runtime remained
unchanged; recent intervening commits only added tests/audits. MuMu instance 1,
Android 15, 1440x2560, ADB 127.0.0.1:16416. Initial current screen and actual
PlayerPrefs retained completed paid round 24: Spin 6, GreenCount
719.3333740234375 ($7.19), GuideStep 3, BonusArea all zero. No scene reset,
save overwrite, board fixture or RNG override was applied on device.

One actual touch of Spin generated round 25. The board showed coins $0.10 and
$0.19, two Scatter symbols and highlighted matching 10 symbols. It naturally
opened Big Win with $0.32 displayed and CLAIM x2. No Free entry was observed.

Tapped the visible Claim control, waited for actual GM ad controls, then
tapped Ad failed. GM controls disappeared, Big Win remained, and the complete
URL-decoded playerData.d record was unchanged from the pending-ad snapshot.
Tapped Claim again and verified GM controls reappeared. Tapped Ad reward;
the popup exited and Main returned with Spin 5, bank 25/30 and BonusArea
[0,1,0,0,1]. Saved GreenCount became 782.6666870117188 ($7.83), a delta of
63.33331298828125 internal units (about $0.633333). Bottom reward shows $0.63.

The displayed $0.32 is rounded, so using it as an exact input and expecting
$0.64 would not be a valid independent reward oracle. Current
RecoveredBigWinClaim multiplies OriginalReward by AdvertisedMultiplier before
formatting; the device result is consistent with an unrounded original of
about 31.6667 internal units. This run did not independently read that private
runtime original value or re-evaluate the full native board reward. Therefore
the exact multiplier is not claimed solely from rounded screen numbers.

Next actual Spin was accepted and completed: Spin 4, bank/paid count 26,
GreenCount 793.6666870117188 ($7.94), bottom reward $0.11 and BonusArea
[0,1,0,1,1]. This verifies actual device retry and return/continuation with the
unchanged simulated ad facade; no real ad network or monetary transfer occurred.

Fresh evidence under Artifacts:
android-core-resume.png / -prefs.xml;
android-core-round25.png and android-core-round25-settled.png;
android-bigwin-ad-visible.png, android-bigwin-ad-failed.png,
android-bigwin-retry-pending.png, android-bigwin-ad-return.png,
android-bigwin-next-spin.png;
android-bigwin-before-prefs.xml, android-bigwin-failed-prefs.xml,
android-bigwin-return-prefs.xml, android-bigwin-next-prefs.xml.
Only freshly captured screens were inspected. No APK, SDK or runtime changes
were needed, and no additional Unity tests were run this turn.
