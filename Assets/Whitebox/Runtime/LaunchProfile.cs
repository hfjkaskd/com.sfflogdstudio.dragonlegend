using UnityEngine;

namespace DragonLegend.Whitebox
{
    [CreateAssetMenu(menuName = "Dragon Legend/Launch Profile")]
    public sealed class LaunchProfile : ScriptableObject
    {
        public string profileId;
        public string countryCode;
        public string snapshotPath;
        public bool cashPresentationEnabled;
        public bool advertisementPresentationEnabled;
        [TextArea] public string evidenceNote;
    }
}
