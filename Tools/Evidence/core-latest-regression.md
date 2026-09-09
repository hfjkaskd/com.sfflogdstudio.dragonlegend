# Current core regression and onboarding audit

Runtime baseline: 1f3755946ef19b733e24e016422890d709c7d958.

Rechecked GuideTxtAnim.SetTxt 0x2372468 and MoveNext 0x2372bd4. Native writes Substring(0,index+1) before each scaled WaitForSeconds; completion follows the last wait. The initial C in a first-frame capture is expected and does not prove missing text. The current FirstSpinGuide and MoreWildWindow tests cover the real overlay, button raycasts, complete strings, first Spin, free Wild claim, next Spin, and GM parent restoration. Fresh current-first-spin-guide.png and current-more-wild-guide.png were inspected during this audit and show complete text.

Full PlayMode run Artifacts/core-latest-regression.xml (Unity PID 45268, terminal): 450 tests, 449 passed, one failed, 100.1225578 seconds. Failure at RecoveredCoinGlowTests.cs:192 was in the transfer/arrival count assertions after the fixture's settlement wait.

The wait checked scan running, both glow states, lamp flashes, and reward bursts, but omitted both active flight queues. RecoveredLampFlight emits Arrived only when its duration completes; RecoveredDownWinFlight creates its burst after releasing that flight. There can therefore be a valid interval with an active flight and no active glow/burst. The test could leave its wait in that interval and assert a completed arrival too early.

Fix: include CoinStops.ActiveFlightCount and WinFlight.ActiveCount in the existing bounded wait. Keep all exact presentation, sound, flight, arrival, vibration, amount, pooling, pixel, and unbind assertions. Add names to the transfer/arrival assertions for future failure diagnosis. No production behavior or SDK handling changed.

This audit does not establish full original-game parity. In particular, CoreRound.AfterBank still advances from review to FinishCoreRound without calling the existing PrepareCashOutPrompt model or CashPromptWindow; the withdrawal destination lifecycle also remains unconnected. Full-suite green results only validate the cases currently covered, not that missing lifecycle branch or all region routing/visual parity.

Verification after the wait correction: Artifacts/core-latest-regression-fixed.xml, Unity PID 28576 (terminal), 450/450 passed, 91.1603658 seconds. The entire PlayMode suite was rerun, including the failing coin transfer case, full current guide tests, Base/Free branches, GM teardown, board shake and anticipation geometry/visibility.
