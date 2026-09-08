using System;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredFreeBall : MonoBehaviour
    {
        [Serializable] private sealed class Clips {public string idle,start;}
        [SerializeField] private RecoveredWorldAnimation art;
        [SerializeField] private Text reward;
        [SerializeField] private RecoveredWorldRectClip clipping;
        public RecoveredWorldRectClip Clipping=>clipping;
        [SerializeField] private Clips[] types;
        [SerializeField] private float flightDuration,flightArcRatio;
        private Vector3 flightStart,flightControl,flightEnd;
        private float flightElapsed;
        private RecoveredFreeBall flightSource;
        private Action<RecoveredFreeBall,RecoveredFreeBall> flightCompleted;
        public bool IsFlying {get;private set;}
        public Vector3 FlightEnd=>flightEnd;
        public int BallType { get; private set; }
        public RecoveredWorldAnimation Art=>art;
        public Text Reward=>reward;
        private Clips Current=>types[BallType==0?0:BallType==1?1:2];

        // LongzhuItem.Init 0x23ae940: isInit does not change its native behavior.
        public void Initialize(int type,bool isInit=false)
        {
            BallType=type;PlayIdle();reward.text=string.Empty;
        }
        // 0x23aeb64; completion 0x23aeef8 selects idle using the current stored type.
        public void PlayStart()=>art.Play(Current.start,false,PlayIdle);
        private void PlayIdle()=>art.Play(Current.idle,true);
        // FlyLongZhu 0x23aecdc: .3s InOutSine quadratic Bezier, automatic arc .3 * distance.
        public void BeginFlight(RecoveredFreeBall source,Transform target,Action<RecoveredFreeBall,RecoveredFreeBall> completed)
        {
            flightSource=source;flightCompleted=completed;
            flightStart=transform.position;flightEnd=target.position;
            flightControl=(flightStart+flightEnd)*.5f+Vector3.up*(Vector3.Distance(flightStart,flightEnd)*flightArcRatio);
            flightElapsed=0;IsFlying=true;
        }
        private void Update()
        {
            if(!IsFlying)return;
            flightElapsed+=Time.deltaTime;
            float t=flightDuration==0?1:Mathf.Clamp01(flightElapsed/flightDuration);
            float eased=(1-Mathf.Cos(Mathf.PI*t))*.5f,inverse=1-eased;
            transform.position=flightStart*(inverse*inverse)+flightControl*(2*inverse*eased)+flightEnd*(eased*eased);
            if(t<1)return;
            IsFlying=false;var callback=flightCompleted;flightCompleted=null;callback?.Invoke(this,flightSource);
        }
        private void OnDisable(){IsFlying=false;flightCompleted=null;flightSource=null;}
    }
}
