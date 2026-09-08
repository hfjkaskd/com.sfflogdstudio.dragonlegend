# Jackpot popup artwork and claim flow

Source assets: `C:/Projects/Nut Sort Relax/reconstruction/mumu-current/reference-unity/ExportedProject/Assets`.
All JSON files are complete decodes of the original 4.1.24 binaries, with SHA-256 fingerprints.

| Resource | Bones / slots | Attachments | Animations |
| --- | --- | --- | --- |
| Res/Spine/jackpottc/ef_jackpottc | 116 / 70 | 60 regions, 12 meshes | grand, major, minor, each 3 seconds |
| Res/Spine/penqian/ef_slpenqian | 127 / 61 | 120 regions, 60 have a 10-frame sequence | both, 2 seconds |
| Res/Spine/shoucanggl/ef_shoucanggl | 16 / 13 | 10 regions | shoucanggl, 2 seconds |

`ef_slpenqian.both` contains 30 Loop sequence timelines. Their frame delays vary between
1/30 and 1/25 seconds; do not replace them with one common frame rate. Some sequence
attachments have no timeline and retain their setup frame. All three source animations
are looped in the original popup prefab. Source prefab order: fountain, jackpot, reward
text, buttons, CashOutTip (whose child includes shoucanggl).

Weighted popup mesh geometry and NoScaleOrReflection inheritance are now supported by
the existing native MaskableGraphic renderer. Each effect is a single CanvasRenderer;
no skeleton bone GameObjects or per-frame mesh/pose allocations are required. The runtime
Animation clock selects shared curve assets. UVs preserve the original atlas rotation,
trimming and PMA colors. No source Spine assemblies are imported.

Regeneration:

1. Run `Tools/extract_coin_effect.py INPUT.skel.bytes Tools/Evidence/JackpotPopup/NAME.json`
   separately for each resource. Atlas is read beside input; never pass it as an argument.
2. Run `Tools/prepare_jackpot_popup.py` to flatten weighted mesh inputs into Artifacts.
3. Unity batch mode: `-executeMethod BuildJackpotPopupArt.Save -quit`.
4. `Tools/sample_jackpot_popup_reference.py` creates 40 source geometry samples. Times
   are rounded to float32 before evaluating attachment switches; 1/30 is an exact frame
   boundary in the original float32 timeline, not the decimal Python approximation.

The resulting prefabs are artwork for the forthcoming complete popup. They do not yet
contain reward text, claim buttons, CashOutTip state, window transition or claim logic,
and are not automatically shown from the real Wild reward chain. Their render captures
must not be described as a finished popup or the complete game.

## Additional native claim evidence (ELF RVAs)

- OnClickButton (`23b32a8`) sets the click latch before checking the button name.
  ClaimBtn is the advertised action; UnPlayBtn is the alternative. Failed/cancelled
  advertised callback (`23b3cec`) clears the latch. Successful callback (`23b3cb0`)
  multiplies winCount by playAdFan and calls finishCall. Retain the existing IAdFacade
  mock; do not introduce a real SDK or auto-complete a pending mock request.
- finishCall (`23b38c0`): if curCount equals winCount, finish immediately. Otherwise
  play `count`, animate the reward text to winCount over 0.5 seconds, then finish.
  Getter (`23b3c58`) reads curCount; setter (`23b3c70`) only formats the supplied value
  into RewardTxt, without changing curCount.
- Finish (`23b3bac`): if UIWheelView is shown, hide it first, then call this window's
  BaseWindow.Hide (`23f9358`). The conditional window is UIWheelView, not UIJackpotView.
  This was verified with direct disassembly and method relocation records.
- UnPlayBtn requests interstitial placement `iv_close`, scene `jackpot`. It multiplies
  by unPlayAdFan without waiting for an interstitial completion callback.
- Popup strings: normal multiplier 1 is `<sprite name="tc_btn_bofang">CLAIM`;
  other multipliers use `<sprite name="tc_btn_bofang">CLAIMx{0}`;
  first-free uses `CLAIM`; unadvertised reward is `Only {0}`.
- PopupWindow ctor (`23f9ea0`) constants at ELF `dbbee0`: toScale=1, scaleTime=0.3;
  `dbb8d0`: enter ease 27 (OutBack), exit ease 26 (InBack). Serialized fromScale=0.
  Enter (`23f9afc`) sets content scale to fromScale before tweening to toScale;
  exit (`23f9ccc`) sets it to toScale before tweening to fromScale.
- After hide still dispatches fly coin and resumes CheckJackPot only through its final
  callback; see `../jackpot-flow.md`. These are necessary next steps, not implemented
  by the artwork prefabs.
