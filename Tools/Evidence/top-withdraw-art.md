# Main top withdrawal artwork

Source: ReferenceOriginal/Res/ViewPrefabs/UIMainView.prefab,
Node/Top/CashOut/SkeletonGraphic (ef_sltxan).
Its RectTransform 224603576887721950 has center anchors, position (-5,11),
size (456.99997,147), pivot (.5,.5034014), and unit scale. SkeletonGraphic
114132299442727904 starts `animation`, loops, and uses scaled time.

The original ef_sltxan.skel.bytes under Res/Spine/按钮/sltxan was fully
consumed: 3311 bytes, 13 region attachments, one 2-second animation with
22 timelines. BuildTopWithdrawArt.Save authors the recovered region rig,
native Unity Animation clip and prefab using the original atlas. No Spine
assembly is added. The atlas keeps premultiplied alpha without importer
alpha dilation or compression.

Tools/prepare_cashout_entry.py now accepts optional name/output arguments;
its existing defaults are preserved. Independent original-data sampling
produced 42 poses, including timeline key boundaries and nearby times.
RecoveredJackpotPopupArtTests checks geometry against these poses with
the existing .00008-world-unit tolerance, and zero managed allocations
over 1000 warmed samples.

Unity author process 38108 exited successfully. Graphics-enabled PlayMode
process 16272 exited; Artifacts/top-withdraw-art-tests.xml reports 15/15
passed, 2.8451321 seconds. This is geometry/playback validation, not a
new full-screen visual comparison or completion of the entry.

Remaining: the current BalancePanel does not contain Top/CashOut or its
WithdrawBtn. Original WithdrawBtn 1985305603660343 is a standard Button
with an empty raycast graphic, sibling to the three visual objects. The
source full-stretch hit rectangle has position (-4.1029053,0), size delta
(-94.3672,0). Implementing the entry must preserve its geometry while
respecting the user's requirement that visuals and Button share a clear
prefab hierarchy. The current central GM control also needs an actual
overlap/raycast check when this entry is integrated.

Main.OnClickButton 23bd4e8 routes WithdrawBtn and CashOutb into the same
branch. NeedWithDrawOpne controls the original destination; it is not the
IsA flag. See main-cashout-entry-audit.md. This art-only change does not
alter that routing or the existing SDK facade.
