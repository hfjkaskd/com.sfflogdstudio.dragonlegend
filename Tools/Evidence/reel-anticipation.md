# Base reel anticipation visual wiring

Native evidence:
- UIMainView.SpeedEffects at +0x70 contains five ef_slliejiasu SkeletonGraphic objects in ReferenceOriginal/Res/ViewPrefabs/UIMainView.prefab.
- InitReels 0x23bce8c hides all effects. Anticipation 0x23c05f4 activates the selected object before the accelerated wait; stop callback 0x23c03e4 hides it before stop presentation/shake.
- Source Roll rect: 948 x 515 at (-1.62,-71), under QiPan. Five column parents: 188 x 518, x=-380,-190,0,190,380, y=-1, centered anchors/pivots, RectMask2D. Effect rect: 267.50006 x 606, pivot (.49532714,.5), local anchored position zero, unit scale.
- Source startingAnimation is empty, startingLoop=true. The inspected native initialization and anticipation callbacks only toggle GameObject visibility. The production effect preserves the source setup pose; it does not invent playback of the animation contained in the skeleton.

Implementation:
- Offline extraction consumes all 21,114 skeleton bytes. ef_slliejiasu.json stores the recovered source rig/animation; prepare_reel_anticipation.py builds mesh authoring input and 188 independent source geometry samples.
- BuildReelAnticipation.Save authors five clipped decorative UI effects under the existing Base Roll. The original atlas loads by Resources path. Gameplay symbols remain existing world-space objects. No Spine/third-party runtime assembly was added.
- RecoveredMainModeView owns serialized effect references. CoreRound binds the actual Base controller visibility event; initialization and Unbind hide every effect. Existing controller timing/audio remain unchanged.
- The reusable converted art prefab retains its source animation for verification. Production instances contain only the setup-pose rig, with Animation and RecoveredRegionAnimator removed offline to match the empty source startingAnimation.

Validation:
- Artifacts/reel-anticipation.xml: 19/19 PlayMode tests passed. Includes actual main scene, real Spin/Free return, ordinary and first/middle/last anticipation controller timing, and source geometry comparison (including 188 anticipation frames). Geometry sampling reports zero allocations in the existing 1000-sample check.
- Actual CoreRound tests require a nonzero number of real anticipation events, assert each event immediately affects the authored object, and check all effects hidden after return.
- Main mode test verifies all five positions/pivots/masks and compares current scene renders with/without the middle effect (>200 changed pixels). Initial capture was made before configuring the render target; corrected the test to assign the target and wait one frame before rendering.

Scope: this addresses a verified missing visual consumer in the core Base spin. It does not establish whole-game 1:1 parity or a fresh APK screenshot comparison. SDK behavior is unchanged.

Follow-up: Artifacts/reel-anticipation-capture.xml passed 2/2 (main mode visual and existing Free GM teardown). Latest Artifacts/current-reel-anticipation.png inspected: the third-column setup highlight is visible and column-clipped in the 1080 x 1920 current scene; the initial guide overlay is also present. This is a controlled current-project display fixture, not a screenshot of an unmodified APK.
