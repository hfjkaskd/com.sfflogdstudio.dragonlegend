# HUD layout verification

Validated in the existing Unity 2022.3.62f3 editor on 2026-09-16.

The main header and its utility buttons now inherit the same Playfield safe area. GM moves with the utility group and returns to the persistent Canvas before the group is destroyed. The periodic withdrawal tip has a dedicated second-row slot between the level progress and PayPal, with non-interactive graphics. The balance text stays in the first row. TASK now uses CashOutEntry's configured side-entry container, with spacing for the wallet's maximum pulse and a standard Button rectangle that contains its icon and label. Prefabs and corresponding authoring scripts were updated together.

## Actual checks

- 16 isolated renders of production prefabs: ScreenSpaceCamera / WorldSpace, 1080 x 1920 / 2160 / 2400 / 2640, with both full screen and top 120 / bottom 60 safe insets.
- 480 geometry and text checks passed, including tip/PayPal/balance/button separation, safe-area alignment, complete text, maximum wallet pulse spacing, TASK click bounds, and the actual GM Canvas-to-Utility reparenting path.
- Reviewed the rendered 1080 x 2400 safe-area camera image and 1080 x 1920 full-screen world image.
- Production runtime and editor scripts compiled in Unity. Additional standalone compilation used the project's existing Unity compiler references.
- Five modified prefab local file-ID graphs checked for duplicates and missing references. Incremental patch whitespace check passed; repository-wide warnings include pre-existing Unity-generated empty scalar spaces.

The fixture deliberately omits gameplay/SDK initialization. It does not modify PlayerPrefs, gameplay probability, money, task state, open scenes or original prefabs. No APK was rebuilt or installed for this change.

## Evidence and rerun

- `Tools/Evidence/hud-layout-validation.json` contains all checks and installed source hashes.
- `Artifacts/HudLayout/` contains the original report and 16 PNGs.
- `Tools/Validation/HudLayout/hud-layout.patch` records only this layout change relative to the pre-task working files, independent of unrelated repository changes.
- To rerun, temporarily copy `ValidateHudLayout.cs` into `Assets/Whitebox/Editor/`, let Unity compile, then use **Tools > Validation > HUD Layout** in Edit mode. The helper creates and disposes isolated preview scenes and restores the Unity random state. Remove the temporary copy afterward.
