# Bundled config verification and GM selection

Baseline 76b16ef096de7ef90c591852ac8ee6416847a3f6.
Both decoded bundled snapshots already existed in StreamingAssets, but neither
had a GM Button/profile. This change exposes them without changing US_Default.

Tools/import_bundled_configs.py decodes both supplied original TextAssets using
Base64 -> repeating GoldenDragon XOR -> Base64 -> UTF8 JSON, verifies equality
against extracted JSON and the existing project snapshots, preserves existing
snapshot formatting, and retains encoded source copies. The evidence JSON
records source, encoded, decoded, reference and current snapshot hashes. The
first import rewrote JSON whitespace; that formatting-only change was reverted
before commit, and the verifier now preserves existing equivalent snapshots.

BuildGmTestProfile now authors cp_test, bundled default and bundled organic
Buttons and profiles as one list. All use the existing RecoveredProfileButton
and GameEntry.Select teardown/load flow. Alongside US_Default and A_Test, GM
now has five choices. These local US/EN/non-A combinations do not imply the
original server selected them for US; config-selection-native.md records the
native input/backup branches and outstanding A-resource gap.

Author process 47796 exited successfully. PlayMode process 50344 exited:
Artifacts/bundled-gm-tests.xml passed 1/1 in 1.6621035 seconds. The expanded GM
fixture checks all five controls initially hidden, actual pointer routing,
each bundled path/type, a real Spin after preparing the count at its configured
maximum, default-mode countdown start and configured duration, organic-mode
no countdown, and disposal when switching to US_Default during the busy Spin.

The fixture prepares counts explicitly; it does not claim to verify waiting
through a complete real-time recovery period or all profiles' reward outcomes.
No SDK routing, ad/cash facade, configuration values or gameplay logic changed.
The last full 465-case run belongs to gm-config-test.md; this turn runs the
targeted expanded GM fixture only.
