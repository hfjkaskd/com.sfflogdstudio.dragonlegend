# Spin input routing through the current scene

Runtime baseline c7a72e304b3983965b9409ab3a7be9e206b821d4.

Native UIMainView.OnClickButton ARM at 23bd7cc calls SoundManager.PlaySound
before reading isSpin (+1ee) at 23bd7d0. A set flag branches away at 23bd7d4;
the SpinCount read and empty-count branch occur later at 23bd814..23bd81c.
RecoveredSpinEntry.TryBegin preserves this click-sound-before-busy-check order.

RecoveredCoreRoundFlowTests previously invoked the Button.onClick event twice.
It now routes the first click and 20 immediate additional clicks through the
current scene's EventSystem: screen point comes from the actual Spin rect
center and configured UI camera; RaycastAll must hit a target whose
IPointerClickHandler is the Spin Button; ExecuteEvents delivers that click.
This verifies the current authored hierarchy admits the click, rather than
only invoking an unconditionally accessible event directly.

Assertions verify 21 click sounds but one start sound, one Spin debit, an
unchanged player record and persisted JSON after the accepted click, and
unchanged generated symbols in all 15 positions. The existing test then
finishes the Base round, traverses its fixture-shortened Free path and real
total window, checks return persistence and accepts another paid Spin.
Unchanged saved JSON proves no content mutation; it does not count identical
PlayerPrefs writes. The lower-level busy-entry test separately counts saves.

Unity process 34180 exited. Artifacts/spin-input-tests.xml passed 1/1 in
4.5376595 seconds. Production code and SDK behavior are unchanged.

## Do not infer paid auto-spin from the sequence name

Direct ARM InitSpinSequence 23ba59c appends a two-second interval and callback
23bf46c. That callback obtains Spine's parent and calls PoolManager.ShowFinger
at 23bf4e0, then stores the returned finger at +210. It does not invoke Spin.
RecoveredSpinHint implements this visual prompt. No paid auto-spin feature
was added based on the ambiguous method name.

This test covers immediate repeated clicks through the UI raycaster and
click handler. It does not simulate native mouse/touch device input,
pointer-down duration, multi-touch or every modal overlay. The sequence
inspection alone is not proof that every source input component lacks a
long-press handler. Full visual/lifecycle parity remains unproven.
