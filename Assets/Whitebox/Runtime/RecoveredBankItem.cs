using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // BankItem.InitUI 2390650 / PlayAnim.MoveNext 2390a3c.
    public sealed class RecoveredBankItem : MonoBehaviour
    {
        [SerializeField] private RecoveredRegionAnimator ball;
        [SerializeField] private RectTransform reward;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image ad;
        [SerializeField] private Button button;
        [SerializeField] private int[] idleClips, fireClips;
        [SerializeField] private float revealDelay, rewardDelay, rewardScale;
        [SerializeField] private AnimationCurve rewardEase;
        private int type, language;
        private RecoveredReelWait revealWait, flightWait;
        private bool scaling;
        private float elapsed;
        private int lifetime;
        public Button Button=>button;
        public RectTransform Reward=>reward;
        public TMP_Text Label=>label;
        public Image Ad=>ad;
        public RecoveredRegionAnimator Ball=>ball;
        public event Action<string> SoundRequested;
        public event Action<float,Action,Transform> FlyRequested;
        public void Initialize(int ballType,int languageType)
        {
            type=ballType==0?0:ballType==1?1:2;language=languageType;
            ball.Play(idleClips[type]);reward.gameObject.SetActive(false);ad.gameObject.SetActive(false);
        }
        public void ShowAd(bool value)=>ad.gameObject.SetActive(value);
        public void Play(float amount,Action<float> completed)
        {
            int generation=lifetime;
            ball.PlayOnce(fireClips[type],null);SoundRequested?.Invoke("coinReveal");
            revealWait=RecoveredReelWait.Delay(revealDelay,()=>{
                revealWait=null;label.text=RecoveredCurrency.Format(amount,language,2);
                reward.localScale=Vector3.zero;reward.gameObject.SetActive(true);elapsed=0;scaling=true;
                flightWait=RecoveredReelWait.Delay(rewardDelay,()=>{
                    flightWait=null;
                    FlyRequested?.Invoke(amount,()=>{if(generation==lifetime)completed?.Invoke(amount);},reward);
                },Fail);
            },Fail);
        }
        private void Update()
        {
            if(!scaling)return;
            elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/rewardDelay);
            reward.localScale=Vector3.one*(rewardScale*rewardEase.Evaluate(t));if(t>=1)scaling=false;
        }
        public void Cancel()
        {
            lifetime++;revealWait?.Cancel();flightWait?.Cancel();revealWait=null;flightWait=null;scaling=false;
        }
        private void Fail(Exception error){Cancel();Debug.LogException(error,this);}
        private void OnDestroy()=>Cancel();
    }
}
