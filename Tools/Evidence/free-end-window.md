# Free end window and exit handoff

RecoveredFreeEndWindow and its prefab import the complete UIFreeSpinEndView
hierarchy, original RectTransforms, legacy Green font, TMP settings/materials,
title/YouWin/button sprites and both UIShiny settings. Source Spine components
are replaced with the already verified Unity-native ef_overtc and ef_xjpl art.
The source SpineRect field refers to the confetti node. Both title shines retain
factor .78734106, width .25, rotation 135, softness/brightness/gloss 1, looping
two-second playback, zero delay. Static layout stays in the prefab.

## Actual lifecycle

- Show plays fsend, retains the completion callback and initial spin count, hides
  the tip/button and clears the amount. The content remains scale one.
- Play start once, wait .8 scaled seconds via the recovered timing-8 runner, then
  select looping idle and activate the confetti. The first callback on the global
  animation runner plays count, reads TotalFreeSpinWin, launches the .5 OutQuad
  count from zero, and shows the tip. Changing TotalFreeSpinWin before this
  callback changes the displayed amount; changing it after does not change the
  active tween's endpoint. Currency language is read by each formatting callback.
- On count completion, PlayBtnAnim hides ContinueBtn and waits .5 on the tween
  clock. Callback 2370c6c activates it and resets localScale to Vector3.zero;
  the appended .3 OutQuad tween scales it to one. No ad or payout happens here.
- Continue uses a standard Unity Button bound in Awake. It plays click, hides
  the window immediately, then signals the retained source once.
- Native Base_HideWindow dependency 33bce80 checks IWindowAnimation. The
  UIFreeSpinEndView/BaseWindow inheritance does not implement it, so this path
  calls Internal_OnHide, SetActive(false), AfterHide immediately. The .3 Back
  animation used by PopupWindow subclasses would be a fidelity error here.
- A normal Hide does not cancel independent global animation jobs. Profile
  unbinding cancels this binding's wait/jobs and clears callbacks before reuse.

RecoveredFreeExitFlow binds the actual collector.Completed to the existing
RecoveredFreeSpinExit.Check. It uses the actual FreeEndWindow and SceneTransition
prefabs. At exact zero remaining count, the existing state object changes mode to
Base, pauses music and shows the end window. Only its close callback launches the
transition. Check completion follows launch; cover and completion callbacks remain
separate (.8 and 3 seconds). They expose Base view/reel reset, Free end flag clear,
and normalBg music consumers. The nonzero branch advances the original generated
result and connects FreeSpinEntry.PresentationRequested to the actual Free reel
controller. Negative count retains the original no-start/no-popup behavior.

## Verification and remaining work

The integration test loads the current Main scene, uses real FreeReels and their
collector, and verifies the zero-increment setter's two saves before this handoff.
It checks scaled pause, delayed/live amount capture, fixed tween endpoint, initial
spin count, staged button visibility, original currency formatting and no additional
money credit. It raycasts the real Continue button and executes its pointer click,
verifies immediate hide, transition callback order/timing, then exercises the
nonzero branch through generation and the real Free reel controller.

The current `Artifacts/current-free-end-window.png` was generated and visually
inspected after the focused integration test passed 1/1. This proves the window
renders and interacts in the Main scene; it does not prove whole-Main visual parity.
GameEntry still must instantiate/bind the production Free board and both entry/exit
flows, connect view/reel reset and flag consumers, and finish the surrounding
lifecycle audit. The audio events still need the broader sound consumer integration.
SDK behavior is unchanged.

Validation: `Artifacts/free-end-window-all.xml` passed the complete 347/347
PlayMode suite with graphics enabled after the focused 1/1 integration pass.
