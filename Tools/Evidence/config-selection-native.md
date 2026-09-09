# Native config input selection

Review baseline c839ec4d59d6c40115a4abca96a31c5c39fbb35f. This audit reads
ConfigManager.LoadConfig RVA 2369c20 in the supplied native ARM and resolves
its GOT references against the supplied ELF and IL2CPP metadata using
Tools/audit_main_click_references.py. config-load-references.json records
20 resolved references and ELF/decompiled-source SHA256 hashes.

At ARM 2369d58..2369d64, GameData +10 (IsA) conditionally replaces the incoming
string with GOT 4f1b988. The metadata resolves that string as empty. The next
IsNullOrEmpty branch therefore enters local-resource selection for A even
when a nonempty remote string was supplied. Ordinary mode retains a nonempty
input string and proceeds to decoding.

For empty input, base resource name is GoldenDragon (0501b968) or
GoldenDragonA (0501b970). The isFail=false branch reads PublishSDK.IsBackup;
isFail=true reads GameDataManager.GetIsBackUp. A true backup condition selects
_default (0501b990), otherwise _organic (0501b978). The combined name is loaded
as TextAsset through ResManager, then its text goes through Base64, UTF8,
XORGoldenDragon.Decode and configuration deserialization.

This method does not itself map an ISO country code to a snapshot. SDK-related
backup decisions remain within the user's SDK exception; no speculative
server routing or new network behavior was added.

## Current reconstruction difference

ConfigSnapshotLoader loads the explicitly selected decoded StreamingAssets
snapshot. A_Test deliberately selects cp_default with IsA=true and already
labels itself as a local comparison. This is not the native A local-resource
selection above. Adding cp_test to GM exposes a supplied snapshot but does
not close this A-mode source-selection difference.

The current extracted material includes GoldenDragon_default and
GoldenDragon_organic assets/decoded configurations, plus GoldenDragon.json
from the APK asset. The reconstruction/mumu-current filename inventory did
not yield an exact GoldenDragonA_default or GoldenDragonA_organic asset.
Names containing GoldenDragonAutoGenConfig are schema files, not those data
assets. This inventory is not proof that the original package could never
obtain A resources through another bundle or service.

Next actionable config work is to verify and expose the supplied bundled
default/organic data for local core testing, including default-mode recovery
and limits, while preserving US_Default as the startup selection. Actual A
source recovery and original country assignment remain separate unproven
requirements. Native LanguageType confirms EN=0 and BR=1; the enum alone
does not establish country-to-language or country-to-AB rules.

No runtime, SDK or prefab changes and no new Unity tests in this audit.
