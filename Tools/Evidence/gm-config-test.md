# GM access to the existing cp_test snapshot

Baseline 033e75fa209000441d087cce82fd0716a00c56a2 exposed two GM profiles,
although StreamingAssets already contained cp_default, cp_default_1 and cp_test.
The third snapshot was not selectable through the authored GM interface.

US_ConfigTest now explicitly selects RecoveredConfig/Remote/cp_test.json with
US, EN and non-A local presentation. This is a local testing combination,
not evidence of the original server's country/AB assignment. The snapshot is
unchanged. US_Default still starts the game using cp_default_1; SDK and ad/cash
facades are unchanged. Real country routing remains unverified.

BuildGmTestProfile authors a third standard Button below the existing alternate
button, a LaunchProfile asset and serialized RecoveredProfileButton binding.
The runtime binding only calls the existing GameEntry.Select lifecycle; it
does not generate UI or bypass teardown/loading. RecoveredGmPanel includes the
new Button and CanvasGroup so it stays hidden/non-interactive until GM opens
and closes with the other choices after selection. BuildGmPanel also reapplies
the third option when rebuilding the panel.

Author process 20940 exited successfully. Targeted process 47584 exited:
gm-test-profile-tests.xml passed 1/1 in 1.4892705 seconds. The test checks all
three hidden controls, actual raycast access, A/US/config-test switching,
replacement playfield identity, a real Spin under cp_test and switching back
to US_Default while that Spin is busy. This covers snapshot access, not every
profile's complete gameplay session or every country/language/AB combination.

Full PlayMode process 15796 exited: Artifacts/gm-test-profile-full-tests.xml
passed 465/465 in 112.3996136 seconds, including the current mixed full Free
sessions, collected Bonus trigger, scene routing and prior teardown cases.
