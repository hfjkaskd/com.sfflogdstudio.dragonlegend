# Interrupted first paid Spin and actual scene reload

Baseline 8cf718be070e0e422f4bdf476e0ffa604be157ea.
Native UIMainView.OnClickButton 0x23bd4e8 advances GuideStep at
0x23bda0c..0x23bda18, then calls the SpinCount setter with count minus one
at 0x23bdf2c..0x23bdf3c. GameData.set_SpinCount 0x236e3c8 writes/clamps
PlayerData.SpinCount and calls SavePlayerData at 0x236e4e4. Subsequent
experience, jackpot and bank setters are called at 0x23bdf74, 0x23bdfa8
and 0x23bdfdc. Thus accepted-input persistence is not deferred until the
whole visual round completes. Raw native/game/023bd4e8.asm and
0236e3c8.asm were inspected for this audit.

The recovered dump declares GameSlotType, FreeSpinCount and IsBonusGame
on GameData (runtime fields); these are not PlayerData save fields.
This alone does not establish every original startup or interruption path.

RecoveredInterruptedSpinLoadTests loads the production GameEntry with a fresh
record, invokes the authored first Spin Button callback and immediately checks
busy, zero completed rounds, one debit, guide step 2 and no balance change.
It checks the whole saved JSON against the current record, deactivates the
scene roots before yielding another frame, and unloads the scene. It then
loads a fresh production scene using its normal loader and verifies:

- the old GameEntry is destroyed and the progress instance is replaced;
- the full saved record survives unchanged, without another debit or credit;
- the new board is idle in Base mode and the first Spin guide is hidden;
- another accepted Spin starts and debits exactly one further count.

Unity process 43308 exited. Artifacts/interrupted-spin-tests.xml passed
1/1 in 1.2387073 seconds. No production or SDK changes were needed.
The previous 468-case full regression predates this additional test.

This test controls the fresh record and RNG seed, then interrupts before any
settlement frame. It exercises scene deactivation/destruction/recreation and
the existing PlayerPrefs store, not an Android process kill, power loss,
mid-cash-flight interruption, every random branch or original APK execution.
It does not introduce a recovery ledger or promise resumption of unsaved work.
Existing PlayerPrefs and RNG are restored after the fixture.
