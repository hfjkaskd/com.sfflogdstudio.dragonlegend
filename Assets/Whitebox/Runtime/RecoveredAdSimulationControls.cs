using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // Local simulation only. Duration and automatic completion are authored in the prefab.
    public sealed class RecoveredAdSimulationControls : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button reward, fail;
        [SerializeField] private bool automaticCompletion;
        [SerializeField] private float simulatedDuration;
        [SerializeField] private RecoveredTipsWindow tipsPrefab;
        [SerializeField] private string startedText,completedText,failedText;
        [SerializeField] private string interstitialStartedText,interstitialCompletedText;
        private bool interstitialRunning;
        private float interstitialRemaining;
        private RecoveredTipsWindow tips;
        private float remaining;
        private bool running;
        private LocalAdFacade ads;
        public Button RewardButton=>reward;
        public Button FailureButton=>fail;
        private void Awake(){reward.onClick.AddListener(Reward);fail.onClick.AddListener(Fail);}
        public void Bind(LocalAdFacade value)
        {
            if(ads!=null){ads.RewardAdStarted-=Started;ads.RewardAdCompleted-=Completed;}
            if(ads!=null){ads.InterstitialStarted-=InterStarted;ads.InterstitialCompleted-=InterCompleted;}
            interstitialRunning=false;
            running=false;tips?.Cancel();
            ads=value;
            // Releasing a facade also runs while its parent hierarchy is inactive.
            // Camera assignment belongs to binding, not teardown.
            if(value!=null)GetComponent<Canvas>().worldCamera=transform.parent.GetComponentInParent<Canvas>().worldCamera;
            if(ads!=null){ads.RewardAdStarted+=Started;ads.RewardAdCompleted+=Completed;if(ads.Pending)Started();}
            if(ads!=null){ads.InterstitialStarted+=InterStarted;ads.InterstitialCompleted+=InterCompleted;if(ads.InterstitialPending)InterStarted();}
            Refresh();
        }
        private void ShowTip(string text)
        {
            if(tipsPrefab==null)return;
            if(tips==null){tips=Instantiate(tipsPrefab,transform,false);tips.Bind(GetComponent<Canvas>().worldCamera);}
            tips.Cancel();tips.Show(text);
        }
        private void Started(){remaining=simulatedDuration;running=automaticCompletion;Refresh();ShowTip(startedText);}
        private void Completed(AdOutcome outcome){running=false;Refresh();ShowTip(outcome==AdOutcome.Rewarded?completedText:failedText);}
        private void InterStarted(){interstitialRemaining=simulatedDuration;interstitialRunning=automaticCompletion;ShowTip(interstitialStartedText);}
        private void InterCompleted(bool shown){interstitialRunning=false;ShowTip(shown?interstitialCompletedText:failedText);}
        private void Reward(){ads?.Complete(AdOutcome.Rewarded);Refresh();}
        private void Fail(){ads?.Complete(AdOutcome.Failed);Refresh();}
        private void Update()
        {
            if(interstitialRunning)
            {
                interstitialRemaining-=Time.unscaledDeltaTime;
                if(interstitialRemaining<=0){interstitialRunning=false;ads?.CompleteInterstitial(true);}
            }
            if(!running)return;
            remaining-=Time.unscaledDeltaTime;
            if(remaining<=0){running=false;ads?.Complete(AdOutcome.Rewarded);}
        }
        private void Refresh(){if(panel==null)return;bool visible=ads!=null&&ads.Pending;if(panel.activeSelf!=visible)panel.SetActive(visible);}
        private void OnDestroy()=>Bind(null);
    }
}
