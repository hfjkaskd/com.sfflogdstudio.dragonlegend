# Android zero-spin replenishment and resumed paid round

Baseline 3db6cfa; continuing the installed ARM64 development APK documented in
coin-display-accumulators.md. No save edits, forced boards or random overrides.
SDK behavior remains the existing local ad facade. These are device UI checks,
not evidence that a real advertising SDK delivered an ad.

After paid round nine gave no reward, round ten displayed BIG WIN $0.23.
The plain claim was tapped at device (720,1930). The resulting screen displayed
$5.60, SPIN 0 and bottom $0.23. Its actual saved GreenCount is
559.6666870117188; the formatted dollar label must not be mistaken for a
rounded integer save. LimitSpinCount=10, BonusArea=[2,1,1,3,2], BankCount=10,
Level=5 and LevelExpCount=3.

1. Tapping Spin at (1280,2420) opened MORE SPINS, offering +10. The underlying
   board and SPIN 0 remained visible.
2. Tapping GET NOW at (720,1890) requested the existing local ad. The GM ad
   failure control at (970,110) completed it as failed. The window remained
   open and Spin stayed zero. The entire decoded playerData.d JSON in
   android-zero-spin-failed-prefs.xml equals android-zero-spins-prefs.xml.
3. Closed the GM profile overlay, tapped GET NOW again and observed another
   pending ad. Tapping GM: Ad reward at (470,110) closed MORE SPINS and showed
   SPIN 10. Comparing every saved property before/after this completion finds
   only SpinCount changed, from 0 to 10.
4. Tapping Spin once resumed normal gameplay. The next settled screen shows
   SPIN 9, $5.71, one $0.11 coin and bottom $0.11. The saved record has
   GreenCount=570.6666870117188, LimitSpinCount=11, BankCount=11,
   Level=6, LevelExpCount=0 and BonusArea=[2,1,2,3,2]. Thus the claim did not
   itself consume a paid round, and the next round consumed exactly one.

Fresh captures in Artifacts: android-zero-spin-attempt.png,
android-zero-spin-gm.png, android-zero-spin-failed.png,
android-zero-spin-retry.png, android-zero-spin-reward.png and
android-spin-eleven.png. Save snapshots use the corresponding *-prefs.xml
names. Captures are of this installed reconstruction, not original parity
references. Bonus has not yet naturally triggered in this device continuation.

The existing RecoveredMoreSpinClaim matches the inspected original success
callback 023d5b2c.asm: get live SpinCount at 23d5bd0, GetAddSpins at 23d5bfc,
add at 23d5c00, set SpinCount at 23d5c0c, then Hide at 23d5c24. Original
failure callback 023d5c2c.asm resets its click latch at +0x80, matching the
successful retry observed above. No runtime change was required by this check.
The last full PlayMode result remains 469/469; no tests were rerun for this
evidence-only change. Full lifecycle and visual parity remain outstanding.
