# UIJackpotView native prefab and presentation

Authoritative source: `reconstruction/mumu-current/reference-unity/ExportedProject/Assets/Res/ViewPrefabs/UIJackpotView.prefab`.
`BuildJackpotPopup.Save` imports its full standard UI serialization, remapping UI/TMP script
identities to the installed official Unity packages and sprites to their recovered PNG
importers. It removes the unavailable original UIJackpotView, CashOutTip, SpineUtils and
SkeletonGraphic components. The three artwork objects become nested native animation
prefabs while preserving their original RectTransforms and sibling positions. No runtime
static hierarchy builder or third-party assembly is introduced.

The source's Text, TMP, Image, Button, Slider, Canvas and GraphicRaycaster fields are
retained. Button listeners are code-bound; serialized persistent calls remain empty.
The claim button uses the original four-second looping legacy `btnanim.anim` unchanged.
Its TMP sprite has the original 77x77 metrics, bearings (-5.4, 59.54), advance 77, glyph
scale 1.7 and character scale 1. The claim material is the original `#0A5902_4` with its
shader reference mapped to the official installed TMP shader; no visual parameters change.

## Window and delayed object presentation

PopupWindow constructor `23f9ea0` reads ELF `dbbee0 = (1f, .3f)` and
`dbb8d0 = (27,26)`, i.e. OutBack/InBack. Enter `23f9afc` resets content to fromScale
before tweening to toScale; exit `23f9ccc` resets to toScale before tweening to fromScale.
The source prefab has fromScale=0, toScale=1. Exact cubic Hermite curves encode the default
Back overshoot 1.70158, with endpoint slopes 4.70158/0 and 0/4.70158. Count uses the original
0.5-second default OutQuad from `23b38c0`, preserving the unchanged original amount getter.
Clocks use scaled Unity delta time; no Editor-specific timing path exists.

`GameDataManager.PlayBtnAnim` (`2370318`) deactivates its target, appends an interval,
invokes `2370c6c` to activate it and set localScale to Vector3.zero, then appends a default
OutQuad tween to scale one. ELF `dbc664 = .30000001192092896f` is the scale duration.
The jackpot plain button parent uses delay 1; CashOutTip uses .5. The popup owns these
clocks so the target can remain inactive during its delay. First-free hides only the
plain text object and does not request the delayed plain-button sequence.

`UIJackpotView.OnInitProperty` (`23b2b0c`) reads ELF `dbb670 = (300,2)`: these are
UIWindowType.Popup and UIWindowBgMaskMode.Black, NOT a sorting order and layer.
The prefab retains its original Canvas fields. The original window-manager mask/stack
and its ordering still need to be connected with the actual main-flow window presenter.

## CashOutTip

`CashOutTip.Init` (`23d7c40`) hides the whole object for GameData.IsA or when every
configured cash-out index has a matching PlayerCashOutDatas record. The predicate
`23d812c` compares only record.id; it does not inspect isCashout, type, step or count.
For the first missing id it requests PlayBtnAnim(.5), then displays current GreenCount
against ConfigManager.GetCashOutCash(index). Slider range is 0..1; over-target progress
is one, while text continues to display the actual balance. No player state is written.

ELF RELATIVE relocations resolved against script.json:

- `4f1ea38`: `You Can Cash Out <material="#003815_3"><gradient="cash">{0}</gradient></material> Now!`
- `4f1ea40`: `Earn <material="#003815_3"><gradient="cash">{0}</gradient></material>  more to withdrawl <material="#003815_3"><gradient="cash">{1}</gradient></material>.`
- `4f1d9b8`: `/`

Preserve the original double space and spelling. Remaining amount uses two decimals,
target uses zero decimals; progress text is formatted balance / formatted target.
After initialization UIJackpotView dispatches cash-out task refresh `(2,1)`.

## Integration boundary

`RecoveredJackpotPopup.Show` drives the actual prefab and the existing claim controller.
Count completion hides a shown wheel via the host, then starts the exit tween. After
deactivation it stops Sound1, resumes music and requests a fly-coin presentation with the
native completion callback. It does not credit or automatically complete that request.
The host must connect the existing main flow's CheckJackPot delay/event, window mask,
audio, fly-coin presentation and later symbol stage. Those integrations remain incomplete.

Current captures are `Artifacts/current-jackpot-window-{grand,major,minor}.png`, created
from this prefab by the current PlayMode test. The earlier `current-jackpot-popup-*`
images only cover artwork and do not demonstrate the complete window.

Validation: `Artifacts/jackpot-window-all-tests-2.xml` passed 181/181 PlayMode tests
(2026-09-08 10:40:59Z–10:41:04Z). The current prefab test exercises all three tiers,
actual Button listeners, delayed object activation, paused count animation, mock ad
failure/retry, count completion, exit deactivation and the deferred fly-coin callback.
Separate assertions cover original layout/font/sprite metrics and cash-out record lookup.
All three current window captures were inspected. The playback icon test inspects its
parsed Sprite character and mesh: TMP 3.0.9's GenerateTextMesh overwrites the aggregate
spriteCount field, so that aggregate alone is insufficient evidence of rendering.
