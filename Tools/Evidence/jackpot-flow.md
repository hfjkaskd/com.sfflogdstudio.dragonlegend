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

Existing real Spin reward flow still ends at JackpotCheckRequested after Wild. The icon win
API is implemented and tested, but its triggering jackpot event, popup, first-free state,
claim-button/SDK boundary, fly-coin presentation and continuation remain to be connected.
No placeholder reward or automatic popup completion has been added. The current screenshot
and source pose comparisons cover these meters, not the entire game's visual fidelity.
