using System;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredCashFlightItem:MonoBehaviour
    {
        private enum FlightCurve { InOutSine,InQuad }
        [SerializeField] private Image image;
        [SerializeField] private string spriteA,spriteB;
        [SerializeField] private float scatterDuration,flightDuration,automaticArcRatio;
        [SerializeField] private AnimationCurve scatterEase;
        [SerializeField] private FlightCurve flightCurve;
        private Transform cachedTransform,destination;
        private Vector3 scatterStart,scatterEnd,start,control,end;
        private float scatterElapsed,delayElapsed,delay,flightElapsed;
        private bool scattering,scheduled,flying;
        private int lastAdvanceFrame=-1;
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
            scatterElapsed=0;scattering=true;scheduled=false;flying=false;lastAdvanceFrame=-1;
        }
        public void Schedule(float seconds,Transform target)
        {delay=seconds;delayElapsed=0;destination=target;scheduled=true;}
        private void Update()=>Advance();
        internal void Advance()
        {
            if(lastAdvanceFrame==Time.frameCount)return;
            lastAdvanceFrame=Time.frameCount;
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
            float eased=flightCurve==FlightCurve.InOutSine?(1-Mathf.Cos(Mathf.PI*progress))*.5f:progress*progress,inverse=1-eased;
            cachedTransform.position=start*(inverse*inverse)+control*(2*inverse*eased)+end*(eased*eased);
            if(progress<1)return;
            flying=false;Arrived?.Invoke(this);
        }
        internal void Cancel(){scattering=false;scheduled=false;flying=false;destination=null;}
        private void OnDestroy()=>Cancel();
    }
}
