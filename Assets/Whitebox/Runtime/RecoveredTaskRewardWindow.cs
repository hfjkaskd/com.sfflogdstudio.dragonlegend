using System;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // TaskItem opens the same UIRewardView used by lucky rewards (native 23ad4dc).
    public sealed class RecoveredTaskRewardWindow:MonoBehaviour
    {
        [SerializeField] private RecoveredBonusRewardPopup popup;
        private GameEntry game;
        public Button AdButton=>popup.ClaimButton;
        public Button PlainButton=>popup.PlainButton;
        public event Action<Component> WindowShowRequested;
        public void Bind(GameEntry context)
        {
            game=context;GetComponent<Canvas>().worldCamera=game.GetComponent<Canvas>().worldCamera;
            popup.WindowShowRequested+=Forward;popup.FlyCoinRequested+=Fly;
        }
        private void Forward(Component view)=>WindowShowRequested?.Invoke(view);
        private void Fly(float value,Action completed)=>game.CashFlight.Begin(value,completed,transform.parent,false);
        public void Show(RecoveredTaskInfo task,Action completed)
        {
            if(gameObject.activeSelf||!game.PlayerProgress.TryClaimTask(task.id,out float reward))return;
            popup.Show(reward,game.PlayerProgress,game.Rules,game.Ads,game.CurrentProfile.isA,game.CurrentProfile.languageType,_=>completed?.Invoke());
        }
        private void OnDestroy(){popup.WindowShowRequested-=Forward;popup.FlyCoinRequested-=Fly;game=null;}
    }
}
