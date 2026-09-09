# Native TreasureCard presentation

## Source and recovered behavior

Current source is `ReferenceOriginal/Res/ViewPrefabs/UITreasureView.prefab`, subtree RectTransform 224841364053301648. BuildTreasureCard copies its complete hierarchy, child order, transform values, Image/TMP/Text settings, legacy button animation and standard Button references. The root is 100 x 100; both faces are 390 x 459 at (0,302). Source names `CardFont` and `CardBlack` are intentionally retained.

Original runtime bodies are under `C:/Projects/Nut Sort Relax/reconstruction/mumu-current/native/game/`:

- `023db8c4.asm` Init ignores collectIndex, resets the flipped flag, shows the back, hides the front, resets XYZ scale to one. It does not modify reward or cancel sequences.
- `023db974.asm` Flip returns immediately if already flipped; otherwise sets the flag before sound and animation. Sound relocation 04f1eb80 resolves to the exact original string `cardReavel`. Sequence closes ScaleX to zero, swaps faces, opens ScaleX to one. Source prefab duration is .25 seconds per half, closeEase=5 (InQuad), openEase=27 (OutBack).
- `023dbc6c.asm` midpoint changes only face visibility. `023dbcb4.asm` completes by invoking OnFlipComplete(card) before the supplied completion callback.
- `023dbbe8.asm` SetFlipped changes the flag and faces only; it leaves scale and running animation alone.
- `023dbc34.asm` destroys transform tweens without completing them. The replacement stops when the Unity object is destroyed. A hidden card continues animating through the native Unity Update service, matching global tween behavior.

Only Unity AnimationCurve, Transform, MonoBehaviour and standard UI components are used. The two-key opening curve represents the OutBack cubic with endpoint derivatives 4.70158 and zero; closing uses the InQuad endpoint derivatives. Per-frame iteration reuses the service list; a sequence enumerator is allocated once when a flip begins. There is no reflection, third-party assembly or runtime static UI construction.

Original EmptyRaycastGraphic on UnPlayBtn is mapped to a transparent Unity Image target on the existing Button object, retaining its visible child text. Button handlers belong to the containing Treasure window and are not replaced with an invented card click target. Existing persistent button callbacks are verified empty during authoring.

The extracted t_icon_02 texture was not a Sprite and could not be assigned to Image. Its importer now exposes its original 222 x 219 sprite, PPU=100, without resizing or compression. Font, reward sprite font, button sprite asset and button animation reuse existing recovered native assets.

## Verification

RecoveredTreasureCardTests passes both focused PlayMode tests. It checks native hierarchy references and sprite dimensions, 32 actual animation frames against independent InQuad/OutBack formulas, midpoint visibility, time-scale pause, duplicate Flip suppression, exact sound/event/callback order, and unchanged Y/Z. A second test checks Init during playback, playback while disabled, and no completion callback after destruction.

The first regression passed 325/325 but its opening-curve expectation was wrong. Cross-checking the original `analysis/dump.cs` Ease enum (TypeDefIndex 11651, lines 817742 onward) proves 27 is OutBack and 30 is OutBounce. Authoring and the independent expected polynomial are now corrected to OutBack; the test explicitly requires scale to exceed 1.09 during opening, which rejects the prior Bounce curve. The earlier green result did not prove that part of fidelity.

The corrected curve passed the full **327/327** PlayMode run in `Artifacts/collect-tip-tests.xml`.

Fresh 1080 x 1920 renders from the current prefab: `Artifacts/current-treasure-card-back.png` and `Artifacts/current-treasure-card-front.png`. Both renders were visually inspected. These isolate the card subtree; the displayed $50.00/CLAIMx2 are original serialized authoring values. The missing Treasure window must replace them with live card/reward data; these captures do not prove window integration or final payout correctness.

## Remaining lifecycle work

UITreasureView, main-card flight and its RefreshCollectCard event, claim/ad branches, actual payout and production Free-loop binding remain pending. The live collection-tip component and prefab are now recovered separately (see `collect-tip.md`); the window still must invoke it on flip completion. This prefab is the native card component required by that window, not a finished Treasure mini-game.
