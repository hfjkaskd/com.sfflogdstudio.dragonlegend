# Jackpot reward and continuation evidence

Source: `C:/Projects/Nut Sort Relax/reconstruction/mumu-current`, Dragon Legend ARM64 ELF.
Addresses below are ELF RVAs, not the Ghidra addresses (which add `0x100000`).

## Runtime reward source

- `ConfigManager.GetJackPot` (`236ad18`) returns `Qonrii.Joqkpor` (offset `78`).
- `GetJpAdd` (`236af28`) reads `Qonrii.JpOpp[0]` (offset `b8`).
- `GetJpClaim` (`236ad3c`) reads `Qonrii.JpQloim[index]` (offset `80`) and divides by float 1000.
- `JackPot.Init` (`2392874`) stores the supplied multiplier and zero-based meter index,
  starts the idle animation, disables the TMP reward object, and refreshes both text values.
- `RefreshRewardValue` (`23982b8`) and `PlayRewardAnim` (`2398660`) calculate
  `multiplier * Bet + (float)(JpAddCount * GetJpAdd())`.
- Direct ELF disassembly at `23983ac`: `scvtf s0,w21; mul w8,w0,w20; ...;
  scvtf s1,w8; fmul s0,s8,s0; fadd s8,s0,s1`. The integer product wraps at 32 bits;
  converting operands to float before multiplication changes the original behavior.
- `SetJackpotReward` (`2398538`) writes meter index 0/1/2 to runtime GameData fields
  `GrandJackPotReward` (`4c`), `MajorJackPotReward` (`50`), `MiniJackPotReward` (`54`).
  Other indices do nothing. These are not PlayerData fields and do not credit GreenCount.
- `PlayRewardAnim` writes the target reward immediately, then kills the previous tween
  without completion and starts a 0.3-second number tween. This is separate from the
  runtime reward used by CheckJackPot.
- Direct ELF `23988c0` getter reads `curJackPotWin` at `3c`. Setter `23988c8..23988f4`
  ONLY formats the supplied float and writes GreenRewardTxt. It does not update `3c`.
  Do not silently change subsequent tween start values to the last rendered number.

## CheckJackPot (`23caa64`)

1. Fewer than three complete Wild columns: return completed task immediately.
2. Otherwise `GameData.SetTaskData(2,1)` occurs first, including its save.
3. Select enum Minor=3 for 3 columns, Major=2 for 4, Grand=1 otherwise.
4. Snapshot the corresponding runtime reward after the task update.
5. Dispatch event key `3` with the enum; pause music; play Sound1 `dragon3`; wait 1.5 scaled seconds.
6. Stop Sound1; show UIJackpotView with `[enum, captured float, Action<float>]`.
7. WaitUntil the callback sets `isPop=true`. The float argument is ignored by this
   callback (`23c253c`); the predicate (`23c2548`) only reads this flag.

## Popup and fly-coin boundary

- `UIJackpotView.OnBeforeShow` (`23b2b2c`): reset click latch, capture enum/reward/callback,
  retain original amount in curCount, play the enum-named loop and format reward.
  Normal advertised multiplier is GetJpClaim(0); first-free-reward forces 1 instead.
  Unadvertised multiplier is always GetJpClaim(1); its text object is hidden for first-free.
  Sound1 is `jackpotBg`, and the additional PlaySound is `jackpotm`.
- `ClickPlayAD` (`23b3658`) and `ClickUnPlayAD` (`23b366c`) multiply winCount by their
  respective multiplier. SDK implementation remains excluded from reconstruction work.
- `OnAfterHide` (`23b3680`): stop Sound1, resume music, dispatch fly-coin event carrying
  `[winCount, Action]` with key `1`. Closing the popup does not itself resume CheckJackPot.
- Fly-coin completion invokes `OnAfterHide.b__17_0` (`23b38a0`), which invokes the saved
  popup callback with winCount. Only then can CheckJackPot's wait complete.
- Existing PlayFlyCoin completion (`23c306c`) runs the caller callback, resets TopTitle,
  then reads the latest balance and adds the amount. Preserve that ordering.

Strings and generic popup method were resolved from ELF `.rela.dyn` RELATIVE relocations
into `analysis/script.json`, not inferred from English names:
`4f1e710 -> dragon3`, `4f1de68 -> jackpotBg`, `4f1de60 -> jackpotm`,
`4f1d220 -> 3`, `4f1cfb8 -> 1`, `4f1d240 -> UIManager.ShowWindow<UIJackpotView>`,
`4f1d180 -> ClaimBtn`, `4f1d040 -> UnPlayBtn`.

## Implementation status

Implemented: configuration accessors, integer arithmetic, runtime reward fields, reward
snapshot, and all three prefab jackpot meters in the actual main view. Spin's existing
JackpotAnimationsRequested now immediately stores all three rewards and starts the native
0.3-second number presentation. Both original text objects, their coordinates, Green font,
and the disabled TMP object are represented in the prefab.

`InitJackPot` (`23bb2cc`) enumerates all configured multipliers; index 0/1 selects Grand/Major
and subsequent indices select Mini (the stored meter index is still the enumerated index).
`OnClickButton` (`23bd4e8`) starts Grand, Major, Mini number tweens in that order.

Original icon resources: `Res/Spine/jackpot3小个/{grand,major,minor}_icon/ef_*icon`.
Each has 22 bones, 31 slots, 30 attachments, and two 2-second animations (`idle`, `win`).
Thirteen bones use OnlyTranslation. The slot-5 `grbao/grbao` sequence has 20 atlas frames,
start 0, digits 0, setup index 0; win uses Once mode with a 0.05-second frame delay.
All three files are fully parsed (12555/12559/12183 bytes). The converter now reads region
and mesh sequence records and attachment sequence timelines without skipping their bytes.

Native Unity representation: one CanvasRenderer per icon, immutable shared animation
curves, prefab geometry, cached instance pose arrays, atlas loaded by Resources path.
PMA color/additive behavior and rotated/trimmed sequence-frame UVs are retained.
`PlaySpineAnim` (`238bc24`) forces Initialize(true), clears tracks, then starts the new
animation, so there is no defaultMix crossfade to reproduce here. Each icon samples from
its cached setup pose, including when switching between win and idle.
`PlayAnim.b__0` (`239940c`) returns to idle, invokes caller, then refreshes the reward from
the latest state. The prefab meter preserves this order and cancels callbacks on release.

Format references: [SkeletonBinary.cs](https://raw.githubusercontent.com/EsotericSoftware/spine-runtimes/4.1/spine-csharp/src/SkeletonBinary.cs),
[Sequence.cs](https://raw.githubusercontent.com/EsotericSoftware/spine-runtimes/4.1/spine-csharp/src/Attachments/Sequence.cs),
[Animation.cs](https://raw.githubusercontent.com/EsotericSoftware/spine-runtimes/4.1/spine-csharp/src/Animation.cs).
These sources inform offline decoding and native math; no Spine runtime assembly is added.

The real Spin reward flow now runs CheckJackPot after Wild, starts the selected icon win,
and shows the authored popup with its captured amount. First-free state and the existing
local ad facade are connected. The actual flow still waits for the unimplemented fly-coin
presentation; see [main-flow integration](JackpotPopup/integration.md).
No placeholder reward or automatic popup completion has been added. The current screenshot
and source pose comparisons cover these meters, not the entire game's visual fidelity.

## Claim lifecycle recovered after popup artwork

`RecoveredJackpotClaim` now implements the native click/reward/window/fly-coin boundary,
using the existing `IAdFacade` mock. It is a presentation-independent controller.
The authored `RecoveredUI/JackpotPopup` now supplies the actual count and exit animations,
standard buttons, native artwork, original text and dynamic CashOutTip; see
[window evidence](JackpotPopup/window.md). Main-flow entry and the first modal mask are
connected; the complete window stack and fly-coin presentation remain outstanding.
`IRecoveredJackpotClaimView` requires real count/exit/flight completion from the presenter;
it does not synthesize completion, credit, or a delay. Count must ultimately use the
original authored 0.5-second OutQuad presentation and exit the 0.3-second InBack scale.

Direct ARM64 verification of `23b32a8` resolves an ambiguity in its expanded pseudocode:

```text
23b34b4 ldrb w8, [x0, #0x5a]  ; reread current GameData.isFirstFreeReward
23b34b8 cmp w8, #1
23b34bc b.ne #0x23b359c
23b34c0 ldr s0, [x19, #0xa4]  ; winCount
23b34c4 ldr s1, [x19, #0xac]  ; multiplier captured on show
...
23b3634 ldr x3, [x8]           ; PTR 4f1de90 = jackpot
23b3640 mov x4, x3             ; posId AND sceneId are jackpot
23b3648 b #0x2379600            ; SdkAdManager.PlayRewardAd
```

ELF RELATIVE relocation -> script.json string: `4f1cf38 = click`,
`4f1c4d8 = iv_`, `4f1c4e0 = rv_`, `4f391e0 = reward`.
Those internal SDK prefixes are not the caller's posId. The mock receives the actual
caller parameters `jackpot/jackpot`; UnPlayBtn receives `iv_close/jackpot`.

Native success `23b3cb0` multiplies winCount and invokes finishCall. Failure `23b3cec`
only clears the click latch. `23b38c0` bypasses counting for equal amounts, otherwise
requests count sound and the tween. `23b3bac` hides a shown wheel before the jackpot.
`23b3680` stops Sound1, resumes music, then dispatches the fly-coin event. `23b38a0`
invokes the saved callback with winCount only on flight completion.

The original curCount remains unchanged by its text setter. First-free status is captured
for the show-time multiplier/visibility but read again on click. The controller neither
consumes this runtime flag nor persists it. Unknown names lock clicks as in the original;
BeforeShow resets the latch. No unrequested synthetic close behavior is introduced.

Tests cover all three mock failure outcomes and retry, repeated clicks while pending,
first-free transitions between show and click, equal-reward count bypass, wheel-before-
popup hide, and flight callback -> title reset -> actual two-save balance credit ordering.
These tests validate the claim controller, not the still-unassembled popup layout or the
remaining jackpot-to-symbol main-flow continuation.
