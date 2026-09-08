using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // GManager.PlayZhuanChang: callbacks at .8 and .8+2.2 seconds;
    // animation completion separately hides the world effect.
    public sealed class RecoveredSceneTransition : MonoBehaviour
    {
        [SerializeField] private Animation player;
        [SerializeField] private RecoveredWorldRig rig;
        [SerializeField] private string clipName;
        [SerializeField] private float eventDelay,completionDelay;
        private RecoveredReelWait wait;
        private Action eventCall,completionCall;
        private bool playing;
        private int generation;
        public bool IsPlaying=>playing;
        public bool AwaitingCallbacks {get;private set;}
        public event Action<Exception> Failed;
        public void Play(Action atCover,Action afterCover)
        {
            Cancel();eventCall=atCover;completionCall=afterCover;
            rig.gameObject.SetActive(true);rig.Sample(0);player.Play(clipName);
            playing=true;AwaitingCallbacks=true;
            wait=RecoveredReelWait.Delay(eventDelay,AtCover,Fail);
        }
        private void AtCover()
        {
            wait=null;int token=generation;eventCall?.Invoke();
            if(token!=generation)return;
            wait=RecoveredReelWait.Delay(completionDelay,AfterCover,Fail);
        }
        private void AfterCover()
        {
            wait=null;AwaitingCallbacks=false;var callback=completionCall;
            eventCall=null;completionCall=null;callback?.Invoke();
        }
        private void LateUpdate()
        {
            if(!playing||player.IsPlaying(clipName))return;
            playing=false;rig.gameObject.SetActive(false);
        }
        private void Fail(Exception error){Cancel();Failed?.Invoke(error);}
        public void Cancel()
        {
            generation++;wait?.Cancel();wait=null;eventCall=null;completionCall=null;
            AwaitingCallbacks=false;playing=false;
            if(player!=null)player.Stop();if(rig!=null)rig.gameObject.SetActive(false);
        }
        private void OnDisable()=>Cancel();
    }
}
