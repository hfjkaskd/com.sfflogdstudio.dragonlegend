# First Spin guide audit

UIMainView.OnBeforeShow 23bb0ec initializes the ordinary two-second
InitSpinSequence at 23bb288. OnAfterShow 23bbfc0 separately reads PlayerData +0x80
(GuideStep). Only value 1 enters GuidePop(main transform, true), followed by
ShowFinger(Main.Spine.transform.parent, existing Main +0x210). It does not kill
the existing idle sequence. The current fresh PlayerData initializes GuideStep=1.

The recovered main previously only started the two-second idle hint. Core binding
now additionally shows the same hand immediately for GuideStep 1. ShowImmediate
does not clear or cancel the outstanding timer; its eventual callback reuses the
same instance. Accepted Spin already hides the hand and cancels the sequence,
increments GuideStep from 1 to 2 and preserves the source guaranteed guide result.
Returning players at step 2 or greater only start the idle sequence.

RecoveredSpinHintTests now verifies a real fresh GameEntry shows the hand before
the timer expires, retains its pending timer, hides/pauses/reuses that instance,
and hides on accepted Spin. After real settlement and GM reload, persisted step 2
does not create an immediate hand. Focused guide-initial-hint.xml passes 1/1.

## Still missing from the complete guide

The timing fix is not a completed guide implementation. Native GuidePop 238be4c
spawns the configured Guide prefab, resets scale to one and calls Guide.Init.
Guide.Init 2371d20 activates its Bg Button according to isBg and looks up the
current GuideStep's registered GuideInfoCollect targets. It positions its text
Node relative to the target and switches above/below using target and Node sizes
and Screen.height. Its pseudocode loses floating-point registers: use ARM to port
the coordinates, not the inferred function signature.

SetGuideInfo 23721f0 records each target's parent, reparents the **actual target**
under Guide (world position retained), then sets GuideTxtAnim text for the current
step. ResetGuideCollect 237251c restores those parents. Do not substitute a copied
visual plus detached click area. Main source registers guideID 1 on component
114389991902224113, GameObject 1291632710222552; resolve this to the actual recovered
Spin Button hierarchy before binding. Guide.OnClickBgMask 23726d4 stops the text
coroutine and reveals the full text; it does not advance or dismiss the guide.

Source Res/Prefabs/Guide.prefab includes black/background Buttons, Content,
SkeletonGraphic (ef_long), Kuang, Image and TMP. Its black alpha is 0.5019608,
distinct from popup window alpha 0.65. The serialized English text is
`Click SPIN and let the dragon breathe fire into your wins! `, but runtime SetTxt
2372468 chooses literals for steps 1/2; resolve those literals and typewriter
coroutine before authoring. GuideHide 238bf70 and the later step-2 guide callers
23d5c54 / 23d5e04 also need integration. No guessed mask or text animation has been
added by this change. Common audio and broader lifecycle branches remain pending.

Additional resolved evidence for the next implementation: ELF relocations
4f1beb0 / 4f1beb8 resolve respectively to `Click SPIN and let the dragon breathe
fire into your wins!` (no trailing space) and `Tap here to unleash extra Wilds!`.
GuideTxtAnim constructor 2372b70 sets charsPerSecond to float 0x42c80000 = 100.
MoveNext 2372bd4 clears the label, sets interval to 1/charsPerSecond, immediately
shows Substring(0,1), then yields scaled WaitForSeconds after **each** character.
The completion callback runs only after the final character's wait. It is not a
time-based multi-character catch-up loop: a slow frame resumes one iteration.

Full guide-hint-regression.xml passes 371/371. This proves the immediate-hand
change and its regression coverage, not the still missing complete Guide overlay.

## Recovered text component

RecoveredGuideText now ports the native coroutine with Unity StartCoroutine and
WaitForSeconds. It preserves the source Substring progression (including layout
changes as text grows), first character in the initial synchronous coroutine step,
scaled waits, and a wait after the last character before callback. SetText changes
stored text only for steps 1/2; other steps reuse its previous text. Like the
source, another SetText does not cancel earlier coroutines, and RevealAll stops
only the latest handle without invoking its callback. Explicit Cancel is for
recovered lifetime teardown and stops all remaining work.

BuildGuideText extracts original text Rect 224632457200066912 from the Guide
prefab, keeps its original TMP font, material, layout and styling, and replaces
only the unavailable GuideTxtAnim script. Runtime literals and 100 chars/second
are authored Inspector fields. GuideText.prefab is prepared for nesting into the
full guide; it is not yet connected to the main guide mask or target reparenting.
No claim of a completed first-Spin guide follows from this standalone component.

RecoveredGuideTextTests exercises the actual authored component: initial character
while paused, scaled-time suspension, at most one character per slow frame,
completion delayed beyond the final character, background reveal suppressing
completion, unknown-step text reuse and explicit cancellation.
Focused PlayMode validation: guide-text.xml passes 1/1. The full 371-test result
above predates this component; no new full-suite claim is made for this change.

## Production first-Spin overlay

BuildFirstSpinGuide now imports the whole original Guide prefab, retaining Rects,
the root black Image, Content/Kuang/Image, source sprites zy_s9g01/zy_s9g02 and
the original standard background Button. The native dragon component is replaced
with the existing recovered ef_long idle rig at the source position (210,-180),
scale .6, size (990,1245.0002), pivot (.47777772,.3566265), layer 0. GuideText is
nested at its source parent. All runtime click events are bound in code.

Correction to the earlier mask description: the `black` child's GUID e0b4d57...
maps to Assembly-CSharp/EmptyRaycastGraphic, not Image. Its serialized .5019608
color does not draw another black quad. RecoveredEmptyRaycastGraphic retains the
standard Graphic raycast participation and clears the mesh, matching native
OnPopulateMesh 236d448; native Rebuild 236d440 tail-calls base Graphic.Rebuild.
The single visible dim layer is the root Image. Importing this component is
required for the background Button to receive hits and reveal the text.

RecoveredFirstSpinGuide.Show converts the target world pivot through the main
UI camera into main Rect local coordinates. ARM 2371fa0..2372020 gives
y = targetLocalY + NodeHeight/2 + targetSizeDeltaY/2; if y > integer Screen.height/2,
y = targetLocalY - NodeHeight/2 - targetSizeDeltaY/2. Node x is zero. It then
records the Spin Button's parent, reparents the actual Button with world position
retained and starts step-1 text. Hide restores its parent (source does not restore
sibling index), cancels the text for teardown and deactivates the pooled view.

CoreRoundFlow now shows this overlay for GuideStep 1 before showing the immediate
hand, subscribes the existing accepted-Spin GuideHideRequested event and restores
the button before GM destroys the old playfield. The overlay inherits the main
Canvas; no invented guide sorting tier is added. This change is the first-Spin
branch only. The second-step extra-Wild guide and CheckBaseEnd continuation are
still incomplete and must not be inferred from this integration.

Focused integration first-spin-guide-viewport.xml passes 1/1. It loads the actual
GameEntry, verifies the original Button is under Guide, uses EventSystem raycasts
to hit Spin and block MoreSpinBtn, executes background/Spin pointer handlers,
checks step/count/busy state and world-position preservation, and rebuilds via GM
while the Button is lifted. The latest screenshot current-first-spin-guide.png
was inspected for the source dimming, smaller dragon, speech box and lit Spin.

Fixture corrections: the first imported background omitted EmptyRaycastGraphic,
which the test exposed and the authoring now preserves. The initial fixture also
changed camera.targetTexture after Guide had recorded target world coordinates;
that resized the Canvas while the button was already reparented. The capture
viewport is now established in sceneLoaded, before the actual async game load
shows Guide. The fresh GM hierarchy is rendered before its raycast checks, so
new CanvasRenderer depths are valid. No runtime coordinate compensation or
Editor-only initialization path was added for those test conditions.

Final event-binding audit corrects an earlier inference: Guide.OnClickBgMask is
implemented in the binary, but Guide has no Awake/Start registration; Init,
SetGuideInfo and constructor 2372744 bind no Button listener, and the prefab's
Bg.m_OnClick list is empty. No native caller was found for OnClickBgMask 23726d4
or StopTxtAnim 23726e8 in the recovered bodies. Therefore the production background
Button only blocks input; it does **not** reveal the text. The initially added
RevealAll listener was removed. The text method remains recovered and tested as
a method; it is not artificially wired into this source prefab. The final guide
fixture clicks the paused background and expects the first character unchanged,
then lets the real coroutine reach the complete text before capturing.

Full-suite fixture updates explicitly isolate unrelated tests from first-time
guidance: CoinGlow and the standalone MoreSpinWindow fixture hide the guide before
their own pixel/mask tests; CollectEntry sets and saves GuideStep 3 before testing
the post-onboarding Treasure entry and GM profiles. The real first-player fixture
continues to start with empty PlayerPrefs and validates the actual blocked input.

Final full validation: first-spin-guide-fixtures.xml passes 373/373. The fresh
current-first-spin-guide.png was inspected after natural text progression, showing
the lit Spin and active hand in their bottom-right position. The first full run's
later failures did not recur after correcting the primary overlay-dependent
fixtures; no RNG, reward arithmetic, animation duration or floating-point checks
were relaxed. The final production background has no reveal listener.
