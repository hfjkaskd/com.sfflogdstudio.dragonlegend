using System;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredCashFlightItem:MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private string spriteA,spriteB;
        [SerializeField] private float scatterDuration,flightDuration,automaticArcRatio;
        [SerializeField] private AnimationCurve scatterEase,flightEase;
        private Transform cachedTransform,destination;
        private Vector3 scatterStart,scatterEnd,start,control,end;
        private float scatterElapsed,delayElapsed,delay,flightElapsed;
        private bool scattering,scheduled,flying;
        public event Action<RecoveredCashFlightItem> Arrived;
        public bool IsFlying=>flying;
        public bool IsScheduled=>scheduled;
        public Vector3 StartPoint=>start;
        public Vector3 ControlPoint=>control;
        public Vector3 EndPoint=>end;
        public Image Image=>image;
        private void Awake()=>cachedTransform=transform;
        public void Initialize(bool isA)
        {
            image.sprite=Resources.Load<Sprite>(isA?spriteA:spriteB);image.SetNativeSize();
        }
        public void Scatter(Transform parent,Transform source,Vector2 offset)
        {
            cachedTransform.SetParent(parent,false);cachedTransform.localPosition=Vector3.zero;
            cachedTransform.localRotation=Quaternion.identity;cachedTransform.localScale=Vector3.one;
            if(source!=null)cachedTransform.position=source.position;
            cachedTransform.SetAsLastSibling();scatterStart=cachedTransform.localPosition;
            scatterEnd=scatterStart+new Vector3(offset.x,offset.y,0);
            scatterElapsed=0;scattering=true;scheduled=false;flying=false;
        }
        public void Schedule(float seconds,Transform target)
        {delay=seconds;delayElapsed=0;destination=target;scheduled=true;}
        private void Update()
        {
            if(scattering) {
                scatterElapsed+=Time.deltaTime;float t=Mathf.Clamp01(scatterElapsed/scatterDuration);
                cachedTransform.localPosition=Vector3.LerpUnclamped(scatterStart,scatterEnd,scatterEase.Evaluate(t));
                if(t>=1)scattering=false;
            }
            if(scheduled) {
                delayElapsed+=Time.unscaledDeltaTime;
                if(delayElapsed<delay)return;
                scheduled=false;flying=true;flightElapsed=0;
                start=cachedTransform.position;end=destination.position;
                control=(start+end)*.5f+Vector3.up*(Vector3.Distance(start,end)*automaticArcRatio);
                return;
            }
            if(!flying)return;
            flightElapsed+=Time.deltaTime;float progress=Mathf.Clamp01(flightElapsed/flightDuration);
            float eased=flightEase.Evaluate(progress),inverse=1-eased;
            cachedTransform.position=start*(inverse*inverse)+control*(2*inverse*eased)+end*(eased*eased);
            if(progress<1)return;
            flying=false;Arrived?.Invoke(this);
        }
        private void OnDisable(){scattering=false;scheduled=false;flying=false;destination=null;}
    }
}
