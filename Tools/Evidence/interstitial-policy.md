# Original interstitial policy and local transport

Verified ARM64 source: SdkAdManager.PlayInterAd 2379904, ConfigManager.CheckInsertAd
236aff0, InsertCD 236b1c4, and success callback 237a198.

Order: IsA returns immediately; reject Level <= Qonrii.Lgtgl[0]; otherwise use
InggrrRonpom[Level-2] below the last Lgtgl entry, or the last probability entry
at/above it. Unity Random.Range(0,1000) <= probability is inclusive. This random
draw occurs before cooldown checks. First eligible request initializes the
timestamp to local DateTime epoch seconds minus InggrrQP[0]. A request is allowed
when timestamp + cooldown <= now. Timestamp changes to current local epoch
seconds only on the successful SDK callback. It is session state, not PlayerPrefs.

The callback also dispatches event "7" with [1,1]. ELF relocation 4f1c520 resolves
to stringliteral 504b238 ("7"), identified in dump.cs as RefreshCashOutTask.
The main binding therefore advances step-1 cash tasks once per successful
interstitial callback. Failure does not advance tasks or reset cooldown.

Native call-site scan identifies seven callers: bank 2391e78, big win 239519c,
task reward 23ad164, jackpot 23b32a8, network error 23d6450, reward/lucky 23d8470,
treasure 23dc94c. All use iv_close; task/network scenes are task/networkError
(relocations 4f1dc78 / 4f1e9a0). Five existing gameplay callers remain connected;
the task-reward and network-error windows are not implemented in this runtime,
so no replacement trigger was invented for them. Free-start plain close has no
native interstitial call. Existing callers continue closing/settling immediately.

LocalAdFacade is still a simulation. GameEntry now supplies the recovered policy;
standalone facade tests may omit it as a transport-only fixture. Eligible requests
emit an interstitial event, show the configured toast and use the existing authored
simulation duration. Interstitial completion is separate from rewarded completion.
The local transport suppresses overlapping interstitial requests; this is a mock
transport constraint, not an added original eligibility rule. Real SDK delivery,
tracking and error callbacks are not claimed as restored third-party internals.

Validation: Artifacts/interstitial-verified.xml, 48/48 PlayMode tests passed.
Coverage includes native level/probability selection and RNG consumption, the
first request, shared cooldown boundary, failure/retry, successful step-1 task
increment/save, isolated rewarded callbacks, and real bank-window close/settlement
with an ineligible level-1 player. Generated prefab changes are limited to the
two interstitial notification strings; source and delivered files match the
isolated validation project.
