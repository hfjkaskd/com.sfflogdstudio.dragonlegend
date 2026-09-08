# Free coin scan to the shared Bonus window

AutoFreeSpin MoveNext 0x23c4758 awaits CheckPlayBonusAnim, then CheckBonusGame
0x23c6d54, before entering the dragon-ball stage. CheckBonusGame uses the same
BonusArea predicate, runtime IsBonusGame flag, task update, reset/save, NPC,
ring and transition path in Base and Free. No separate Free Bonus window is
created. Its shared exit selects normalBg or freeBg from the current slot type.
See bonus-flow.md, bonus-transition.md and bonus-exit.md for those recovered
timings and native implementations.

RecoveredBonusFlow now accepts a Free coin scanner through BindFreeScan and
subscribes its existing Begin method to scan completion. Rebinding detaches
the previous scanner; Unbind (including profile cleanup) releases this hook
alongside the existing Base playfield hook. It retains the same authored
window, NPC, collection view, cash presenter, ad facade and transition.

RecoveredFreeBonusFlowTests loads the actual GameEntry scene, instantiates
the actual FreeReels prefab, generates a full coin board, and binds its scan
to the entry's real player/rules/collection and Bonus flow. Actual five-column
stops and coin collection trigger Bonus without forcing IsBonusGame. The test
checks the 15 reward entries, area reset, Free mode retained, real Bonus window
and bonusBg, clicks the configured free cards, claims actual reward popups,
closes the window and checks a single completion with freeBg restored.

Validation: Unity 2022.3.62f3 full PlayMode suite 296/296 passed in
Artifacts/free-bonus-flow-tests.xml.

Production FreeEntry still needs to instantiate and bind its board/context,
including this connection. Subsequent CheckPlayLongzhuAnim, CheckRewardCollect
and CheckFreeSpinEnd continuations remain pending, as do actual audio playback
consumers and the previously recorded full UI/lifecycle gaps. The integration
test exercises the new connection but does not imply that the unfinished
production FreeEntry chain already reaches it. SDK handling is unchanged.
