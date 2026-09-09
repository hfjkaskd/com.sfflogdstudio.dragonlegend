using DragonLegend.Whitebox;

// Integration fixtures traverse the production prompt via its actual Button.
internal static class RecoveredCorePromptDriver
{
    internal static void ClaimAndClose(RecoveredCoreRoundFlow core)
    {
        if(!core.IsCashPromptPending)return;
        core.CashPrompt.ClaimButton.onClick.Invoke();
        core.CashOut.BackButton.onClick.Invoke();
    }
}
