# Collapsible local version controls

Current captures showed the two 600x86 local version buttons, each on a sorting
5000 Canvas, covering the center of actual reward and minigame windows. These
are reconstruction test controls, not original game UI.

GameEntry.prefab now authors a 96x64 GM Button at bottom-left (16,16). The two
existing selectors retain their GameEntry bindings but are grouped with alpha
zero, interaction disabled and raycast blocking disabled by default. Clicking
GM opens them; clicking Close or choosing a profile collapses them. The toggle
does not restart the game or mutate player state. Default US cash/ads selection
and the alternative profile remain unchanged. This is a local GM affordance,
not an assertion about unknown original country/server routing.

RecoveredGmPanel only binds standard Button callbacks and updates CanvasGroups.
No static layout is built at runtime and no Editor-only gameplay path is added.
The shared authoring helper that copies version-button styling for existing ad
simulation buttons removes the new inherited CanvasGroup, so regeneration does
not accidentally hide those controls. Runtime SDK/ad behavior is unchanged.

The integration test uses actual GraphicRaycaster/PointerClick events to open,
close and select both profiles. Hidden selectors produce no hits, toggling
retains the same playfield/progress/spin count, and actual selection rebuilds
the profile then closes the panel. Existing jackpot interaction tests explicitly
open GM before checking selector reachability and close it before capture.

Latest Slot and reward captures confirm removal of the central purple overlays.
The compact GM entry remains intentionally visible for local testing. Other
original visual differences, title shine, country routing coverage and unfinished
main/free lifecycle branches are still pending.

Validation: Artifacts/gm-panel-final-tests.xml reports 313/313 PlayMode tests passed on Unity 2022.3.62f3 with graphics enabled. Fresh current-lucky-spin-window.png was inspected after the final prefab regeneration; only the compact GM entry remains and the central Slot artwork is unobstructed.
