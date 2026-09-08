# Bonus exit and completion source

Authoritative input: current `reconstruction/mumu-current/native-functions/game`
and `analysis/script.json`, plus the current arm64 ELF. This extends the window
and world transition recovered earlier. Main CheckBonus is now connected through
RecoveredBonusFlow; see `bonus-flow.md` for the current boundary and tests.

## Native ordering

- `239b3a4`: after the empty BaseWindow.OnClickButton (`23f9330`), only CloseBtn
  with `isEnd == false` proceeds. It sets `isEnd` (+110) immediately, creates a
  cancellation source and starts CloseBonusView. It does not inspect `isClick`.
- Final-card release `239bc14` sets the same flag before calling HideBonusView.
- HideBonusView state machine `239d1e8`: wait **2 scaled seconds**, create the
  cancellation source, start CloseBonusView. This is not an immediate hide.
- CloseBonusView `239ccc4`: Update-timing WaitUntil reads `isEnd` through
  `239bc88`. Its token argument is not passed to WaitUntil (native token is zero).
  If `isClose` (+128) was already set, do nothing; otherwise set it, PauseMusic,
  PlaySound("transform"), then launch GManager.PlayZhuanChang without awaiting it.
- The first callback `239bc90` hides this window through UIManager. PopupWindow
  ExitAnimation (`23f9ccc`) resets scale to toScale and tweens to fromScale with
  the configured .3-second InBack curve. After this animation, the object becomes
  inactive and OnAfterHide cancels the hint sequence/hides the finger.
- Second callback `239bc98` starts WaitZhuanChang (`239d5f4`): read current
  GameData.GameSlotType (+2c), choose normalBg for zero, freeBg otherwise, then
  wait **.2 scaled seconds** before resolving the context completion source.
- The independent world transition callbacks are at .8 and 3 seconds; its
  animation ends at 3.333333492. Window hide, source completion and the visual
  endpoint therefore are three distinct milestones.

ELF RELA addends resolved against metadata (Ghidra pointer addresses):

| Pointer | Metadata/string address | Value |
| --- | --- | --- |
| 0501d5a0 | 050387a0 | callback 239bc90 |
| 0501d5a8 | 050387a8 | callback 239bc98 |
| 0501d5b8 | 050643f8 | transform |
| 0501d5d0 | 050623e8 | normalBg |
| 0501d5d8 | 05060400 | freeBg |

Ghidra float 00ebc540 / ELF 00dbc540 is .20000000298023224.
The automatic wait uses immediate 0x40000000 = 2.0.

## Implementation and verification

`RecoveredBonusExit`, authored as `RecoveredUI/BonusExit.prefab`, lives outside
the window so hiding the window cannot terminate source completion. Bind accepts
the real window, world transition, progress and caller completion. It uses the
existing native Update wait runner, preserves the duplicate-close guard and reads
slot mode at the second callback. Runtime teardown cancels its pending work.
It does not restore Spin availability or credit cash.

`RecoveredBonusWindow` code-binds CloseBtn to the shared isEnd flag and implements
the source scale-out via serialized duration/ease. No close-click sound is added
to the empty native BaseWindow callback.

`RecoveredBonusExitTests` uses actual prefabs, CloseBtn and all twelve card turns.
It verifies manual vs automatic timing, duplicate manual clicks, scaled pause,
window inactivity after the cover plus .3 seconds, music at the second callback,
late Base/Free mode selection, and exactly-once source completion .2 seconds later.
Neither hide nor completion is stubbed. These checks prove this exit segment,
not the still-unconnected whole-game lifecycle or complete UIManager stack parity.

Unity 2022.3.62f3 full PlayMode regression passed **248/248** on 2026-09-08:
`Artifacts/bonus-exit-full-tests.xml`.
