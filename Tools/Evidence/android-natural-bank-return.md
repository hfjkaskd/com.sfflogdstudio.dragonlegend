# Android natural bank event and next paid round

Validated 2026-09-09 on MuMu Android Device-1, 1440x2560, reconstruction package. Runtime APK SHA256: `af071891c01772a257aa5f954f8c799ab82bb0c2f41294aa7f80bc2cc2b6ec38`. Repository baseline: `24aace9`. No runtime changes, save replacement, board override, or RNG forcing in this verification.

The existing session continued from paid round 26 through actual Spin button taps. Rounds 27–29 returned without a payout popup. Round 30 showed MEGA WIN $0.57. Selecting its ordinary reward opened the bank's LUCKY BONUS window naturally, after the round reward; bank progress reset to 0/30 and Spin count was zero.

Tapped the rightmost bank ball (device 1200,1800). Its reward displayed $0.18, the other two balls showed ad indicators, and OPEN / NO,THANKS appeared. Tapped NO,THANKS (720,2470): the window closed and returned to the main board. Read-only Android PlayerPrefs snapshots confirm:

| Stage | GreenCount (internal units) | SpinCount | BankCount | LimitSpinCount |
| --- | ---: | ---: | ---: | ---: |
| Bank entry, before selection | 850.6666870117188 | 0 | 0 | 30 |
| Ordinary bank exit | 868.6666870117188 | 0 | 0 | 30 |
| Next paid round settled | 868.6666870117188 | 9 | 1 | 31 |

The bank reward increased GreenCount by exactly 18 internal units. At zero spins, tapping Spin opened MORE SPINS +10. GET NOW exposed the existing GM ad completion controls. Completing GM: Ad reward closed the window and displayed SPIN 10. One subsequent actual Spin completed with GOOD LUCK, SPIN 9, level 9 progress 1/10 and bank 1/30. No duplicate bank reward was observed.

Fresh local captures are in `Artifacts/`: `android-bank-round30.png`, `android-bank-natural-entry.png`, `android-bank-first-selection.png`, `android-bank-plain-return.png`, `android-bank-zero-spin.png`, `android-bank-refill-ad.png`, `android-bank-refill-return.png`, `android-bank-next-spin.png`. Read-only snapshots: `android-bank-before-selection-prefs.xml`, `android-bank-return-prefs.xml`, `android-bank-next-prefs.xml`.

This establishes the current Android ordinary bank selection/exit and zero-spin replenishment path, not every bank ad branch or original-game pixel parity. SDK behavior is unchanged; the refill uses the existing simulated ad facade. The preceding complete Unity suite remains 474/474; this evidence-only change does not require rebuilding the unchanged APK.
