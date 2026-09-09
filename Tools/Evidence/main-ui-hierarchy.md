# Restore the Main UI scene parent relationship

Baseline b71152add43ddc4e796fd5928abaa26994f3e142 had the correct camera/environment
records but kept GameEntry's Canvas and EventSystem at scene root. Original
Main.unity places Canvas/23, UICamera/13 and EventSystem/11 beneath [UI]Main/12
in that order. EventSystem is layer 5, local position zero, identity rotation
and unit scale. The UI parent is at (100,0,-10).

BuildMainUiHierarchy.Save restores those parent/order relationships in the
authored GameEntry scene, preserving the existing Canvas prefab and camera
reference. EventSystem returns to source layer 5 and its original local pose.
This is scene authoring only; no runtime reparenting or Editor-only initialization
fallback is introduced. The world camera and transition remain separate.

The authoring process 29020 exited successfully. The scene audit now verifies
the complete UI ancestor child list, EventSystem GameObject/Transform and Canvas
prefab instance parent, alongside previous render/lighting/camera records.
All 14 record comparisons pass. The audit handles Unity's repositioning of the
root serializedVersion property without dropping its value, and treats numeric
negative zero as zero. It resolves the new stripped Canvas RectTransform and
uses explicit current/source local object-ID mappings.

RecoveredMainModeViewTests now checks the shared UI parent and exact child order,
EventSystem layer/local pose and the UI parent's world position before its
existing real-scene Base/Free rendering, safe-area and cash-target assertions.
Full PlayMode results and fresh rendering are recorded below when complete.

Full graphics-enabled PlayMode process 46892 exited: 463/463 passed,
102.1191656 seconds, Artifacts/main-ui-hierarchy-tests.xml. This includes the
combined BigWin/Bonus/Free cases and actual guide, popup, GM and cash-entry clicks.

Fresh render inspection exposed a pre-existing fixture setup problem: the mode
test changed camera aspect after FirstSpinGuide had cached the Spin world pose,
so guide restoration could leave Spin above its authored bottom position in the
test capture. Camera target assignment now occurs through sceneLoaded before
guide initialization, matching the existing core/guide fixture preparation.
No production layout compensation is added. Targeted render validation follows.

Targeted process 41716 exited: 1/1 passed, 2.2030066 seconds,
Artifacts/main-ui-hierarchy-render-tests.xml. Fresh current-main-base-return.png
was inspected after this corrected preparation: Spin is again in its authored
bottom region, and the board, top bar and other entry controls remain visible.
This fresh render and the tests validate the changed scene integration, not
complete original-game pixel equivalence or every requested lifecycle branch.
