# Bank continue advertisement branch

The existing selection model now handles the native OpenBtn and UnPlayBtn paths. The actual Bank popup and its core end-flow wait remain pending; SDK implementations are unchanged.

## Button dispatch and latch

OnClickButton ARM 2391e78 calls its base handler, returns if isContinue (+B8) is set, and compares object names. Unknown names have no effect.

- OpenBtn: click sound, set isContinue=true, reward ad with placement `bank` and scene `Openbank`. Success 2392e24 calls PlayAd 2392128; failure 2392e28 clears the latch. No reward RNG is consumed before ad success. This handler does not hide the finger.
- UnPlayBtn: click sound, interstitial request with placement `iv_close` and scene `bank`, then set isContinue=true and Hide immediately. No ad-completion wait.
- The latch only guards these window buttons; OnClickLongZhu remains separately guarded by selected.Contains. The existing LocalAdFacade concurrent request policy is unchanged.
- ELF relocation-backed strings: 4f1d038 OpenBtn, 4f1d040 UnPlayBtn, 4f1d028 Openbank, 4f1d030 iv_close, 4f1d048 bank, 4f1cf38 click.

## Success selection

PlayAd 2392128 creates two ordered lists from native WinList [0,1,2]: unselected item indices, and categories unequal to the live shared tempIndex. It draws a single integer offset with Random.Range(0, unselected.Count) at 2392404. The same w23 offset indexes the first list at 2392418 and the second at 2392430. GetBankReward(category) at 2392470 performs the existing inclusive integer reward draw.

It appends the chosen item to selected, refreshes ad indicators at 23924e8, then starts that item's PlayAnim at 2392540 with callback 2392e30. It does not change tempIndex. Consequently, when one item remains the offset is zero and the reward uses the first category other than tempIndex, even if a previous continue already paid that category. Do not replace this with an independent random category draw or exclude all previously paid categories.

Callback MoveNext 2392ec8 compares selected.Count and WinList.Count at 2392f44. Unequal clears isContinue at 239304c; equal waits 0.5 scaled seconds in Update timing 8 then hides. As with manual selection, this is a count check at flight arrival, not a completed-flight tally, and no second comparison follows the wait. Lifetime guards reject discarded GM callbacks.

OpenAllReward 2394580 was inspected but is not the OpenBtn success implementation: it starts one supplied item/reward animation with a forwarded callback and awaits the existing completed UniTask. Do not infer an all-items reveal loop from its name. Its reachability and production role remain unproven.

## Verification

Tests compare the native two-list algorithm and Unity Random.state across 64 seeds; cover unknown/latched buttons, failure without RNG consumption, retry, scene identifiers, reward captured after success, unchanged tempIndex, flight-gated unlock, and immediate interstitial close. A PlayMode case performs both continue rewards, verifies the final category rule and scaled half-second close, then checks cancellation/reset while the advertisement is pending.

Unity 2022.3.62f3 focused PlayMode result: `Artifacts/bank-open-verified.xml`, **8/8 passed**, process 44368 exited. An earlier final-ball fixture incorrectly assumed weights [0,1,0] force category one; the native cumulative >= comparison can select the zero-weight first entry when Random.Range returns zero. The fixture now uses a single weighted category and retains all three reward ranges; the production weighted sampler was unchanged. Full regression was not repeated for this model-only increment (previous full run: 384/384 in `bank-item-regression.xml`). Production UI integration and visual equivalence are not established by these model tests.
