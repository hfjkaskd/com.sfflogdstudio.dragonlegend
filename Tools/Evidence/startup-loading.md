# Startup Loading

- Artwork: Assets/Resources/Loading/GildedDragonLoading.png, verified PNG dimensions 1080 x 1920.
- Prefab: Assets/Resources/Loading/StartupLoading.prefab, screen-space overlay, sorting order 32000. GameEntry prefab references it; the GameEntry scene uses that prefab.
- StartupLoadingView loads the background through Resources.LoadAsync. Closing the view releases the texture.
- GameEntry displays Connecting, Loading configuration and Preparing game stages. The bar represents these stages, not measured download percentage. Completion closes the view after a rendered Ready frame. SDK, configuration and game construction failures leave an error visible.
- Editor and Android script compilation passed. Unity Play Mode visually verified: loading artwork shown during startup, then automatically removed when the real game appeared at 1080x2400. No Editor bypass added. No APK/device validation performed.
- Authoring tool: Assets/Whitebox/Editor/BuildStartupLoading.cs; menu Dragon Legend / Build / Startup Loading. Static UI is authored into prefab assets, not constructed in runtime code.

## Image generation
Built-in image_gen editing used; final generated image exported with high-quality bicubic resampling to exact 1080x1920. Source user attachment was the edit target.

Prompt:
Edit target: attached game artwork. Resize/adapt this exact artwork to 1080 x 1920 pixels portrait for a Unity loading background. Preserve the existing artwork, composition, golden dragon, treasure, red/gold palette and exact title 'Gilded Dragon Odyssey'. Do not redesign, add text, add a loading bar, crop the title or change any characters. Only adjust image dimensions/framing minimally to fit 9:16. Save the resulting image.

## Loading bar style revision
Removed the full-width translucent footer Image and its CanvasRenderer. The gold stage bar now has a 28.8-unit track and bold text directly over the artwork. Runtime loading logic is unchanged; the prefab authoring tool matches the saved prefab.

Progress bar edge fix: replaced the padded built-in fill sprite with a plain Simple Image. SetStage now changes the horizontal anchor, so 100% covers the entire track. Updated both the saved prefab and authoring builder; verified zero offsets and sprite-free fill configuration. Live/device rendering not revalidated for this change.
