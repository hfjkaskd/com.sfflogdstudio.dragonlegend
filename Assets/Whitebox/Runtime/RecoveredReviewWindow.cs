using System;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // UIReViewUsView 23d7700 / 23d77f0 / 23d79a8 / 23d7b54.
    public sealed class RecoveredReviewWindow : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private Button[] stars;
        [SerializeField] private GameObject[] highlights;
        [SerializeField] private Button claim, close;
        [SerializeField] private string storePrefix, showSound;
        [SerializeField] private float duration;
        [SerializeField] private AnimationCurve enterEase, exitEase;
        private Action<string> openUrl;
        private string identifier;
        private Action completed;
        private int phase;
        private float elapsed;
        private bool destroyAfterHide;
        public int StarCount {get;private set;}
        public Button Star(int index)=>stars[index];
        public GameObject Highlight(int index)=>highlights[index];
        public Button ClaimButton=>claim;
        public Button CloseButton=>close;
        public event Action<string> SoundRequested;
        private void Awake()
        {
            for(int i=0;i<stars.Length;i++){int count=i+1;stars[i].onClick.AddListener(()=>{PlayClick();Select(count);});}
            claim.onClick.AddListener(Claim);close.onClick.AddListener(()=>{PlayClick();Hide(false);});
        }
        public void Bind(Camera camera,string applicationIdentifier,Action<string> navigate)
        {GetComponent<Canvas>().worldCamera=camera;identifier=applicationIdentifier;openUrl=navigate;}
        public void Show(Action callback)
        {
            if(gameObject.activeSelf)return;
            gameObject.SetActive(true);SoundRequested?.Invoke(showSound);completed=callback;Select(0);
            destroyAfterHide=false;content.localScale=Vector3.zero;elapsed=0;phase=1;
        }
        private void Select(int count){StarCount=count;for(int i=0;i<highlights.Length;i++)highlights[i].SetActive(i<count);}
        private void PlayClick()=>SoundRequested?.Invoke("click");
        private void Claim()
        {
            PlayClick();if(StarCount>4){openUrl(storePrefix+identifier);Hide(true);}else Hide(false);
        }
        private void Hide(bool destroy){destroyAfterHide=destroy;content.localScale=Vector3.one;elapsed=0;phase=2;}
        private void Update()
        {
            if(phase==0)return;elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/duration);
            content.localScale=Vector3.one*(phase==1?enterEase.Evaluate(t):1-exitEase.Evaluate(t));if(t<1)return;
            bool hiding=phase==2;phase=0;if(!hiding)return;
            gameObject.SetActive(false);var callback=completed;completed=null;callback?.Invoke();
            if(destroyAfterHide)Destroy(gameObject);
        }
        public void Cancel(){completed=null;phase=0;gameObject.SetActive(false);}
    }
}
