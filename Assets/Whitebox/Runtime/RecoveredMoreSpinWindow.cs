using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredMoreSpinWindow : MonoBehaviour,IRecoveredMoreSpinView
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private TMP_Text amount;
        [SerializeField] private Button claimButton,closeButton;
        [SerializeField] private float duration;
        [SerializeField] private AnimationCurve enterEase,exitEase;
        private RecoveredMoreSpinClaim claim;
        private int phase;
        private float elapsed;
        public Button ClaimButton=>claimButton;
        public Button CloseButton=>closeButton;
        public TMP_Text Amount=>amount;
        public bool IsClicked=>claim!=null&&claim.IsClicked;
        public event Action<string> SoundRequested;
        public event Action LimitTipRequested;
        private void Awake(){claimButton.onClick.AddListener(Claim);closeButton.onClick.AddListener(Close);}
        public void Bind(GameEntry game)
        {
            Cancel();claim=new RecoveredMoreSpinClaim(game.Rules,game.PlayerProgress,game.PlayerStore.Data,game.Ads,this);
            GetComponent<Canvas>().worldCamera=game.GetComponent<Canvas>().worldCamera;
        }
        public void Show()
        {
            if(gameObject.activeSelf)return;
            gameObject.SetActive(true);amount.text=string.Format("+{0}",claim.BeforeShow());
            content.localScale=Vector3.zero;elapsed=0;phase=1;
        }
        private void Claim()=>claim.Click("ClaimBtn");
        private void Close()=>claim.Click("CloseBtn");
        public void PlaySound(string sound)=>SoundRequested?.Invoke(sound);
        public void ShowLimitTip()=>LimitTipRequested?.Invoke();
        public void Hide(){content.localScale=Vector3.one;elapsed=0;phase=2;}
        private void Update()
        {
            if(phase==0)return;
            elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/duration);
            content.localScale=Vector3.one*(phase==2?1-exitEase.Evaluate(t):enterEase.Evaluate(t));
            if(t<1)return;bool closing=phase==2;phase=0;if(closing)gameObject.SetActive(false);
        }
        public void Cancel(){claim?.Cancel();phase=0;gameObject.SetActive(false);}
        private void OnDestroy()=>claim?.Cancel();
    }
}
