# Cash / Gift tab presentation

Source evidence: UICashOutView.CheckLayOut 0x23a9858 toggles LayOut (+0xa8) only for config type default and cash mode. Bottom (+0xb0) and CashOutRect (+0x88) depend on cash mode regardless of config; GiftRect (+0x90) depends on gift mode. CheckTag 0x23a99e8 toggles child 2 of each tab, matching its numeric mode. OnClickButton 0x23a9efc..0x23a9fb0 plays click before checking the existing mode; repeated selected-tab clicks return without CheckAll. A change updates openType then CheckAll.

RecoveredCashOutModeView now owns these authored references and code-bound standard Button listeners in the existing CashOutWindow prefab. It refreshes the existing payment/account header, panel visibility and tab selection before requesting item refresh from the eventual window owner. ResetForShow selects cash mode without a click sound. No hierarchy, layout, sprites, colors or fonts are created at runtime.

BuildCashOutModeView.Save updates the existing prefab without recreating its children. BuildCashOutWindow.Save also attaches this component so subsequent full authoring retains the wiring.

This is an intermediate dependency for completing the main settlement-to-withdrawal branch. It is not a complete withdrawal window: the owning show/hide lifecycle, gift item population, account entry destinations, item-refresh event consumers and main CashPrompt route remain to be connected. Existing SDK handling is unchanged. No main-flow event is connected to a placeholder destination.

Validation exercises the actual prefab/tab Buttons, repeated clicks, all cash/gift panel and selection states, config-specific provider visibility, current account prompts, reset-to-cash, and unchanged PlayerData/no saves. Existing payment-header and entrance-animation tests are included in the focused run.

Result: Artifacts/cash-mode.xml passed 4/4; Unity test PID 32360 is terminal.
