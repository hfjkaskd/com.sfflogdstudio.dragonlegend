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
