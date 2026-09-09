# BR presentation comparison through GM

Baseline 6b9a387cce8c926ae3b3c8e8316ef4742f674d49.
Native LanguageType (dump TypeDefIndex 8682) defines EN=0 and BR=1; the current
LaunchProfile field already supports both, but all five authored choices used
EN. BuildGmTestProfile now adds BR_Comparison and its standard SelectBR Button.
The profile explicitly pairs BR language with the existing cp_default_1
snapshot and non-A presentation. This is a local comparison combination,
not recovered server assignment for Brazil or a complete Portuguese translation.

GM defaults remain US_Default. The new Button uses the same serialized binding,
hidden CanvasGroup and GameEntry.Select lifecycle as the other local profiles.
No SDK, cash/ad facade, currency formatter or gameplay rules were changed.

Author process 37416 exited successfully. The expanded GM test checks the sixth
control initially hidden, BR click access and collapse, preserved internal cash
1234.5, language 1, actual balance text R$12,35, a real paid Spin, then switching
back while busy and seeing the English balance $12.35 with language 0.

Initial process 27608 failed because the new test expected raw-number grouping
instead of the source currency conversion. RecoveredCurrency.Format divides
the internal amount by 100 and prefixes the currency symbol; the existing
implementation was retained. Corrected process 440 exited: 1/1 passed in
1.7987211 seconds (Artifacts/gm-br-final-tests.xml). This is a targeted expanded
GM run, not a new full-suite or original-APK visual comparison.
