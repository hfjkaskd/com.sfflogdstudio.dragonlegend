using UnityEditor;

namespace DragonLegend.Whitebox.EditorTools
{
    public static class FinalizeGameLocalization
    {
        [MenuItem("Dragon Legend/Localization/Prepare and Verify Assets")]
        public static void PrepareAndCapture()
        {
            PrepareLocalizationFonts.Prepare();
            InstallGameLocalization.Install();
            InstallPortugueseTextLayout.Install();
            ValidateGameLocalization.Capture();
        }
    }
}
