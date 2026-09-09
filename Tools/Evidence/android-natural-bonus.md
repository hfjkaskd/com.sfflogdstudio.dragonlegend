# Natural Android collection, Bonus and Base return

Baseline dcf786a; same installed ARM64 reconstruction APK and existing player
save. The independent device session continues from bonus-scene-input.md.
No board, random seed, Bonus readiness or save values were overridden.

The preceding record had SpinCount=9, LimitSpinCount=21, cash
670.3333740234375 and BonusArea=[2,1,3,5,4]. Physical device UI taps, with a
fresh screenshot inspected between dependent actions, produced:

- Paid round 22: no reward, Spin 8, bank 22, level 8 exp 0/8.
- Paid round 23: coins displayed $0.14 in column 2, $0.20 in column 3 and
  $0.10 in column 4, numbering columns from one. MEGA WIN displayed $0.44.
  The second column had been the only incomplete collection column. Selecting
  Only $0.44 at device (720,1930) led through the production transition to
  Bonus with LUCKY DRAW CHANCES (0/6).
- Six cards were clicked individually, inspecting each reveal: first row left
  to right, then the first two in row two. Reveals were Bao, Cai, Bao, Cai,
  Cai, Cai. The first Bao and Cai lit the corresponding GRAND indicators;
  subsequent duplicates advanced MAJOR/MINOR indicators. The sixth reveal
  completed MINOR and opened the $0.50 Jackpot popup.
- After the delayed plain option became visible, Only $0.05 was selected at
  (720,2125). The popup closed and Bonus showed 6/6, with advertising icons on
  the six remaining cards and a visible top-right Close. This checks the
  native plain Jackpot branch, not a full $0.50 advertised claim.
- Close at (1360,170) returned to Base. All ten collection lights were empty,
  Spin stayed 7, bank stayed 23, the original round bottom remained $0.44,
  and the main balance displayed $7.19.

The returned app-private playerData.d snapshot has BonusArea=[0,0,0,0,0],
GreenCount=719.3333740234375, SpinCount=7, LimitSpinCount=23, BankCount=23,
Level=8 and LevelExpCount=1. Thus the net saved balance increase is 49 internal
units: the ordinary 44 plus the plain Jackpot 5. Bonus did not consume another
paid spin. The following actual Spin consumed one (7 -> 6), advanced the paid
round counter to 24 and bank to 24, settled without reward and left collection
empty. The next screenshot shows idle Base and GOOD LUCK.

Evidence is under Artifacts: android-collection-22.png,
android-collection-23.png, android-natural-bonus.png, android-natural-card1.png
through android-natural-card6.png, android-natural-minor-ready.png,
android-natural-minor-claimed.png, android-natural-bonus-return.png and
android-natural-bonus-next.png. The last two have corresponding *-prefs.xml
snapshots. These current reconstruction screenshots are not original-game
visual parity references.

ADB transport disconnected intermittently. A separate local server port 5038
was tried, but that server also sometimes disappeared; reconnecting remained
necessary. The endpoint was 127.0.0.1:16416. A failed screenshot never justified
replaying a delivered tap; live command sessions were polled to completion.
No cause for the host-side ADB lifetime is established by this observation.

No runtime or SDK change was needed for this path, and no Unity tests were
rerun for this evidence-only commit. This verifies one naturally triggered
Bonus session ending after the free choices. Paid extra-card branches, automatic
twelve-card completion and Bonus/Free interleavings are not proven by this run.
