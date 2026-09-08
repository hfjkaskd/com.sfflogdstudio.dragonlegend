using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // Lives outside the window: the native completion source resolves after hide.
    public sealed class RecoveredBonusExit:MonoBehaviour
    {
        [SerializeField] private float automaticDelay=2,sourceDelay=.2f;
        private RecoveredBonusWindow window;
        private RecoveredSceneTransition transition;
        private RecoveredPlayerProgress progress;
        private Action completed;
        private RecoveredReelWait automaticWait,closeWait,sourceWait;
        private bool closing;
        public bool IsClosing=>closing;
        public event Action PauseMusicRequested;
        public event Action<string> SoundRequested,ChangeMusicRequested;
        public event Action<Exception> Failed;
        public void Bind(RecoveredBonusWindow view,RecoveredSceneTransition effect,RecoveredPlayerProgress data,Action completion)
        {
            Unbind();window=view;transition=effect;progress=data;completed=completion;
            window.CloseRequested+=Close;window.ExitRequested+=AutoClose;transition.Failed+=Fail;
        }
        private void AutoClose()
        {
            automaticWait=RecoveredReelWait.Delay(automaticDelay,()=>{automaticWait=null;Close();},Fail);
        }
        private void Close()
        {
            closeWait=RecoveredReelWait.Until(()=>window.Selection.IsEnd,()=>{
                closeWait=null;if(closing)return;closing=true;
                PauseMusicRequested?.Invoke();SoundRequested?.Invoke("transform");
                transition.Play(window.HideAtCover,AfterCover);
            },Fail);
        }
        private void AfterCover()
        {
            ChangeMusicRequested?.Invoke(progress.GameSlotType==RecoveredSlotType.Base?"normalBg":"freeBg");
            sourceWait=RecoveredReelWait.Delay(sourceDelay,()=>{
                sourceWait=null;var callback=completed;completed=null;callback?.Invoke();
            },Fail);
        }
        private void Fail(Exception error){Unbind();Failed?.Invoke(error);}
        public void Unbind()
        {
            automaticWait?.Cancel();closeWait?.Cancel();sourceWait?.Cancel();automaticWait=null;closeWait=null;sourceWait=null;
            if(window!=null){window.CloseRequested-=Close;window.ExitRequested-=AutoClose;}
            if(transition!=null){transition.Failed-=Fail;transition.Cancel();}
            window=null;transition=null;progress=null;completed=null;closing=false;
        }
        private void OnDisable()=>Unbind();
    }
}
