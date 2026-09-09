# Spin button audio ordering

Native UIMainView.OnClickButton 23bd4e8 identifies SpinBtn through GOT 4f1e348.
At 23bd7bc..23bd7cc it plays GOT 4f1cf38 (`click`) before checking Main.isSpin
at 23bd7d0 and SpinCount at 23bd814. Thus busy and zero-count clicks still request
click audio. A zero-count request then takes the existing MoreSpin branch.

On an accepted spin, PlayerData.isStartSpin (+91) is set if needed, then native
SDK tracking occurs. SDK handling is intentionally unchanged/skipped here. The
next audio call at 23bd904..23bd90c plays GOT 4f1e350 (`spin`). This precedes
setting the main busy flag and starting the button visual animation at 23bd96c.
Both calls use PlaySound, not PlaySound1. Names were verified by ELF RELA and
ScriptString metadata rather than inferred from the exported audio filenames.

RecoveredSpinEntry now exposes ClickSoundRequested before its guards and
SpinSoundRequested after isStartSpin assignment, before StartVisualsRequested.
The existing extra result.IsGenerating guard remains unchanged. CoreAudio binds
the two events to prefab-authored click/spin names and unsubscribes on GM release.
No new Inspector event, SDK call, debit, save, random draw or visual object is added.

Tests extend the native-order unit fixture with click/spin/start ordering, verify
busy and empty requests emit only click while keeping player JSON unchanged, and
check duplicate requests during generation do not debit again. The production
Spin Button test clears the channel before each event, verifies AudioSource
playing afterward, and expects two clicks but only one spin sound/debit for a
same-frame double click. Existing Free return/save and GM rebuild checks also run.

This resolves the accepted-Spin audio gap recorded in core-audio.md and
core-reel-audio.md. Unmatched peripheral events, source catalog membership and
waveform/device comparison remain outside this increment; full parity is unproven.

Validation: Unity 2022.3.62f3, graphics enabled, Artifacts/spin-audio.xml reports
7/7 passed in 5.24551 seconds. Unity process 32980 exited. No SDK behavior changed.
