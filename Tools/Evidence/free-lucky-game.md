# Free Lucky branch with the actual reward popup

CheckSmallGame MoveNext 0x23cfe28's Lucky branch uses UIMainView.RewardPrefab
(+0x140). The current original UIMainView prefab references GameObject
1092105871539913: inactive Image, size 498x368, center anchors/pivot, localScale
(.5,.5,.5), sprite GUID 34837b9c28c8b2a4f86a768fd659213d. That GUID resolves to
Res/UI/pop-up/ty_hb_meijing.asset, already recovered as the matching PNG sprite.

Entry sets its world position to the passed ball position, activates it and
captures its current XY scale. It scales to XY * 1.3, Z=0 over .3 seconds,
then callback 0x23bfc4c scales back to the captured XY, Z=0 over .3 seconds.
The independent .6s wait then hides the Image, draws GetLuckyReward and opens
UIRewardView with amount and the float callback. ELF 0xdbc678 is 1.3,
0xdbc664 is .3 and 0xdbc450 is .6. The entry does not reset scale on later calls.

Resolved window metadata from ELF RELA and script.json:
0x4f1c5d0 -> UILuckySpinView (Slot branch), 0x4f1c5d8 -> UIWheelView,
0x4f1c5e0 -> UITreasureView, 0x4f1d2e0 -> UIRewardView (Lucky branch).
The existing RecoveredBonusRewardPopup is the actual recovered UIRewardView;
its historical Bonus name does not mean a different source window.

RecoveredFreeLuckyGame and its prefab now implement this entry and use that
existing popup. Its native buttons, reward-ad/interstitial behavior, .5 plain
multiplier / 2 advertised multiplier and count/exit animations remain in the
shared popup. After popup hide, cash flies through the existing presenter to
the main title. Its arrival callback invokes the float reward consumer before
the shared presenter credits the balance. No extra balance write is added.
The popup's canvases receive the main UI camera at bind time.

The integration test loads actual GameEntry, uses the authored FreeLuckyGame,
pauses entry, checks the .65 XY/zero-Z pulse peak and .6s popup timing, clicks
the actual plain button, waits for real cash flight and verifies callback
before credit and the final balance. Fresh 1080x1920 captures are generated as
current-free-lucky-icon.png and current-free-lucky-reward.png.

Pending: weighted CheckSmallGame router and entry effects for the other three
branches, actual Slot/Wheel/Treasure windows, production FreeEntry/ball-scan
binding, full camera/world-position integration and remaining lifecycle/UI
gaps. This is the real Lucky branch, not completion of all four games.
SDK handling remains unchanged.

Validation wait corrections: actual entry resource loads now use a bounded
five-second realtime wait instead of a 200-frame cap in the integration tests.
The previous failures moved between Free Bonus and GM switching and completed
before loading finished. Lucky cash completion also uses a realtime deadline:
CashFlightItem schedules departures with unscaledDeltaTime, so captureDeltaTime
cannot substitute for that elapsed time. Gameplay timing and SDK code were not
changed to accommodate tests; pulse and popup timing assertions remain exact.

Final validation: Artifacts/free-lucky-game-complete-tests.xml reports 305/305
PlayMode tests passed on Unity 2022.3.62f3 with graphics enabled. The fresh icon
and reward captures from this run were inspected at original 1080x1920 size.
They show the cash icon, real reward amount, CLAIMx2/plain controls and payout
tip. Existing purple GM controls still overlay the popup, and the production
main/free layout remains incomplete; these captures do not establish full
visual parity. SDK behavior remains unchanged.
