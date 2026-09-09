using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredBankWindow : MonoBehaviour, IRecoveredBankSelectionView
    {
        [SerializeField] private RectTransform content, buttons, fingerPrefab;
        [SerializeField] private Button openButton, leaveButton;
        [SerializeField] private RecoveredBankItem[] items;
        [SerializeField] private RecoveredBankFloat[] floats;
        [SerializeField] private RecoveredJackpotMeters jackpots;
        [SerializeField] private float duration;
        [SerializeField] private Vector2 floatHeight, floatDuration;
        [SerializeField] private AnimationCurve enterEase, exitEase, buttonEase;
        [SerializeField] private string showSound;
        private RecoveredBankSelection selection;
        private RecoveredPlayerProgress player;
        private RecoveredGameplayRules rules;
        private Func<int> bet;
        private int language, phase;
        private float elapsed, buttonElapsed, buttonDuration;
        private bool revealingButtons;
        private Vector3 buttonStart;
        private RectTransform finger;
        private Action closed;
        public int WinCount => 3;
        public RecoveredBankSelection Selection => selection;
        public RecoveredBankItem Item(int index) => items[index];
        public RectTransform Finger => finger;
        public Button OpenButton => openButton;
        public Button LeaveButton => leaveButton;
        public event Action<string> SoundRequested;
        public event Action<float,Action,Transform> FlyRequested;
        private void Awake()
        {
            openButton.onClick.AddListener(()=>selection.ClickButton("OpenBtn"));
            leaveButton.onClick.AddListener(()=>selection.ClickButton("UnPlayBtn"));
            for(int i=0;i<items.Length;i++)
            {
                int index=i;items[i].Button.onClick.AddListener(()=>selection.Click(index));
                items[i].SoundRequested+=PlaySound;items[i].FlyRequested+=Fly;
            }
        }
        public void Bind(RecoveredPlayerProgress progress,RecoveredGameplayRules config,IAdFacade ads,Func<int> readBet,int languageType,Camera camera)
        {
            Cancel();player=progress;rules=config;bet=readBet;language=languageType;
            selection=new RecoveredBankSelection(config,ads,this);GetComponent<Canvas>().worldCamera=camera;
        }
        public void Show(Action completed)
        {
            if(gameObject.activeSelf)return;
            gameObject.SetActive(true);PlaySound(showSound);closed=completed;
            player.SetBankCount(0);jackpots.Initialize(player,rules,bet,language);
            selection.Reset();buttons.localScale=Vector3.zero;revealingButtons=false;
            for(int i=0;i<items.Length;i++)items[i].Initialize(i==0?2:i==1?1:0,language);
            content.localScale=Vector3.zero;elapsed=0;phase=1;
        }
        private void AfterShow()
        {
            ShowFinger(items[UnityEngine.Random.Range(0,items.Length)].transform);
            for(int i=0;i<3;i++)floats[i].Play(UnityEngine.Random.Range(floatHeight.x,floatHeight.y),UnityEngine.Random.Range(floatDuration.x,floatDuration.y));
        }
        private void ShowFinger(Transform target)
        {
            if(finger==null)finger=Instantiate(fingerPrefab,target,false);else finger.SetParent(target,false);
            finger.anchoredPosition=Vector2.zero;finger.localScale=Vector3.one;finger.gameObject.SetActive(true);
        }
        public void HideFinger(){if(finger!=null)finger.gameObject.SetActive(false);}
        public void ShowContinueFinger()=>ShowFinger(buttons.GetChild(1));
        public void ShowAdIndicators(IReadOnlyList<int> selected)
        {
            for(int i=0;i<WinCount;i++){bool found=false;for(int j=0;j<selected.Count;j++)if(selected[j]==i){found=true;break;}items[i].ShowAd(!found);}
        }
        public void PlayItem(int itemIndex,float reward,bool firstSelection,Action<float> completed)=>items[itemIndex].Play(reward,completed);
        public void RevealButtons(float seconds){buttonStart=buttons.localScale;buttonElapsed=0;buttonDuration=seconds;revealingButtons=true;}
        public void PlaySound(string sound)=>SoundRequested?.Invoke(sound);
        private void Fly(float amount,Action completed,Transform source)=>FlyRequested?.Invoke(amount,completed,source);
        public void Hide(){content.localScale=Vector3.one;elapsed=0;phase=2;}
        private void Update()
        {
            if(revealingButtons){buttonElapsed+=Time.deltaTime;float t=Mathf.Clamp01(buttonElapsed/buttonDuration);buttons.localScale=Vector3.LerpUnclamped(buttonStart,Vector3.one,buttonEase.Evaluate(t));if(t==1)revealingButtons=false;}
            if(phase==0)return;
            elapsed+=Time.deltaTime;float percent=Mathf.Clamp01(elapsed/duration);
            content.localScale=Vector3.one*(phase==1?enterEase.Evaluate(percent):1-exitEase.Evaluate(percent));
            if(percent<1)return;
            bool hiding=phase==2;phase=0;
            if(!hiding){AfterShow();return;}
            gameObject.SetActive(false);HideFinger();var callback=closed;closed=null;callback?.Invoke();
            for(int i=0;i<floats.Length;i++)floats[i].Stop();
        }
        public void Cancel()
        {
            selection?.Cancel();closed=null;phase=0;revealingButtons=false;HideFinger();
            for(int i=0;i<items.Length;i++)items[i].Cancel();
            for(int i=0;i<floats.Length;i++)floats[i].Stop();
            jackpots.Cancel();gameObject.SetActive(false);
        }
        private void OnDestroy()=>selection?.Cancel();
    }
}
