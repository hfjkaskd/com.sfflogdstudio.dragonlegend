# Native sound manager and production core audio

The production GameEntry prefab now owns a prefab reference to CoreAudio. Its
three authored Unity AudioSources consume existing core sound events instead of
leaving those events unhandled. SDK facades are unchanged.

Native evidence:

- SoundManager fields (type 8730): bgm +30, sound +38, sound1 +40,
  name dictionary +48, remembered BGM +50. OnInit 237a590 keys clips by name.
- ChangeBGM 237a764 remembers the requested name before checking availability
  and IsMusic; successful requests assign clip and call AudioSource.Play.
- PlaySound 2376cf0 and PlaySound1 237a8b4 use separate AudioSources and
  PlayOneShot at volumeScale 1. Native PlaySound's diagnostic LogError(name)
  is omitted; it is not a gameplay/audio operation.
- Both IsMusic 237a6e8 and IsSound 237a838 read PlayerData +38 (IsMusic).
  Do not invent a second sound preference or mute existing one-shots implicitly.
- SetMusic 237a64c stops BGM when disabled, otherwise indexes the remembered
  name, assigns clip and plays. Unlike ChangeBGM, an unknown remembered name
  is not silently ignored here.
- StopSound1 237a964 / StopSound 237a994 stop their respective channels.
  PauseMusic 237a9c4 and CtnMusic 237a9f4 both check IsMusic; CtnMusic uses
  Play, not UnPause.
- Source Loading.unity under reverse delivery/UnityFramework/ReferenceOriginal
  has AudioSources 33/34/35: playOnAwake false, volume/pitch 1, priority 128,
  spatial blend zero, BGM looping, other channels not looping.

The 41 OGG files are byte-identical copies of ReferenceOriginal/AudioClip
(SHA-256 comparison: zero mismatches). The source clips total 2,359,027 bytes.
Per user asset-reference requirements, the prefab serializes names and a Resources
path instead of strong AudioClip references. Clips load on first enabled request
and remain cached for that manager lifetime; muted requests do not load clips.
The first request can incur decode/load cost. No Update scans or runtime creation
of static AudioSource hierarchy are used. Extracted importer metadata is not
proof of the original encoder/import settings; output waveform parity is unproven.

Bindings cover the existing playfield forwarding boundary, Bonus flow and its
window, Free entry/exit, Free reel stops/specials, cash flight, collection entry,
four minigame windows, MoreSpin/MoreWild/Bank and their existing entry events.
Bindings deliberately avoid additionally subscribing to children whose event is
already forwarded by a parent. GM release unbinds before destroying the old graph
and stops all three sources; it does not modify the SDK cancellation semantics.

Remaining core audio work: Base reel stop/speedup, coin-stop and win-flight
parameterless events still need native-name/channel mapping; accepted Spin input
and lazily instantiated review/cash windows are not included in this binding.
The full original clips-array membership has not been recovered from the stripped
Loading MonoBehaviour; available exported clip names currently supply the catalog.
This change does not establish complete audio or game lifecycle parity.

Follow-up: the parameterless reel/coin/flight mappings listed above are now
implemented and validated in core-reel-audio.md. Other stated limitations remain.

Validation: Unity 2022.3.62f3 with graphics enabled, Artifacts/core-audio.xml,
5/5 passed (21.3633249 seconds), process 7520 exited. The sound-manager test
checks real AudioSource playing state, isolated second-channel stop, shared mute
gate, lazy loading, pause/play, remembered muted BGM and unknown-name behavior.
The production core test now asserts normalBg -> freeBg -> normalBg requests;
the existing four-game, repeated-Free and GM teardown scene tests also pass with
the real audio consumer installed. This verifies engine playback state, not
speaker output, waveform identity or every source timing/interleaving.
