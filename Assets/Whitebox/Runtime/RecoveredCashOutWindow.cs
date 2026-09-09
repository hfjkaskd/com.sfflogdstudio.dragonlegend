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
        public void Bind(RecoveredGameplayRules config,RecoveredPlayerProgress progress,int currencyLanguage,Canvas main,Func<int> utcClock)
        {
            Unbind();rules=config;player=progress;language=currencyLanguage;clock=utcClock;
            GetComponent<Canvas>().worldCamera=main.worldCamera;
            adapt.BindUiRootScaler(main.GetComponentInParent<CanvasScaler>());adapt.AdaptScreen();
            mode.Bind(rules,player,language);
            mode.SoundRequested+=Sound;header.SoundRequested+=Sound;list.SoundRequested+=Sound;bottom.SoundRequested+=Sound;bound=true;
        }
        public void Show()
        {
            if(gameObject.activeSelf)return;
            header.PrepareCashShow();
            if(!initialized){list.Initialize(rules,player,bottom,clock,listParent.rect.size,1,language);initialized=true;}
            else list.SetPaymentTypeForRefresh(1);
            mode.ResetForShow();header.ResetPaymentFrame();entrance.Play(()=>!mode.IsGift);
            gameObject.SetActive(true);content.localScale=Vector3.one*fromScale;elapsed=0;phase=1;
        }
        private void Sound(string name)=>SoundRequested?.Invoke(name);
        private void Close()
        {
            Sound("click");content.localScale=Vector3.one*toScale;elapsed=0;phase=2;
        }
        private void Update()
        {
            if(phase==0)return;elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/duration);
            content.localScale=Vector3.one*(phase==1?Mathf.LerpUnclamped(fromScale,toScale,enterEase.Evaluate(t)):Mathf.LerpUnclamped(toScale,fromScale,exitEase.Evaluate(t)));
            if(t<1)return;bool closing=phase==2;phase=0;if(closing)gameObject.SetActive(false);
        }
        public void Cancel(){phase=0;entrance.Cancel();list.CancelTimers();gameObject.SetActive(false);}
        public void Unbind()
        {
            Cancel();if(!bound)return;
            mode.SoundRequested-=Sound;header.SoundRequested-=Sound;list.SoundRequested-=Sound;bottom.SoundRequested-=Sound;bound=false;
        }
        private void OnDestroy()=>Unbind();
    }
}
