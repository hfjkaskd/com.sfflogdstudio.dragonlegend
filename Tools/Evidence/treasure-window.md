# UITreasureView native window

## Presentation and data path

RecoveredTreasureWindow and BuildTreasureWindow restore current `ReferenceOriginal/Res/ViewPrefabs/UITreasureView.prefab`. The original world-space Canvas belongs under the main UI hierarchy, with its full-stretch root and native child transforms. Window type/mask constants from ELF dbc1f0 are (300,1): popup with a transparent normal input mask. Original `bLACK` supplies its own .6509804 alpha only after the card arrives.

Existing TreasureCard and CollectTip prefabs are nested, retaining their source transforms. Light uses the recovered ef_shoucanggl animation at scale 1.2 and (0,306); the source's misleadingly named `SkeletonGraphic (ef_caidai)` actually references ef_xjpl, idle loop. The recovered ef_xjpl is used, not a different confetti asset. Title keeps its 875 x 236 size at (0,672), source sprite and native shine parameters (factor .31216258, width .25, rotation 135, two-second loop).

All fifteen original t_icon sprites are imported without resizing and referenced through serialized resource paths, loaded on selection. Config info is selected by matching ID, the native value is formatted at precision 2, claim labels use configured multipliers, and Image.SetNativeSize applies the selected sprite's actual dimensions.

Native order from 23dc0a8/23dc8b8/23dbe7c/23dc02c:

1. Play jump, reset Content to scale one, reset card faces; hide tip, title, light and confetti.
2. Read current CollectInfo and claim settings; update texts and sprite; dispatch event 7 with integer arguments (4,1); hide card and bLACK.
3. EnterAnimation completes immediately. Reset EndPos.localPosition to zero and invoke the supplied card-flight callback.
4. On flight-arrival event, reset the incoming object to local zero/scale (.4,.4,.4), hide it, activate the actual card and bLACK, then Flip.
5. On flip completion, show title/light/confetti and initialize the live collection tip, which owns its native delayed reveal and A-profile hiding.

Claim behavior is documented in `treasure-claim.md`. The window implements the deferred original-worth getter, .5-second OutQuad reward count, and .3-second InBack exit with scale reset. After exit it invokes the actual shared RecoveredCashFlightPresenter, then publishes the card departure with current EndPos.position and CardImage.sprite. Arrival releases the caller before the existing cash presenter credits the balance.

## Verification boundaries

RecoveredTreasureWindowTests loads actual GameEntry and configuration, displays all fifteen cards, exercises regular and rewarded claims (including failed-ad retry), checks mask/flip/title/light/tip sequencing, pauses scaled time, uses actual cash flight to the actual balance, and observes departure-before-arrival callbacks. A final window-only A-profile check verifies tip hiding and Brazilian currency formatting.

Initial logic verification passed, but the first screenshot exposed a test-fixture error: the world-space window was instantiated at scene root rather than under Main. That capture did not prove visual correctness. The corrected fixture parents the prefab under actual GameEntry, asserts the root Canvas and on-screen card position, and passed its focused test. Fresh `Artifacts/current-treasure-window.png` and `Artifacts/current-treasure-window-last-card.png` were both visually inspected; they show the title, live ID 1/15 cards and cash values, native effects, buttons and collection progress over the current main scene. The oversized main dragon/background remains a separate known visual gap.

Full runtime regression passed **334/334** (`Artifacts/treasure-window-tests.xml`); the subsequent fixture-only correction passed **1/1** (`Artifacts/treasure-window-visual.xml`). No runtime code changed after the full regression.

## Still pending

The real small-card entry flight and RefreshCollectCard arrival are now supplied by `RecoveredFreeTreasureGame`; see [free-treasure-game.md](free-treasure-game.md) for the native branch and actual two-ball integration. Card departure event 14 is connected to the original pooled image and animated target through [treasure-departure.md](treasure-departure.md). Main event 7, production Free ball routing, the collection Button action, full collection claims and Free-spin lifecycle remain pending. SDK facade behavior is unchanged.
