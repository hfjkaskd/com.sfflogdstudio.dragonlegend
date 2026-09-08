# Dragon-ball activation and original-ball reward presentation

LongzhuItem.PlayAnim MoveNext 0x23af314 resolves GetBallAnim(current type, huo),
plays once without an animation completion callback, waits .7 scaled seconds,
then invokes the supplied callback with the CURRENT stored BallType. It does
not await the two-second huo clip or switch back to idle at .7 seconds.
ELF RELA pointer 0x4f1cfb0 -> ScriptString 0x5060b70 resolves to huo. Float at
ELF 0xdbc6e4 is .699999988079071. FreeBall now authors huo_zi/huo_lan/huo_lv
and activationDelay .7, and PlayActivation uses the shared scaled wait runner.

PlayRewardAnim MoveNext 0x23af730 kills only the stored numeric tween, waits
.1 seconds (ELF 0xdbc650), then creates DOTween.To from a getter that always
returns zero (0x23af104) to the requested float reward over .3 seconds
(0xdbc664). Its setter 0x23aef68 formats the value with two decimal places
using the current language. Creating that tween completes the async method;
it does not await the number or scale animations. The numeric completion
0x23aefa0 scales the existing reward Text to 1.2 over .2 seconds; callback
0x23af06c starts a separate .2s return to 1. These use default OutQuad easing.
There is no SetActive on the Text and no reinitialization of its font/style.

RecoveredFreeBallReward is authored on FreeBall with the existing label and
those timing/scale values. Numeric jobs start from zero; scale jobs capture
the actual current scale on their first advancing update. Completion-created
jobs start next update, preserving the native tween-manager snapshot behavior.
The latest numeric job is retained separately so replay cancels only it.
Pending waits still start independently, and existing scale jobs survive.
Runtime language is supplied as a provider and read on every numeric update.

Tests cover all three huo clips, pause, callback at .7 while the clip is still
playing, changing the stored type during the wait, .1s reward scheduling,
number/scale completion, hidden label remaining hidden, live language changes,
two pending reward calls and a later replacement numeric tween.

Validation: Unity 2022.3.62f3 full PlayMode suite 300/300 passed in
Artifacts/free-ball-reward-tests.xml. Prefab authoring completed successfully
in Artifacts/free-ball-reward-author.log.

Pending: full row-by-row dragon-ball orchestration, NPC arrival reparent/state,
branch reward windows, original-ball reward invocation plus ledger writes,
then collection/Free-end continuation and production FreeEntry binding.
Inactive object tween behavior still depends on MonoBehaviour Update and
needs the previously recorded broader lifecycle alignment. SDK unchanged.
