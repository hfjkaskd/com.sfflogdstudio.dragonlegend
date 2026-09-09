# Treasure collection data

## Native evidence

Source: `C:/Projects/Nut Sort Relax/reconstruction/mumu-current/native-functions/game/` and matching raw ARM64 disassembly in `native/game/`.

- `236c644.c`, ConfigManager.GetCollectInfos: assign cache before constructing records; read Qollgqr columns Ip, Lgtgl, Ronpom and Korrt into id, level, random and worth, in ID list order. Repeated reads reuse records. A malformed column can leave an already-published partial cache. PlayerData.Init's count now goes through this actual cache path.
- `236c880.c`, RandomCollectIndex: pass Qollgqr.Ip to RandomListWeight and return Ip at the selected index. Ronpom is retained in CollectInfo but is not the weight input here. IDs are not list ordinals. Selection reads live configuration even after the info cache is populated.
- `236c8fc.c`, GetCollectClaim: signed configured integer converted to float and divided by 1000, without clamping.
- `native/game/0236c978.asm`, GetCollectReward: read QollgqrRgkorp[0], convert signed integer to float, clear w0 (decimal places), tail-branch to CurrencyUtils.FormatCurrency at 238c4e4. This API produces display text, not a numeric cash award.
- `236f48c.c`, GameData.SetCollectData: first matching ID only. Missing record is added with count=1 and isRecieve=false regardless of requested increment. Existing unreceived record adds signed int32 increment. Received record and null list still reach the single native save. No balance award or notification occurs here.

## Implementation and verification

RecoveredGameplayRules, RecoveredCollectInfo and RecoveredPlayerProgress retain these behaviors using the existing recovered config and PlayerData save callback. No SDK changes or parallel collection store.

RecoveredCollectTests covers cache identity and partial-cache failure, all record columns, 64 seeded ID-weight draws and subsequent random-stream state, live configuration changes, claim scaling and formatted reward, duplicate IDs, first-record initialization, negative and overflowing increments, received/null-list saves, and actual PlayerData JSON round-trip.

Full Unity 2022.3.62f3 PlayMode regression: **323/323 passed**, `Artifacts/collect-tests.xml` (current run). No visual change was made in this data-layer increment.

## Next presentation evidence

Raw `023dbe7c.asm` is authoritative for RefreshCollectCard; its decompilation incorrectly includes neighboring/inlined paths after a type-failure branch. The real method takes a GameObject from event arguments, resets its local position, restores scale (.4,.4,.4), hides it, activates CardTreasure and BlackBg, then tail-calls TreasureCard.Flip(null). Thus the flight-completion event triggers the flip, not OnAfterShow alone. The third scale component at ELF dbc6e8 was verified as .4 during window restoration.

`023db960.asm` OnClickCard checks the flipped flag and otherwise tail-calls Flip(null). `023dbc34.asm` OnDestroy kills transform tweens without completing them. These method bodies do not establish a prefab click binding or that a containing sequence is killed; those details still need checking.

## Remaining integration

TreasureCard and UITreasureView presentation, collection tip and Treasure cash claim are now recovered; see `treasure-window.md`. Main card flight/departure, the full collection redemption window, and the production Free loop still need implementation/connection. These data rules alone do not establish that the Treasure branch or full lifecycle matches the original.
