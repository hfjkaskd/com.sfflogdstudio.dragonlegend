# Main collection entry and shared destination

RecoveredCollectEntry connects the real Main Treasure Button to the recovered
collection window. Native UIMainView.OnClickButton 0x23bd4e8 compares Treasure
(ELF relocation 0x4f1e2e0), calls SoundManager.PlaySound(click), then
ShowWindow<UICollectView> (0x4f1e2b0). The existing sound-request contract emits
click before opening and also forwards the collection Close click. A complete
native SoundManager/audio-output integration remains a broader pending task;
the request event alone is not proof of audible output.

Original hierarchy UIMainView/Node/Tubiao/Treasure has all four activeSelf values
set to 1. This entry preserves that source default, source Button parameters,
Tubiao RectTransform and the same animated ef_shoucangicon destination. No new
level gate or profile-specific hiding rule is invented. Further lifecycle-wide
visibility auditing is still required together with Base/Free transitions.

BuildTreasureDeparture now authors the icon into CollectEntry.prefab, while
TreasureDeparture.prefab owns only its pooled flight controller. GameEntry
creates one CollectEntry after configuration is ready and binds the current
player, rules, profile and language. FreeTreasureGame.Bind receives that same
destination RectTransform explicitly; it no longer brings a duplicate icon.
The native Treasure card flight calculation, event timing and cash ordering
remain unchanged.

The first entry click instantiates the authored collection window under Main;
later clicks reuse the same window and its ListView. Close retains the window.
Changing the GM profile destroys the old entry and its externally parented
window, then creates an entry bound to the new profile. This keeps A/normal
window initialization separate and prevents stale profile references or
duplicate main icons after repeated switches.

RecoveredCollectEntryTests loads actual GameEntry, uses EventSystem raycasts
and the top hit to invoke standard Button pointer clicks, opens/closes/reopens,
checks live collection progress and retained scroll/list identity, then changes
GM to A while the window is open and back to US. It checks old owner/window
destruction and exactly one entry. Existing FreeTreasureGame integration tests
now explicitly assert the shared Main destination and retain their actual
flip/claim/cash/departure checks. Fresh captures come from these real main-entry
interactions, not a separately instantiated collection-window fixture.

## Initial frame ordering

Native BaseUIManager.Base_ShowWindow (dependency/33bcb9c.c) runs BeforeShow,
then SetActive(true), then the enter animation. Collection initialization now
follows this order. Its Adapt calculation is available before OnInit's width
read, matching the original active-prefab Awake path.

Native Main.OnBeforeShow calls InitSpinSequence (0x23ba59c), which creates a
DOTween sequence before any collection click. GameEntry now ensures the shared
official-Unity animation runner exists at that same startup stage. This change
does not claim to implement the initial sequence's separate two-second action.

The first pointer test clicked transform.position, but the source Treasure
Button has pivot (0,1), placing that point on the screen edge. It now clicks
the actual RectTransform center, with no runtime geometry change. A second
fixture issue was calling pointer handlers from a resumed coroutine after
Update, exposing ScrollRect to the zero-scale popup frame before animation
Update. The test now installs a temporary BaseInputModule and lets the actual
EventSystem.Update call Process; it does not override script execution order.
The test asserts first-open content offset remains zero. No scroll reset,
disabled runtime ScrollRect or minimum-scale workaround was introduced.

Final full PlayMode regression passed 343/343 in
Artifacts/collect-entry-verified.xml. The fresh normal/A main-entry captures
were inspected; the normal initial list now begins with the complete first
row, and the A capture retains the collected count after the GM switch.

## Remaining chain identification

Do not confuse Main.CheckRewardCollect with completing the treasure set.
Its MoveNext 0x23ceeb4 reads FreeSlotGameResult.GetSymbolIdByPos and operates on
free reel symbols (including IDs 9/11) and the per-reel reward dictionary.
Callbacks are 0x23bfca8, 0x23bfcec, 0x23bfeb0, 0x23c0168, 0x23c01d0 and
0x23bf690. This belongs to the pending production Free reward-collection/end
chain. Treasure-set eligibility/presentation must instead be traced through
the actual GiftItem/UICashOutView paths: GiftItem.InitUI 0x23a6a44 formats
GetCollectReward and raw collection progress; do not invent a Main payout
branch based solely on the misleading CheckRewardCollect name.

This does not complete production Free board orchestration, complete-set award
flow, general multi-window stack handling, full audio playback or Main visual
fidelity. Those requirements remain active.
