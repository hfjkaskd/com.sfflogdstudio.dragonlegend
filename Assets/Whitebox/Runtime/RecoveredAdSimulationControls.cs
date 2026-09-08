using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // Local GM controls for the existing SDK facade. Never auto-grants a pending ad.
    public sealed class RecoveredAdSimulationControls : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button reward, fail;
        private LocalAdFacade ads;
        public Button RewardButton=>reward;
        public Button FailureButton=>fail;
        private void Awake(){reward.onClick.AddListener(Reward);fail.onClick.AddListener(Fail);}
        public void Bind(LocalAdFacade value)
        {
            ads=value;
            // Releasing a facade also runs while its parent hierarchy is inactive.
            // Camera assignment belongs to binding, not teardown.
            if(value!=null)GetComponent<Canvas>().worldCamera=transform.parent.GetComponentInParent<Canvas>().worldCamera;
            Refresh();
        }
        private void Reward(){ads?.Complete(AdOutcome.Rewarded);Refresh();}
        private void Fail(){ads?.Complete(AdOutcome.Failed);Refresh();}
        private void Update()=>Refresh();
        private void Refresh(){bool visible=ads!=null&&ads.Pending;if(panel.activeSelf!=visible)panel.SetActive(visible);}
    }
}
