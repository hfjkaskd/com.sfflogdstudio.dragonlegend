# Free ball initial and start presentation

The current original Res/Prefabs/Longzhu.prefab has root scale .8, main
skeleton starting at huo_lan, and an active reward Text initially containing
$8.99. The Text uses the recovered Green bitmap font (GUID
36977c4faccb97c4ebe0b4fdeea88b25), font size 0, best-fit disabled, max size 40,
center alignment, a 160x30 rectangle and anchored position zero.

FreeBall.prefab preserves those authored initial settings with native world
animation geometry and a world-space Canvas for the label only. The shared
CoinRewardText resource supplies the original font but its Base-only runtime
behavior is removed from this instance.

RecoveredFreeBall.Initialize implements LongzhuItem.Init 0x23ae940: store the
supplied ball type, play its idle loop and clear the Text. It does not enable
or disable the label. The native isInit argument is unused. GetBallAnim
0x23ae9f4 maps type 0 to zi (purple), type 1 to lan (blue), and every other
value to lv (green), including values outside the ordinary 0..2 range.
The clip names are saved in the prefab rather than derived from enum strings.

PlayStart implements 0x23aeb64: start_<color> once; completion 0x23aeef8
starts idle_<color> looping using the current stored type. There is no coin
style scale pulse in this method. Reinitialization replaces an existing start
animation rather than letting its old completion override the new type.

Tests check the actual prefab font and label rectangle, initial and cleared
text states, both isInit values, all color branches including out-of-range
inputs, pause, start-to-idle transitions, unchanged scale and reinitialization.
The current preview Artifacts/current-free-ball-idle-start.png shows purple,
blue and green in native type order, idle above and start below, at .173s.

Pending: pooled CreateLongzhu and shared type-index ownership, mini-reel
clipping, collection/reward animations and the production Free gameplay loop.
This prefab does not claim those integrations are complete. SDKs are unchanged.

Validation: Unity 2022.3.62f3 full PlayMode suite **277/277 passed** in
Artifacts/free-ball-full-tests.xml. The fresh 1200x800 three-type idle/start
preview was inspected at original resolution.
