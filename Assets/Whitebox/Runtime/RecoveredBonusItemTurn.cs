using System;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // BonusItem.Init and the opening portion of PlayBonusAnim (2396d54).
    // TurnReady hands off to the payout/flight stage; it is NOT the window's
    // input-release or completion callback.
    public sealed class RecoveredBonusItemTurn : MonoBehaviour
    {
        [SerializeField] private RecoveredRegionAnimator body,glow;
        [SerializeField] private Text rewardText;
        [SerializeField] private Image ad;
        [SerializeField] private Button button;
        [SerializeField] private int concealedClip=12,glowClip;
        [SerializeField] private int[] turnClips={8,10,7,9,6},holdClips={3,5,2,4,1};
        [SerializeField] private float turnSpeed=3,rewardDelay=.2f,scaleDuration=.2f,scalePeak=1.2f;
        [SerializeField] private AnimationCurve scaleEase;
        private RecoveredReelWait wait;
        private Action turnReady;
        private bool turned;
        private int scalePhase,generation;
        private float elapsed;
        private Vector3 scaleFrom;
        public int Reward {get;private set;}
        public bool RewardJump {get;private set;}
        public bool IsTurning {get;private set;}
        public RecoveredRegionAnimator Body=>body;
        public RecoveredRegionAnimator Glow=>glow;
        public Text RewardText=>rewardText;
        public Button Button=>button;
        public Image Ad=>ad;
        public event Action Selected;
        public event Action<Exception> Failed;
        private void Awake()=>button.onClick.AddListener(OnSelected);
        private void OnSelected()=>Selected?.Invoke();

        public void Initialize()
        {
            Cancel();
            glow.gameObject.SetActive(false);rewardText.gameObject.SetActive(false);ad.gameObject.SetActive(false);
            body.Play(concealedClip);button.enabled=true;
        }
        public void ShowAd(bool value)=>ad.gameObject.SetActive(value);
        public void Begin(RecoveredBonusType type,RecoveredGameplayRules rules,int language,Action ready)
        {
            int index=(int)type;
            if(index<0||index>=turnClips.Length)throw new ArgumentOutOfRangeException(nameof(type));
            if(rules==null)throw new ArgumentNullException(nameof(rules));
            Cancel();int current=generation;turned=false;IsTurning=true;turnReady=ready;
            Reward=0;RewardJump=false;body.PlaybackSpeed=turnSpeed;
            body.PlayOnce(turnClips[index],()=>{body.Play(holdClips[index]);turned=true;});
            if(type==RecoveredBonusType.Reward) {
                wait=RecoveredReelWait.Delay(rewardDelay,()=>{
                    rewardText.gameObject.SetActive(true);
                    var value=rules.GetBonusReward();Reward=value.amount;RewardJump=value.jump;
                    rewardText.text=RecoveredCurrency.Format(Reward,language,2);
                    scaleFrom=rewardText.transform.localScale;elapsed=0;scalePhase=1;
                    WaitForTurn(current);
                },Fail);
            } else WaitForTurn(current);
        }
        private void WaitForTurn(int current)
        {
            wait=RecoveredReelWait.Until(()=>turned,()=>{
                if(current!=generation)return;
                wait=null;body.PlaybackSpeed=1;IsTurning=false;
                glow.gameObject.SetActive(true);glow.PlayOnce(glowClip,()=>glow.gameObject.SetActive(false));
                var callback=turnReady;turnReady=null;callback?.Invoke();
            },Fail);
        }
        private void Update()
        {
            if(scalePhase==0)return;
            elapsed+=Time.deltaTime;float fraction=Mathf.Clamp01(elapsed/scaleDuration);
            var target=Vector3.one*(scalePhase==1?scalePeak:1);
            rewardText.transform.localScale=Vector3.LerpUnclamped(scaleFrom,target,scaleEase.Evaluate(fraction));
            if(fraction<1)return;
            if(scalePhase==1){scalePhase=2;elapsed=0;scaleFrom=rewardText.transform.localScale;}
            else scalePhase=0;
        }
        private void Fail(Exception error){Cancel();Failed?.Invoke(error);}
        public void Cancel()
        {
            generation++;wait?.Cancel();wait=null;turnReady=null;IsTurning=false;scalePhase=0;
            if(body!=null)body.Stop();if(glow!=null)glow.Stop();
        }
        private void OnDisable()=>Cancel();
    }
}
