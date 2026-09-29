using System;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // Local UICashOutView.OnBeforeShow tail 23a9168..23a91dc plus PopupWindow animation.
    public sealed class RecoveredCashOutWindow : MonoBehaviour
    {
        [SerializeField] private RectTransform content,listParent;
        [SerializeField] private RecoveredScreenAdapt adapt;
        [SerializeField] private RecoveredCashOutModeView mode;
        [SerializeField] private RecoveredCashOutPaymentHeader header;
        [SerializeField] private RecoveredCashOutList list;
        [SerializeField] private RecoveredCashOutBottom bottom;
        [SerializeField] private RecoveredCashOutEntrance entrance;
        [SerializeField] private Button backButton;
        [SerializeField] private RecoveredAccountWindow accountPrefab;
        [SerializeField] private RecoveredTipsWindow tipsPrefab;
        [SerializeField] private string pendingMessage,failedMessage,giftUnavailableMessage;
        private RecoveredAccountWindow accountWindow;
        private RecoveredTipsWindow tipsWindow;
        private RecoveredCashOutSubmission submission;
        [SerializeField] private float fromScale,toScale,duration;
        [SerializeField] private AnimationCurve enterEase,exitEase;
        private RecoveredGameplayRules rules;private RecoveredPlayerProgress player;private Func<int> clock;
        private int language,phase;private float elapsed;private bool bound,initialized;
        public RecoveredCashOutList List=>list;
        public RecoveredCashOutModeView Mode=>mode;
        public Button BackButton=>backButton;
        public RectTransform Content=>content;
        public event Action<string> SoundRequested;
        private void Awake()=>backButton.onClick.AddListener(Close);
        public void Bind(RecoveredGameplayRules config,RecoveredPlayerProgress progress,int currencyLanguage,Canvas main,Func<int> utcClock,ICashFacade cashFacade=null)
        {
            Unbind();rules=config;player=progress;language=currencyLanguage;clock=utcClock;
            player.RestoreLegacyCashOutReviews();
            GetComponent<Canvas>().worldCamera=main.worldCamera;
            adapt.BindUiRootScaler(main.GetComponentInParent<CanvasScaler>());adapt.AdaptScreen();
            mode.Bind(rules,player,language);
            submission=new RecoveredCashOutSubmission(rules,player,cashFacade??new LocalCashFacade());
            header.CashAccountRequested+=EditAccount;header.GiftAccountRequested+=GiftAccount;
            bottom.AccountRequested+=RequestCash;bottom.TipRequested+=Tip;bottom.MissingCashRequested+=Tip;
            bottom.TaskContinuationRequested+=ContinueTask;
            mode.SoundRequested+=Sound;header.SoundRequested+=Sound;list.SoundRequested+=Sound;bottom.SoundRequested+=Sound;bound=true;
        }
        public void Show()
        {
            if(gameObject.activeSelf)return;
            // ScrollRect must not clamp its content against a zero-scale entrance viewport.
            list.Scroll.enabled=false;
            header.PrepareCashShow();
            if(!initialized){list.Initialize(rules,player,bottom,clock,listParent.rect.size,1,language);initialized=true;}
            else list.SetPaymentTypeForRefresh(1);
            mode.ResetForShow();header.ResetPaymentFrame();entrance.Play(()=>!mode.IsGift);
            gameObject.SetActive(true);content.localScale=Vector3.one*fromScale;elapsed=0;phase=1;
        }
        private void Sound(string name)=>SoundRequested?.Invoke(name);
        private void Tip(string message)
        {
            if(tipsWindow==null){tipsWindow=Instantiate(tipsPrefab,transform,false);tipsWindow.Bind(GetComponent<Canvas>().worldCamera);}
            tipsWindow.Show(message);
        }
        private void GiftAccount()=>Tip(giftUnavailableMessage);
        private void EditAccount(int type)=>OpenAccount(type,-1);
        private void RequestCash(int type,int tier)=>OpenAccount(type,tier);
        private void OpenAccount(int type,int tier)
        {
            if(accountWindow==null){accountWindow=Instantiate(accountPrefab,transform,false);accountWindow.SoundRequested+=Sound;}
            accountWindow.Show(type,player,GetComponent<Canvas>().worldCamera,(name,email)=>
            {
                try
                {
                    if(tier<0)player.SaveCashAccount(type,name,email);
                    else submission.Submit(tier,type,name,email,clock());
                    header.RefreshAccount(type);list.RefreshData();
                    if(tier>=0)list.FocusTier(tier);
                    return true;
                }
                catch(Exception error){Tip(failedMessage);Debug.LogException(error,this);return false;}
            });
        }
        private void ContinueTask(DragonLegend.Whitebox.Recovered.PlayerCashOutData record,int next)
        {
            // Cached UI flags can become stale; never advance a record that no longer qualifies.
            var state=player.GetCashOutConditions(record.id,clock());
            if(!state.TaskComplete||!state.WaitComplete){list.RefreshData();return;}
            if(next==1000){RequestCash(record.type,record.id);return;}
            player.ApplyCashOutTaskStep(record,next,clock());list.RefreshData();
        }
        private void Close()
        {
            list.Scroll.enabled=false;
            Sound("click");accountWindow?.Cancel();tipsWindow?.Cancel();content.localScale=Vector3.one*toScale;elapsed=0;phase=2;
        }
        private void Update()
        {
            if(phase==0)return;elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/duration);
            content.localScale=Vector3.one*(phase==1?Mathf.LerpUnclamped(fromScale,toScale,enterEase.Evaluate(t)):Mathf.LerpUnclamped(toScale,fromScale,exitEase.Evaluate(t)));
            if(t<1)return;bool closing=phase==2;phase=0;if(closing)gameObject.SetActive(false);else list.Scroll.enabled=true;
        }
        public void Cancel(){phase=0;accountWindow?.Cancel();tipsWindow?.Cancel();entrance.Cancel();list.CancelTimers();gameObject.SetActive(false);}
        public void Unbind()
        {
            Cancel();if(!bound)return;
            header.CashAccountRequested-=EditAccount;header.GiftAccountRequested-=GiftAccount;
            bottom.AccountRequested-=RequestCash;bottom.TipRequested-=Tip;bottom.MissingCashRequested-=Tip;bottom.TaskContinuationRequested-=ContinueTask;
            mode.SoundRequested-=Sound;header.SoundRequested-=Sound;list.SoundRequested-=Sound;bottom.SoundRequested-=Sound;bound=false;
        }
        private void OnDestroy()=>Unbind();
    }
}
