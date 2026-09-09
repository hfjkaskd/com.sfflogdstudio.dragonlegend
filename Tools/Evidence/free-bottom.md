# Main Free bottom counter

Source UIMainView Bottom/Free Rect 224396801632836831 is centered, size 100x100,
position zero. Its Bg 224610726481676589 is 640x86 at (0,-73), a Sliced Image using
17c564eadab8a3940aac81ae943b3097 (zjm_s9g_spin_bg). Its Text Rect is 200x50 at
(0,-5), font size 60, no autosizing/wrapping, original vertex gradient, Quorum SDF
and #2B1506_3 shared material. BuildFreeBottom copies the complete source subtree.

InitFreeSpinTimes 23bf060 formats live FreeSpinCount using:
`FREE SPIN <material="#1A1457_3"><gradient="free">{0}</gradient></material> TIMES`.
The missing #1A1457_3 material is recovered with its original property values;
only the shader and font-atlas GUIDs map to the project's official TMP shader and
unchanged recovered Quorum atlas, as for existing TMP materials. The `free`
gradient is already available under Resources/Color Gradient Presets.

SetInitShow 23bca34 switches Main and Free with exact-zero Base / nonzero Free
branch. It does not itself refresh the count. Transition callback 23bf73c calls
InitShow, InitFreeReels, then InitFreeSpinTimes. FreeAutoSpin 23bf158 debits first
and generates; callback 23bf7d0 starts AutoFreeSpin. Its MoveNext 23c4758 clears
the reward map at 23c48d4, calls InitFreeSpinTimes at 23c48dc, then starts columns.
Thus the counter must not refresh when the debit happens while generation is
still pending.

Production GameEntry binds the authored bottom to the actual PlayerProgress and
FreeSpinEntry. It applies initial mode and refreshes on PresentationRequested
(after generation). Apply visibility and RefreshCount remain separate APIs to
preserve the native separation. OnDestroy removes the old entry subscription so
GM replacement cannot update a destroyed label. No per-frame polling is used.

This recovers the missing bottom and count binding, not the complete Main mode
orchestration. The transition consumer must still call RefreshCount at the source
cover stage while all background/reel/result/Yanhua states switch together.
Base/Free mode application is exercised explicitly in the test; the current
free-bottom capture still has Base reels and is not a complete Free mode capture.

Validation: `Artifacts/free-bottom-tests.xml` passes 349/349 PlayMode tests. The
new production fixture checks 12 before generation completion / 11 after it,
zero-entry rejection, exact nonzero mode visibility, authored Sliced background,
font settings, supported rich-text material and GM teardown. Completing the old
entry after teardown no longer touches its destroyed label. The fresh
current-free-bottom.png was inspected and shows the original count bar and
separate digit style. SDK behavior remains unchanged.
