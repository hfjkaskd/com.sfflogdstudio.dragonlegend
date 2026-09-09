# Reel and coin audio consumers

RecoveredCoreAudio now consumes the previously unbound parameterless reel and
coin events. Names are authored in CoreAudio.prefab by BuildCoreAudio. No reel
timing, reward generation, ledger ordering, or SDK behavior is changed.

Native mapping verified against ARM and ELF RELA -> ScriptString:

| Existing event | Audio operation | Native evidence |
| --- | --- | --- |
| ReelStopSoundRequested | PlaySound(reelstop) | 23c01e0 normal stop callback, tail 23c02d4..23c02f4, GOT 4f1e438 |
| SpeedupSoundRequested | PlaySound1(speedup) | anticipation MoveNext 23c05f4, GOT 4f1e460 |
| SpeedupSoundStopRequested | StopSound1 | 23c03e4, before hiding anticipation and presenting stopped reel |
| CoinShowSoundRequested | PlaySound(coinshow) | RollReel stop callback 2377960, 2377ab8..2377ad4, GOT 4f1c3a0 |
| CoinRevealSoundRequested | PlaySound(coinReveal) | JinBiEffectItem MoveNext 23da0c0, GOT 4f1cfa8 |
| ExpSoundRequested | PlaySound(exp) | lamp arrival 23d9dcc, GOT 4f1d230 |
| CoinBurstSoundRequested | PlaySound(coinBrust) | Main arrival 23bfeb0, GOT 4f1e430; existing DownWinFlight presentation boundary |

Both Base WinFlight and Free RewardCollect.Flights are subscribed. Free coin
show/reveal/lamp sounds already travel through FreeSpecials.SoundRequested and
are not subscribed again here. Normal stop audio remains after the stop
presentation/shake/count increment. Anticipation stops retain their different
native behavior: stop channel 1, present the reel, then start the next anticipation;
no additional normal-stop sound is invented for those callbacks.

The existing shared music preference still controls both audio channels. All
subscriptions are removed before GM graph destruction. Static AudioSources and
sound-name configuration remain prefab-authored; there is no Update polling or
runtime construction of audio objects.

Remaining scope includes accepted Spin input audio, further unmatched events,
lazy peripheral windows, exact source catalog membership, and speaker/waveform
comparison. This increment does not prove complete 1:1 lifecycle or audio parity.

Validation: Unity 2022.3.62f3 with graphics enabled. The first run
Artifacts/core-reel-audio.xml passed the four-minigame, repeated-Free and manager
tests (3/3), but the expanded core fixture failed its coin-event coverage count:
its fixed Spin seed did not produce a Base coin. No channel-playing assertion
failed. The fixture now first supplies one stopped coin and reward through the
production CoinStops presenter, real lamp flight and real WinFlight, before its
unchanged actual Spin/Free/return path. This is explicit fixture-controlled input,
not proof that the selected random Spin path naturally includes a Base coin.

Artifacts/core-reel-audio-fixed.xml passes the corrected core test (1/1); process
29940 exited. Each observed play event clears its destination channel before the
production subscriber, then checks real AudioSource.isPlaying afterward. Thus an
older one-shot cannot conceal an unbound event. All six play-event categories
occur; anticipation stop checks second-channel stopped state and matches its
start count. The pre-Spin coin sequence checks exactly one show, reveal, lamp
arrival and burst. Remaining scene, persistence and music-switch assertions pass.
The unchanged three other tests were not rerun after this fixture-only correction.
