# Slot entry and actual Lucky Spin window

Native CheckSmallGame 0x23cfe28 selects Slot before activating UIMainView's
SlotPrefab. The router owns SetTaskData(4,1). Entry places the icon at the passed
ball position and pulses captured XY scale *1.5, Z=0 over .3, then returns to
captured XY, Z=0 over .3. Its independent .6 wait hides the icon, draws
GetSlotReward (0x236c3b0) and shows UILuckySpinView. Original icon GO
1589652054545887 is inactive, 244x186, scale 1, sprite
0222fb72a8638f348932f3b3280a796e = free_game/mfyx_icon_slots.

RecoveredFreeSlotGame and FreeSlotGame.prefab implement that entry and call the
real RecoveredLuckySpinWindow. They do not repeat task accumulation or credit.

UILuckySpinView OnBeforeShow 0x23b6140 plays jump, stores reward/callback,
initializes four columns to 9 with isDot=false and plays idle. Its hidden Btn
group remains hidden. After the standard popup enter, StartLuckySpin MoveNext
0x23b6960 waits .5, plays start once and slotsSpin, waits .5, then generates
digits from (reward/100f).ToString("0.00",InvariantCulture). Non-digits become
10. The complete formatted list is retained; the four columns read its first
four elements. There is no extra LuckySpinResult.GenerateResult draw.

It shakes the machine for 1.1 (intensity/frequency 20), starts each non-dot
column, sets its target and waits .05 after each. After .15 it captures Time.time
and schedules column i at max(0,.3*(i+1)-(Time.time-start)). Stops do not wait for
prior column completion. Every actual column completion increments stoppedCount
and plays reelstop2 (0x23b66e0); bounce callbacks start a .3 shake with 20/20.
The native wait predicate is stoppedCount >= columns.Length (0x23b693c).
After that wait and .7, it clears the spinning flag, starts window Hide and
immediately shows UIRewardView with the original amount/callback. Hide is not
awaited. The real shared popup and cash presenter perform claim, flight and
callback-before-credit in the same way as the Lucky branch. SDK behavior stays
unchanged.

Strings resolved via ELF RELA to script.json: 0x4f1dff8="0.00",
0x4f1dff0="slotsSpin", 0x4f1dd00="start", 0x4f1d000="jump",
0x4f1dfa8="reelstop2", 0x4f1cf58="idle". Constants: 0xdbc520=.15,
0xdbc4a4=1.1; .3 column stop interval is set in constructor 0x23b6824.

BuildLuckySpinWindow preserves the original window subtree and authored text,
button visibility, column positions, font and image references. The original
machine rect is size (1010.0239,583.99994), pivot (.4678107,.5), position (0,105).
ef_slotsspin's complete 2403-byte skeleton is decoded in ef_slotsspin.json;
one mesh and both idle/start clips are converted to existing native UI mesh and
Animation components. prepare_lucky_spin_art.py samples 36 independent source
geometry frames. The geometry test compares all active attachment vertices.

Known incomplete visuals: original title UIShiny component is not converted;
its source parameters remain in ReferenceOriginal (duration 2, width .25,
rotation 135). The existing GM controls still overlay popups. Full main/free
camera/world-coordinate placement and production FreeEntry binding remain
pending, as do Wheel and Treasure. Sound requests are emitted through the
existing event interface; complete game audio binding is still a broader gap.
The integration tests do not establish full lifecycle or whole-game parity.

Validation: Artifacts/free-slot-game-tests.xml reports 312/312 PlayMode tests
passed with graphics enabled on Unity 2022.3.62f3. This covers the new art
geometry samples, both decimal-position window flows and routed Slot entry
through actual claim/cash arrival. A subsequent screenshot-timing-only change
waits for the plain-button reveal to settle; the affected window test passed
again (1/1, Artifacts/lucky-spin-window-capture-tests.xml). Fresh 1080x1920
current-lucky-spin-window.png and current-lucky-spin-reward.png were inspected:
the machine shows 1.23, then UIRewardView shows $1.23 with CLAIMx2 and Only $0.62.
The GM overlay remains visible in both and is not treated as matched original UI.
