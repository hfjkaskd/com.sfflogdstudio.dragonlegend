using UnityEngine;

namespace DragonLegend.Whitebox
{
    [CreateAssetMenu(menuName = "Dragon Legend/Launch Profile")]
    public sealed class LaunchProfile : ScriptableObject
    {
        public string profileId;
        public string countryCode;
        public int languageType; // Original LanguageType: EN=0, BR=1; explicit GM profile setting.
        public bool isA; // Explicit local GM selection, not inferred server routing.
        public string snapshotPath;
        public bool cashPresentationEnabled;
        public bool advertisementPresentationEnabled;
        [TextArea] public string evidenceNote;
    }
}
