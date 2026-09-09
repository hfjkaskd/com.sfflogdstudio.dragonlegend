# Bonus selection hint presentation

The existing Bonus selection controller scheduled FingerRequested but no runtime
consumer displayed a hand. RecoveredBonusWindow now instantiates the already
recovered Finger prefab on first request, retains that instance, and reparents it
to the selected card on subsequent requests. HideFinger actually hides the object
before forwarding its existing notification.

Native evidence: UIBonusView.ShowFinger 239aaf8 selects an unclicked BonusNode
child. Callback 239bdfc calls PoolManager.ShowFinger(child, FingerObj) and stores
the returned object at +f8. Callback 239bea8 hides that same object and schedules
the next hint. PoolManager.ShowFinger 238b954 initializes/reparents, sets scale
one and anchoredPosition zero, activates the retained object and plays child 0's
animation looping. Existing Finger.prefab supplies the original ef_shouzhi art
and its official Unity animation; its animator restarts on activation.

The original 1.83-second show/hide intervals, random choice, selection exclusion,
card click hide/cancel ordering and close/disable cancellation are unchanged.
The hand remains presentation only; card input stays on its standard Button.
No runtime static UI construction or replacement click area is added. The window
retains one hand instead of allocating one on each cycle. The prefab field is
configured both in full authoring and the incremental SaveFinger authoring path.

The advertisement failure behavior remains the recovered source behavior:
RecoveredBonusSelection restores the card Button and hint but retains IsClicked.
This increment does not invent a retry unlock absent from native 239c3f4. SDK
facades remain unchanged. Broader mixed Bonus/Free and country lifecycle parity
is not established by fixing this visual consumer.

Validation: Artifacts/bonus-finger.xml passes 8/8 in Unity 2022.3.62f3 with
graphics enabled (process 45996 exited), covering the real Bonus window, Base
entry/exit, Free Bonus return and selection semantics. The expanded window test
checks delayed creation, unclicked card parent, zero anchored position/unit scale,
hide/reuse cycle and immediate selection hide. Its first image captured the
transparent opening pose; a fixture-only half-second wait produced the visible
animated hand. Artifacts/bonus-finger-pose.xml passes 1/1 (process 46372 exited).
The newly generated Artifacts/current-bonus-window.png was inspected: the hand
is visible over the middle-row third coin in this run. No historical original
screenshot was used, and this is not a claim of pixel parity against a live APK.
