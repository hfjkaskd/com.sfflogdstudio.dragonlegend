# Actual default-profile load and elapsed Spin recovery

Baseline c62b9bb770fe0c148556632be1bbca420849fc6d. The existing unit evidence
in spin-recovery.md covers the native arithmetic and save order. This case
adds the normal GameEntry loader/presenter integration with the unchanged
bundled GoldenDragon_default snapshot.

RecoveredOfflineSpinLoadTests loads the production scene, selects the authored
bundled-default Button callback and reads its real maximum/cooldown (30/30).
It prepares a saved record at max-3 with LastSpinTime two intervals plus five
seconds in the past, then reselects through the same loader. Assertions check
a new progress instance, max-1 count, LastSpinTime advanced by exactly two
intervals, an active remainder countdown and both recovered fields persisted.
A second immediate load must not grant those intervals again, must preserve
the save and must dispose the previous timer.

The test then prepares the max-1 saved record with the same two-interval age
and reloads. Count clamps to the actual maximum, persists at that maximum,
and the new timer is stopped. This integration assertion does not replace
the more detailed native timestamp/save ordering assertions in the unit test.

Process 43236 exited with the initial case passing 1/1 in 1.3786943 seconds.
After the cap assertions, process 44704 exited: offline-default-cap-tests.xml
passed 1/1 in 1.500141 seconds. No production or SDK changes were required.

This fixture prepares elapsed time in the save; it does not wait offline,
change the system clock, simulate an Android process kill or exercise every
clock-boundary/corruption case. Existing PlayerPrefs and RNG restore afterward.
