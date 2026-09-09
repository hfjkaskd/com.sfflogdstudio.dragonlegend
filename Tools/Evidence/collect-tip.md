# Treasure CollectTip

## Native behavior

Raw ARM64 `C:/Projects/Nut Sort Relax/reconstruction/mumu-current/native/game/023db440.asm`:

- IsA==true hides the GameObject and returns without config/progress reads.
- Normal profile first requests PlayBtnAnim(.5), then reads GetCollectInfos().Count and PlayerCollectDatas.Count. It does not deduplicate IDs, sum card counts, filter received records, check level, or select an icon.
- Tips use total minus record count. ELF relocation 04f1eb50 resolves to `Collect <material="#003815_3"><gradient="cash">{0}</gradient></material> treasures to redeem the rewards.`
- Slider min/max are 0/1; value is floating recordCount/totalCount passed to the actual Unity Slider setter. Progress format is `{0}/{1}` (04f1d9c8). Reward is GetCollectReward's zero-decimal currency string.
- CollectImg is never changed by this method. No persistence or payout occurs.

PlayBtnAnim `2370318` immediately hides its target, delays .5 scaled seconds, calls `2370c6c` to activate and reset XYZ scale to zero, then scales to one over .3 seconds with default OutQuad. The runtime uses the shared native Unity Update animation service, so the hidden object does not stall its own reveal. Repeated initialization does not invent cancellation of an earlier sequence.

## Prefab

BuildCollectTip copies RectTransform 224574304967836189 and its entire subtree from current `ReferenceOriginal/Res/ViewPrefabs/UITreasureView.prefab`. The panel remains bottom-anchored, size 1080 x 275.5964, pivot (.5,0), position zero. Original TMP settings/materials, non-interactable Slider, child order, images and dimensions remain serialized.

The source SkeletonGraphic is replaced with the already recovered native `JackpotPopupArt/ef_shoucanggl` animation prefab, retaining the source transform and sibling position. Its 16-bone/13-slot source animation and geometry validation are documented in `JackpotPopup/README.md`. No new third-party runtime is introduced.

The t_hb_amazon extracted image had remained a plain texture. Its importer now exposes a non-resized Sprite at PPU=100. Original t_icon_02 remains unchanged by Initialize, including its authored selection.

## Verification scope

RecoveredCollectTipTests checks the actual prefab, duplicate/unknown/received record counting, remaining text, progress and reward formatting, no saving, unchanged icon, pause, all frames of the delayed reveal, over-complete text with native Slider clamping, and IsA's early return before data access. It captures `Artifacts/current-collect-tip.png` from the current prefab at 1080 x 1920.

Full Unity 2022.3.62f3 PlayMode regression passed **327/327**, `Artifacts/collect-tip-tests.xml`, including the corrected TreasureCard OutBack test. The fresh collection-tip render was visually inspected: native bottom panel, animated glow, live 3/4 progress and $1,000 reward card are visible.

The Treasure window now calls this component when its card finishes flipping; see `treasure-window.md`. Main entry flight and card-departure consumers remain pending. The isolated tip test alone does not prove full lifecycle integration.
