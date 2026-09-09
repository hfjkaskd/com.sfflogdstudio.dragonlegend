# Main mode visual consumers

Main.SetInitShow 23bca34 branches on exact zero (Base), otherwise Free. The
production RecoveredMainModeView applies BaseBg/FreeBg, Yanhua, Bottom/Main/Free,
Base Roll/FreeRoll and Result/FreeResult together. It restores DownWinText to
GOOD LUCK in Free and Base with zero temporary total; otherwise Base uses the
temporary total formatted to two decimals. It does not alter the accumulated
amount or debit spins. GameEntry binds and applies the current mode at startup.

FreeRoll retains source position (-1.62,-71), size 948x515 beneath QiPan. Its
authored 15 native mini-reels are instantiated from the existing FreeReels prefab,
with the established 100 pixel/unit conversion. FreeResult remains that native
prefab's child, so it shares the Free visibility switch. InitializeFreeReels is
explicit and uses the actual generated FreeSpinResult. RefreshFreeCount remains
separate, matching source cover callback order rather than generating or refreshing
from a per-frame mode watcher.

Yanhua source Rect 224521597976012101 is Node/SkeletonGraphic (ef_slyanhua), size
50x50, centered anchors/pivot, anchored (0,83). Source Node lists it before QiPan.
The complete original binary consumes 23,443 bytes: 30 bones, 33 slots, 335 region
attachments and one 4-second `animation` clip with 78 timelines. The extractor
preserves region/atlas sequence data; prepare_main_fireworks.py independently
samples 368 boundary/intermediate geometry frames. No foreign runtime is added.

Native Canvas order is Main background -4, fireworks -3, body -2, board -1,
existing Main/world reels at zero relative to Main. The fireworks graphic keeps
source Layer 0 under a Layer 5 Canvas wrapper, for the same camera-culling reason
as the dragon wrapper. Its native Animation loops on activation and stops while
inactive. The unchanged recovered atlas is used without compression.

Still incomplete: original current-symbol definition selection, CashOutTipSeq
kill/hide and Base ShowCashOutTip consumers, and production Free entry/exit
orchestration. This component is a visual consumer with separate initialization
methods, not proof of a complete SetInitShow or whole Free lifecycle.

Validation: `Artifacts/main-mode-tests.xml` passes 351/351 PlayMode tests, including
all 368 source fireworks vertex frames and the existing warmed-up zero-GC check.
The actual GameEntry fixture switches to Free, initializes its real result,
refreshes 12 spins, checks all 15 mini-reels and the separate visibility groups,
then returns to Base and restores temporary 123.45 as $1.23. A with/without
fireworks pixel comparison verifies the source effect actually contributes to
the production render. Fresh current-main-free-mode.png and
current-main-base-return.png were inspected. These exercise the production
visual consumer directly; they do not exercise the pending entry/exit orchestration.
