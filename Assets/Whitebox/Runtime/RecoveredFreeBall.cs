using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredFreeBall : MonoBehaviour
    {
        [Serializable] private sealed class Clips {public string idle,start,activate;}
        [SerializeField] private RecoveredWorldAnimation art;
        [SerializeField] private Text reward;
        [SerializeField] private RecoveredWorldRectClip clipping;
        public RecoveredWorldRectClip Clipping=>clipping;
        [SerializeField] private Clips[] types;
        [SerializeField] private float activationDelay;
        [SerializeField] private RecoveredFreeBallReward rewardPresentation;
        public RecoveredFreeBallReward RewardPresentation=>rewardPresentation;
        private readonly List<RecoveredReelWait> activationWaits=new List<RecoveredReelWait>(1);
        public Exception Error {get;private set;}
        [SerializeField] private float flightDuration,flightArcRatio;
        [SerializeField] private SortingGroup sortingGroup;
        [SerializeField] private int flightSortingOffset;
        private int restingSortingLayer,restingSortingOrder;
        private bool restingSortAtRoot;
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
        private void Awake()
        {
            restingSortingLayer=sortingGroup.sortingLayerID;restingSortingOrder=sortingGroup.sortingOrder;
            restingSortAtRoot=sortingGroup.sortAtRoot;
        }

        // LongzhuItem.Init 0x23ae940: isInit does not change its native behavior.
        public void Initialize(int type,bool isInit=false)
        {
            BallType=type;PlayIdle();reward.text=string.Empty;
        }
        // 0x23aeb64; completion 0x23aeef8 selects idle using the current stored type.
        public void PlayStart()=>art.Play(Current.start,false,PlayIdle);
        private void PlayIdle()=>art.Play(Current.idle,true);
        public void PlayActivation(Action<int> completed)
        {
            art.Play(Current.activate,false);RecoveredReelWait wait=null;
            wait=RecoveredReelWait.Delay(activationDelay,()=>{activationWaits.Remove(wait);completed?.Invoke(BallType);},error=>Error=error);
            activationWaits.Add(wait);
        }
        // FlyLongZhu 0x23aecdc: .3s InOutSine quadratic Bezier, automatic arc .3 * distance.
        public void BeginFlight(RecoveredFreeBall source,Transform target,Action<RecoveredFreeBall,RecoveredFreeBall> completed)
        {
            // World meshes do not inherit Canvas sibling ordering. Lift only the borrowed
            // flight above the main board; pooled stationary balls keep their original order.
            var canvas=target.GetComponentInParent<Canvas>();
            sortingGroup.sortAtRoot=true;
            sortingGroup.sortingLayerID=canvas!=null?canvas.sortingLayerID:restingSortingLayer;
            sortingGroup.sortingOrder=(canvas!=null?canvas.sortingOrder:restingSortingOrder)+flightSortingOffset;
            flightSource=source;flightCompleted=completed;
            flightStart=transform.position;flightEnd=target.position;
            flightControl=(flightStart+flightEnd)*.5f+Vector3.up*(Vector3.Distance(flightStart,flightEnd)*flightArcRatio);
            flightElapsed=0;IsFlying=true;
        }
        public void AttachForActivation(Transform parent)
        {
            // The destination is in world space; the fire Canvas uses a different scale.
            transform.SetParent(parent,true);
            var canvas=parent.GetComponentInParent<Canvas>();
            sortingGroup.sortAtRoot=true;
            sortingGroup.sortingLayerID=canvas!=null?canvas.sortingLayerID:restingSortingLayer;
            sortingGroup.sortingOrder=(canvas!=null?canvas.sortingOrder:restingSortingOrder)+flightSortingOffset;
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
        private void OnDisable()
        {
            IsFlying=false;flightCompleted=null;flightSource=null;
            sortingGroup.sortingLayerID=restingSortingLayer;sortingGroup.sortingOrder=restingSortingOrder;
            sortingGroup.sortAtRoot=restingSortAtRoot;
        }
        private void OnDestroy(){foreach(var wait in activationWaits)wait.Cancel();}
    }
}
