# Dragon-ball small-game routing and reward configuration

CheckSmallGame MoveNext 0x23cfe28 maps BallType 0 to config column 0, 1 to 1,
and every other value to 2. ConfigManager.GetFreeReward 0x236c01c constructs
four weights in this order: Rrggiomg.RollGlorg (+0x50), RollKtggl (+0x58),
RollRrgogirg (+0x60), RollLiqki (+0x68), reading the supplied column from each.
It uses RandomListWeight and maps indices 0/1/2 directly, everything else to 3.
The dump's FreeSpinReward enum is Slot=0, Wheel=1, Treasure=2, Lucky=3.
Thus ball color selects a weighted distribution, not a fixed game.

RecoveredGameplayRules now implements that lookup using a reusable four-item
scratch list, preserving the existing inclusive cumulative float comparison
and integer RNG call. Invalid columns still fail before the random draw.

GetLuckyReward 0x236c30c reads LiqkiRgkorp[0..1] and draws an integer with
upper bound +1, then converts to float. GetSlotReward 0x236c3b0 first selects
an index from GlorgKgiitr (+0x88), then draws inclusively from GlorgMin[index]
(+0x78) through GlorgMoj[index] (+0x80), also converting int to float. Neither
method applies currency division, bet multipliers or balance writes.

GetWheelInfo 0x236c470 lazily caches one eight-entry dictionary with wedge
values [Major,Cash,Mini,Cash,Grand,Cash,Mini,Cash]; WheelType enum values are
Major=0, Cash=1, Mini=2, Grand=3. GetWheelReward 0x236c5b0 directly returns the
configured KtgglRgkorp[index] as float without consuming RNG. RandomWheelWeight
0x236c620 is a raw-assembly tail call to RandomListWeight with
KtgglRgkorpKgiitr (+0x98). These methods are now restored in the rules layer.

Tests compare three color-column selections against explicit thresholds and
the next RNG value, live config edits, invalid index behavior, Slot's two draws
and Lucky's one draw, integer-to-float rounding above 2^24, cached eight-wedge
mapping, zero/empty weight behavior and no RNG consumption in reward getters.
Unity 2022.3.62f3 full PlayMode suite **304/304 passed** in
Artifacts/free-minigame-rules-tests.xml.

The original UIWheelView, UITreasureView and UILuckySpinView prefabs are present
in the current reverse delivery. Their runtime windows/interactions, full
CheckSmallGame entry effects and production binding remain pending. Restoring
configuration does not substitute for implementing those windows. SDK unchanged.
