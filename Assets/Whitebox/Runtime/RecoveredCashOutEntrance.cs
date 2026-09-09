using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // UICashOutView.LoadAnim 23ab52c. The owning window starts this after CheckAll.
    public sealed class RecoveredCashOutEntrance : MonoBehaviour
    {
        [SerializeField] private RecoveredCashOutList list;
        [SerializeField] private RectTransform bottom;
        [SerializeField] private float cardDuration,cardInterval,bottomDuration,bottomOffset;
        [SerializeField] private AnimationCurve cardEase,bottomEase;
        private Vector2 initialBottom;
        private readonly List<RecoveredReelWait> waits=new List<RecoveredReelWait>();
        private readonly List<Motion> motions=new List<Motion>();
        private void Awake()=>initialBottom=bottom.anchoredPosition;
        public void Play(Func<bool> isCashMode)
        {
            motions.RemoveAll(motion=>motion.Finished);
            RecoveredReelWait pending=null;
            pending=RecoveredReelWait.Until(()=>list!=null&&list.IsCreateFinished,()=>{
                waits.Remove(pending);
                if(!isCashMode())return;
                var content=list.Scroll.content;
                for(int i=0;i<content.childCount;i++)
                {
                    var child=content.GetChild(i);child.gameObject.SetActive(true);child.localScale=Vector3.zero;
                    Schedule(new Motion(child,null,cardDuration,i*cardInterval,cardEase,0,0));
                }
                bottom.anchoredPosition=new Vector2(initialBottom.x,initialBottom.y-bottomOffset);
                Schedule(new Motion(null,bottom,bottomDuration,0,bottomEase,initialBottom.y-bottomOffset,initialBottom.y));
            },Debug.LogException);
            waits.Add(pending);
        }
        private void Schedule(Motion motion){motions.Add(motion);RecoveredReelStopLoop.Requeue(motion);}
        public void Cancel(){foreach(var pending in waits)pending.Cancel();waits.Clear();foreach(var motion in motions)motion.Cancel();motions.Clear();}
        private void OnDestroy()=>Cancel();
        private sealed class Motion:IRecoveredReelUpdateItem
        {
            private readonly Transform card;private readonly RectTransform panel;private readonly float duration,delay,from,to;private readonly AnimationCurve ease;
            private float elapsed;private bool cancelled;
            public bool Finished { get; private set; }
            public Motion(Transform target,RectTransform rect,float seconds,float offset,AnimationCurve curve,float start,float end)
            {card=target;panel=rect;duration=seconds;delay=offset;ease=curve;from=start;to=end;}
            public void Cancel()=>cancelled=true;
            public bool Step(int frame,float delta)
            {
                if(cancelled||(card==null&&panel==null)){Finished=true;return false;}elapsed+=delta;if(elapsed<delay)return true;
                float fraction=Mathf.Clamp01((elapsed-delay)/duration),value=ease.Evaluate(fraction);
                if(card!=null)card.localScale=Vector3.one*value;else panel.anchoredPosition=new Vector2(panel.anchoredPosition.x,Mathf.LerpUnclamped(from,to,value));
                Finished=fraction>=1;return !Finished;
            }
        }
    }
}
