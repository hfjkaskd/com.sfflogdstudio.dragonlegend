# Bonus reward popup presentation

BuildJackpotPopup.SaveReward imports the current original UIRewardView.prefab
serialization, retaining its RectTransforms, legacy/TMP typography, sprite
settings, button layout and cash-out tip. It binds RecoveredBonusRewardPopup to
the existing claim controller. Original ef_slpenqian and both ef_shoucanggl
nodes use the already converted native Animation/UI-mesh prefabs. The title
sprite is tc_luckyreward_txt (original GUID 48aec213d31978a4686437111f08efe4).

The ordinary claim Button now lives on the visible TMP label. Its raycast padding
is calculated during authoring from the original parent's rectangle, preserving
that hit area without the original separate EmptyRaycastGraphic. Button listeners
are code-bound. There are no retained third-party runtime components.

The presenter uses authored .3-second window scale and delayed reveals (plain
button 1 second, cash tip .5), and the native .5-second count. BeforeShow uses
sound jump, verified by resolving Ghidra pointer 0501d000 through ELF RELA and
script.json. It does not dispatch the Jackpot-only cash task event, change music,
or bypass reward ads for the first-free flag. Native 2 / .5 multipliers are stored
in the prefab. After the actual exit, the existing claim controller requests cash
flight and retains the caller until arrival.

Known visual gap: the original Title has UIShiny with effect factor .5, width .25,
rotation 135, softness/brightness/gloss 1, duration 2, looping with no delay and
updateMode 0. That effect still needs a native conversion; the current title only
retains its base image. This partial presentation must not be claimed as 1:1.
Full Bonus-window and shared-flight binding remains pending as well.

The PlayMode test exercises the actual prefab, buttons, delayed reveal, pause,
count -> exit -> flight callback, and captures the latest window. A green result
does not establish title shine fidelity or the full Bonus lifecycle.

Full PlayMode run `Artifacts/bonus-popup-tests.xml`: 241 passed, 0 failed.
Latest capture `Artifacts/current-bonus-reward-window.png` was inspected at native
1080x1920 resolution: title, cash art, green legacy amount, TMP ad icon/button,
ordinary claim text and cash-out tip are visible. This is an isolated current
window capture; title sweep and compositing over the live Bonus board are pending.
