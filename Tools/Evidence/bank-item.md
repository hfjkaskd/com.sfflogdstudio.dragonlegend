# Bank item reveal and flight continuation

This increment adds an authored `RecoveredUI/BankItem` prefab and its animation controller. The Bank selection model and this item are not yet connected to a production Bank window. Popup continuation, floating items and the core end-flow wait remain pending. SDK behavior is unchanged.

## Native evidence

- BankItem.InitUI 2390650 stores the type, plays a looping idle clip, and hides Reward and AdImg. GetBallAnim 239070c formats `{0}_{1}`: type 0 uses zi, type 1 lan, every other value lv. Relocation-backed GOT strings: 4f1cf58 idle, 4f1cf60 format, 4f1cf68 zi, 4f1cf70 lan, 4f1cf78 lv.
- PlayAnim wrapper 23907ec / MoveNext 2390a3c plays the matching huo clip once, plays coinReveal, then waits 0.5 scaled seconds in Update timing 8. It does not wait for the two-second fire clip to finish.
- It formats the captured amount with two decimal places, activates Reward at zero scale and animates scale to 0.6000000238418579 over 0.5 seconds using Ease 4 (InQuad). The float is stored at ELF address 0xdbc450.
- After another 0.5-second scaled wait it dispatches FlyCoin event string `1` with amount, callback and Reward transform. GOT 4f1cfa8 resolves coinReveal, 4f1cfb0 huo, 4f1cfb8 `1`.
- The async item method finishes at dispatch. Callback 2390a1c invokes the original Action<float> with the captured amount only when the flight owner calls it. The item itself neither credits money nor completes the selection at reveal time.

## Authored structure

`BuildBankItem.Save` imports the first actual BankItem subtree (RectTransform 224499726237547504) from `ReferenceOriginal/Res/ViewPrefabs/UIBankView.prefab`, retaining original layout, images, text material and the standard Button on the root. Its child named btn is an Image, not the Button. Original serialized click events remain empty; the eventual window will bind listeners in code.

The Longzhu and reward glow children use the existing official Unity rig/animation authoring path in place of Spine components. All nine original longzhu clips come from `Tools/Evidence/FreeSymbols/ef_longzhu.json`; they contain region attachments only. Idle mapping in the resulting pose list is [5,3,4], fire mapping [2,0,1]. Static timing, scale and easing are serialized on the prefab rather than assigned during gameplay.

The runtime emits sound and flight requests for the eventual window owner. Cancellation stops pending waits and rejects a flight continuation belonging to a discarded GM owner. The original FloatBankItem movement is deliberately still pending: PlayAnim 23908d0 uses original Y plus height, Linear easing and infinite Yoyo loops; StopAnim 23909a0 kills its latest tween and restores the original local position when a handle exists.

## Verification

`RecoveredBankItemTests` instantiates the actual authored prefab and checks all three clip mappings, hidden initial reward/ad state, standard root Button with no serialized click handlers, scaled-time pauses before reveal and before flight, formatting, final 0.6 scale, flight source and amount, callback only on arrival, default type mapping and cancellation before reveal/after dispatch.

Unity 2022.3.62f3 full PlayMode regression: `Artifacts/bank-item-regression.xml`, **384/384 passed**; process 7796 exited. This includes the prior production core flow and four Bank selection tests. Visual comparison of the integrated Bank popup remains pending; this test does not claim screenshot equivalence.
