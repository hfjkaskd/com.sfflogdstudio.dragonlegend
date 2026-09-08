using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredBonusWindow:MonoBehaviour,IRecoveredBonusSelectionView
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private Canvas canvas;
        [SerializeField] private RecoveredBonusItemTurn[] cards;
        [SerializeField] private Transform[] grandTargets,majorTargets,minorTargets;
        [SerializeField] private RecoveredJackpotMeters meters;
        [SerializeField] private TMP_Text chances;
        [SerializeField] private string chanceFormat;
        [SerializeField] private Button closeButton;
        [SerializeField] private RecoveredBonusCharacterRewards characters;
        [SerializeField] private RecoveredBonusCashRewards cash;
        [SerializeField] private RecoveredBonusRewardPopup rewardPopup;
        [SerializeField] private RecoveredJackpotPopup jackpotPopup;
        [SerializeField] private float enterDuration=.3f,hintInterval=1.83f;
        [SerializeField] private AnimationCurve enterEase;
        private RecoveredBonusSelection selection;
        private RecoveredPlayerProgress progress;
        private RecoveredGameplayRules rules;
        private RecoveredCashFlightPresenter cashFlight;
        private IAdFacade ads;
        private int language;
        private bool isA,entering;
        private float elapsed;
        private RecoveredReelWait hint;
        public RecoveredBonusSelection Selection=>selection;
        public int CardCount=>cards.Length;
        public RecoveredBonusItemTurn Card(int index)=>cards[index];
        public TMP_Text Chances=>chances;
        public event Action CloseRequested,ExitRequested,HideFingerRequested;
        public event Action HideWheelRequested;
        public event Action<int,int> CashOutTaskRefreshRequested;
        public event Action<Transform> FingerRequested;
        public event Action<string> SoundRequested,Sound1Requested;
        public event Action PauseMusicRequested,ResumeMusicRequested,StopSound1Requested;
        public event Action<int> VibrationRequested;
        public event Action<Exception> Failed;
        private void Awake()
        {
            for(int i=0;i<cards.Length;i++){int index=i;cards[i].Selected+=()=>selection.Select(index);}
            closeButton.onClick.AddListener(()=>CloseRequested?.Invoke());
            characters.SoundRequested+=Sound;cash.SoundRequested+=Sound;rewardPopup.SoundRequested+=Sound;jackpotPopup.SoundRequested+=Sound;
            characters.VibrationRequested+=value=>VibrationRequested?.Invoke(value);
            characters.JackpotAnimationRequested+=type=>meters.At((int)type-1).PlayAnim(null);
            characters.JackpotPopupRequested+=(type,amount,completed)=>jackpotPopup.Show(type,amount,progress,rules,ads,isA,language,completed);
            cash.RewardPopupRequested+=(amount,completed)=>rewardPopup.Show(amount,progress,rules,ads,isA,language,completed);
            cash.FlyCoinRequested+=FlyCoin;rewardPopup.FlyCoinRequested+=(amount,completed)=>FlyCoin(amount,completed,null);
            jackpotPopup.FlyCoinRequested+=(amount,completed)=>FlyCoin(amount,completed,null);
            jackpotPopup.PauseMusicRequested+=()=>PauseMusicRequested?.Invoke();jackpotPopup.ResumeMusicRequested+=()=>ResumeMusicRequested?.Invoke();
            jackpotPopup.StopSound1Requested+=()=>StopSound1Requested?.Invoke();jackpotPopup.Sound1Requested+=value=>Sound1Requested?.Invoke(value);
            rewardPopup.HideWheelRequested+=()=>HideWheelRequested?.Invoke();jackpotPopup.HideWheelRequested+=()=>HideWheelRequested?.Invoke();
            jackpotPopup.CashOutTaskRefreshRequested+=(type,count)=>CashOutTaskRefreshRequested?.Invoke(type,count);
            characters.Failed+=Fail;cash.Failed+=Fail;
        }
        private void Sound(string sound)=>SoundRequested?.Invoke(sound);
        private void Fail(Exception error)=>Failed?.Invoke(error);
        private void FlyCoin(float amount,Action completed,Transform source)=>cashFlight.Begin(amount,completed,transform,false,source);
        public void Show(RecoveredPlayerProgress data,RecoveredGameplayRules config,IAdFacade adFacade,
            RecoveredCashFlightPresenter flight,Func<int> readBet,bool versionA,int languageType)
        {
            progress=data;rules=config;ads=adFacade;cashFlight=flight;language=languageType;isA=versionA;
            gameObject.SetActive(true);characters.Cancel();cash.Cancel();
            if(selection==null)selection=new RecoveredBonusSelection(rules,ads,this,cards.Length);
            meters.Initialize(progress,rules,readBet,language);
            ResetTargets(grandTargets);ResetTargets(majorTargets);ResetTargets(minorTargets);
            selection.BeforeShow();content.localScale=Vector3.zero;elapsed=0;entering=true;
        }
        private static void ResetTargets(Transform[] targets){foreach(var target in targets)target.GetChild(0).gameObject.SetActive(true);}
        private void Update()
        {
            if(!entering)return;elapsed+=Time.deltaTime;
            float t=Mathf.Clamp01(elapsed/enterDuration);content.localScale=Vector3.one*enterEase.Evaluate(t);
            if(t>=1){entering=false;selection.AfterShow();}
        }
        public void HideFinger()=>HideFingerRequested?.Invoke();
        public void CancelFingerSequence(){hint?.Cancel();hint=null;}
        public void InitializeCards(){foreach(var card in cards)card.Initialize();}
        public void SetCloseVisible(bool visible)=>closeButton.gameObject.SetActive(visible);
        public void SetCardEnabled(int index,bool enabled)=>cards[index].Button.enabled=enabled;
        public void ShowCardAd(int index,bool visible)=>cards[index].ShowAd(visible);
        public void SetChances(int selected,int free)=>chances.text=string.Format(chanceFormat,selected,free);
        public void PlayCard(int index,RecoveredBonusRound.Reveal reveal,Action releaseInput)
        {
            if(reveal.type==RecoveredBonusType.Reward){cash.Begin(cards[index],rules,language,releaseInput);return;}
            Transform target=null;
            if(reveal.targetIndex>=0){var targets=reveal.jackpot==RecoveredJackpotType.Grand?grandTargets:reveal.jackpot==RecoveredJackpotType.Major?majorTargets:minorTargets;target=targets[reveal.targetIndex];}
            characters.Begin(cards[index],reveal,target,progress,rules,language,canvas.sortingOrder,releaseInput);
        }
        public void ShowFinger()
        {
            if(selection.Round.ClickedCount==cards.Length)return;
            int index;do{index=UnityEngine.Random.Range(0,cards.Length);}while(selection.Round.WasClicked(index));
            CancelFingerSequence();
            hint=RecoveredReelWait.Delay(hintInterval,()=>{
                FingerRequested?.Invoke(cards[index].transform);
                hint=RecoveredReelWait.Delay(hintInterval,()=>{hint=null;HideFinger();ShowFinger();},Fail);
            },Fail);
        }
        // Coordinator must execute the original close/exit transition and complete
        // the main-flow source; a selection alone must not deactivate this window.
        public void HideBonus()=>ExitRequested?.Invoke();
        private void OnDisable(){entering=false;CancelFingerSequence();HideFinger();}
    }
}
