using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredTreasureWindow : MonoBehaviour,IRecoveredTreasureClaimView
    {
        [Serializable] private sealed class Icon {public int id;public string path;}
        [SerializeField] private RectTransform content, title, light, confetti;
        [SerializeField] private Transform endPosition;
        [SerializeField] private GameObject blackBackground;
        [SerializeField] private RecoveredTreasureCard card;
        [SerializeField] private RecoveredCollectTip collectTip;
        [SerializeField] private TMP_Text claimText,plainText;
        [SerializeField] private Text rewardText;
        [SerializeField] private Image cardImage;
        [SerializeField] private Button claimButton,plainButton;
        [SerializeField] private Icon[] icons;
        [SerializeField] private string claimFormat,claimPlainFormat,plainFormat;
        [SerializeField] private float countDuration,exitDuration,fromScale,toScale;
        [SerializeField] private Vector3 resetFlightScale;
        [SerializeField] private AnimationCurve countEase,exitEase;
        private RecoveredPlayerProgress player;
        private RecoveredGameplayRules rules;
        private RecoveredCashFlightPresenter cash;
        private Transform main;
        private bool isA;
        private int language;
        public RecoveredTreasureClaim Claim {get;private set;}
        public bool IsRunning {get;private set;}
        public RecoveredTreasureCard Card=>card;
        public RecoveredCollectTip CollectTip=>collectTip;
        public RectTransform Content=>content;
        public Transform EndPosition=>endPosition;
        public Image CardImage=>cardImage;
        public Text RewardText=>rewardText;
        public TMP_Text ClaimText=>claimText;
        public TMP_Text PlainText=>plainText;
        public Button ClaimButton=>claimButton;
        public Button PlainButton=>plainButton;
        public event Action<string> SoundRequested;
        public event Action<int,int> MainAnimationRequested;
        public event Action<Vector3,Sprite> CollectCardDepartureRequested;
        private void Awake()
        {
            claimButton.onClick.AddListener(ClickClaim);plainButton.onClick.AddListener(ClickPlain);
            card.OnFlipComplete+=FlipCompleted;card.SoundRequested+=PlaySound;
        }
        private void ClickClaim()=>Claim.OnClickButton("ClaimBtn");
        private void ClickPlain()=>Claim.OnClickButton("UnPlayBtn");
        public void Bind(RecoveredPlayerProgress progress,RecoveredGameplayRules gameplayRules,IAdFacade ads,
            RecoveredCashFlightPresenter flight,Transform mainWindow,bool profileA,int languageType)
        {
            player=progress;rules=gameplayRules;cash=flight;main=mainWindow;isA=profileA;language=languageType;
            Claim=new RecoveredTreasureClaim(ads,this);
            GetComponent<Canvas>().worldCamera=main.GetComponent<Canvas>().worldCamera;
        }
        public void Show(int id,Action<float> completed,Action<Transform> flyCard)
        {
            IsRunning=true;gameObject.SetActive(true);PlaySound("jump");content.localScale=Vector3.one;
            card.Init(id);collectTip.gameObject.SetActive(false);title.gameObject.SetActive(false);
            light.gameObject.SetActive(false);confetti.gameObject.SetActive(false);
            RecoveredCollectInfo info=null;var records=rules.GetCollectInfos();
            for(int i=0;i<records.Count;i++)if(records[i].id==id){info=records[i];break;}
            Claim.BeforeShow(info,rules,value=>{IsRunning=false;completed?.Invoke(value);});
            claimText.text=Claim.AdvertisedMultiplier>1?string.Format(claimFormat,Claim.AdvertisedMultiplier):claimPlainFormat;
            plainText.text=string.Format(plainFormat,RecoveredCurrency.Format((float)info.worth*Claim.UnadvertisedMultiplier,language,2));
            rewardText.text=RecoveredCurrency.Format(info.worth,language,2);
            Sprite sprite=null;
            for(int i=0;i<icons.Length;i++)if(icons[i].id==id){sprite=Resources.Load<Sprite>(icons[i].path);break;}
            cardImage.sprite=sprite;cardImage.SetNativeSize();
            MainAnimationRequested?.Invoke(4,1);
            card.gameObject.SetActive(false);blackBackground.SetActive(false);
            // EnterAnimation completes immediately; OnAfterShow resets this local target.
            endPosition.localPosition=Vector3.zero;flyCard?.Invoke(endPosition);
        }
        public void RefreshCollectCard(GameObject arrivingCard)
        {
            arrivingCard.transform.localPosition=Vector3.zero;
            arrivingCard.transform.localScale=resetFlightScale;arrivingCard.SetActive(false);
            card.gameObject.SetActive(true);blackBackground.SetActive(true);card.Flip();
        }
        private void FlipCompleted(RecoveredTreasureCard value)
        {
            title.gameObject.SetActive(true);light.gameObject.SetActive(true);confetti.gameObject.SetActive(true);
            collectTip.Initialize(player,rules,isA,language);
        }
        public void PlaySound(string name)=>SoundRequested?.Invoke(name);
        public void CountReward(Func<float> readOriginal,float to,Action completed)
            =>RecoveredTreasureCardRunner.Run(Count(readOriginal,to,completed));
        private IEnumerator Count(Func<float> readOriginal,float to,Action completed)
        {
            float from=readOriginal(),elapsed=0;
            while(this!=null){elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/countDuration);
                rewardText.text=RecoveredCurrency.Format(Mathf.LerpUnclamped(from,to,countEase.Evaluate(t)),language,2);
                if(t>=1){completed();yield break;}yield return null;}
        }
        public void HideTreasure()
        {
            content.localScale=Vector3.one*toScale;
            RecoveredTreasureCardRunner.Run(Exit());
        }
        private IEnumerator Exit()
        {
            float elapsed=0;
            while(this!=null){elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/exitDuration);
                content.localScale=Vector3.one*Mathf.LerpUnclamped(toScale,fromScale,exitEase.Evaluate(t));
                if(t>=1){gameObject.SetActive(false);Claim.AfterHide();yield break;}yield return null;}
        }
        public void FlyCoin(float amount,Action completed)=>cash.Begin(amount,completed,main,true);
        public void FlyCollectCard()=>CollectCardDepartureRequested?.Invoke(endPosition.position,cardImage.sprite);
    }
}
