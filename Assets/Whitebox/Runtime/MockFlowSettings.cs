using UnityEngine;

namespace DragonLegend.Whitebox
{
    [CreateAssetMenu(menuName = "Whitebox/Mock Flow Settings")]
    public sealed class MockFlowSettings : ScriptableObject
    {
        public string placement;
        public string scene;
        public string requestId;
        public long amount;
        public AdOutcome adOutcome;
        public CashOutcome cashOutcome;
    }
}
