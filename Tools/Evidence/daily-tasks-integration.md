# Daily tasks and full-scene Free rendering

The TASK Button now opens the recovered UIDailyTaskView layout with six authored
TaskItem rows inside a standard ScrollRect. Entry art uses regions from the original
ef_taskicon atlas. Scripts bind all Button listeners; no persistent UnityEvents or
runtime-generated static layout is used.

Superseded reset/receipt behavior: see task-native-alignment.md for the native
audit and corrected implementation. The initial defensive behavior described
below was replaced after the user's request to match the original.

Rogk supplies IDs, descriptions, goals, rewards and jump flags. Existing gameplay
events already advance IDs 1–5 (Big Win, Jackpot, Free Game, free Slots mini-game,
Bonus Game). Login supplies ID 6. Persisted DailyTaskDay resets only task records
at local midnight, including while the task window is closed. Clock rollback does
not reset claimed rewards. Legacy records migrate using LoginTime.

Completed tasks with jump=0 pay the configured reward and request iv_close/task
interstitials under the existing policy. jump=1 uses the recovered reward popup
art with a plain .5 multiplier or rewarded-ad 2 multiplier. Ads use the existing
local simulation transport. Claims validate completion and receipt state again
at payout, and save the claimed flag together with the updated balance; failure
does not claim or pay. This deliberately defers the native early receipt flag
until successful payout so an interrupted ad does not consume a reward.

Native references: TaskItem.Init 23acdf8, ClaimTaskReward 23ad164,
GameData.ClaimTaskReward 236f210, UIDailyTaskView.CountDown 23ada40.

Free symbols had two independent issues: an opaque Base board cover was shared
with Free cells, and the FreeRoll subtree remained on layer 0 while the game's UI
camera only renders layer 5. FreeSymbolItem supplies a black alpha .65 cover;
BuildMainModeView and BuildFreeVisibility author layer 5 throughout FreeRoll.
The mini-reel SortingGroup is above the board. World SpriteRenderer gameplay
objects remain world renderers, not UI Images.

Validation: 68 PlayMode tests passed in Artifacts/daily-verified.xml. The integration
test covers real popup raycasts, login claim, duplicate rejection, ad failure and
retry, successful task reward and scene teardown. A rendered-pixel comparison in
the complete GameEntry scene verifies ordinary stopped Free symbols contribute
visible pixels above the board (the earlier isolated-reel render missed camera
culling). Captures: daily-tasks.png and free-symbols-full-scene.png.
