# Bank item floating motion

`RecoveredBankFloat` is now attached to the authored `RecoveredUI/BankItem.prefab`. The popup owner will call Play with the original randomly drawn height and duration; production Bank window integration is still pending. No SDK changes.

## Authoritative behavior

- FloatBankItem.Start 2391498 captures transform.localPosition. PlayAnim 23908d0 targets originalPosition.y + height, uses the supplied duration, Linear ease (1), infinite loops (-1), and Yoyo (1). ARM 2390934..2390948 computes the target; 2390950 selects Linear; 2390970..2390988 configures infinite Yoyo.
- The newly created tween replaces the stored handle at +20 without killing an older one. StopAnim 23909a0 kills only the latest handle, clears it, and restores the entire original localPosition. A null handle returns without changing position (23909b4). Retaining an earlier overwritten motion is intentional native behavior.
- The official Unity Update runner drives the motion with scaled delta time, including while its object is inactive, matching an independently running tween. It changes only Y while moving, preserves live X/Z, and removes work after the component is destroyed. No third-party tween assembly is introduced.
- UIBankView.OnAfterShow 2391c3c first calls RandFinger 2391c60, then FloatBankItem 2391d34. RandFinger consumes one integer Random.Range(0, BankItems.Count). Floating then consumes six float draws in item order 0,1,2: height Random.Range(20f,30f), duration Random.Range(1f,2f) for each. This ordering must be retained when wiring the popup; the motion component itself consumes no RNG.
- OnAfterHide 2392ab0 hides the finger, invokes the stored close callback, then stops each item's floating motion. The owner must preserve callback-before-reset order.

## Verification

The PlayMode test instantiates the actual authored BankItem. It checks Start position capture, null-handle Stop, paused scaled time, linear outward and return legs, inactive-object updates, preservation of X/Z during motion, whole-position restoration, and the native overwritten-handle behavior.

Unity 2022.3.62f3 focused PlayMode result: `Artifacts/bank-float.xml`, **1/1 passed**, process 31592 exited. The authoring process 31448 also exited successfully. Full regression was not repeated for this isolated, not-yet-window-driven component; previous full run remains 384/384 in `bank-item-regression.xml`. A full Bank popup screenshot and core end-flow integration remain pending.

## Next popup authoring references

Source `UIBankView.prefab` contains Content/Grand, Major, Minior; their Spine children reference the small jackpot icon rigs. Content also contains three BankItem roots, Btn/UnPlayBtn and Btn/OpenBtn. New sprite mappings needed beyond BankItem: 1d47a370dfffde042a3a22d6d2b3d893 -> bank/hou_yun, 21699d62cfe5c324fafeb964d40995c5 -> bank/b_luckybonus_txt, deeb1147e3346884d8b26004fefaef1d -> bank/qian_yun. Standard legacy Text script GUID is 04f84fc2003509a5e7e068ec1271cc40. Original button animation GUID b02dda94bf0cfd148b568608d58490f9 maps Res/Anim/btnanim.anim. These are source references, not proof of completed popup authoring.
