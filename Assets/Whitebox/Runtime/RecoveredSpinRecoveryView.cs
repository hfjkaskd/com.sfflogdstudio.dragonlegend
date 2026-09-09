using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredSpinRecoveryView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Button moreSpinButton;
        [SerializeField] private string countFormat,timeFormat,countdownFormat;
        private RecoveredSpinEntry entry;
        private Action showMore;
        public RecoveredSpinRecovery Recovery {get;private set;}
        public TMP_Text Label=>label;
        public Button MoreSpinButton=>moreSpinButton;
        public event Action<string> SoundRequested;
        public void Bind(GameEntry game,Action openMore)
        {
            Unbind();entry=game.SpinEntry;showMore=openMore;
            Recovery=new RecoveredSpinRecovery(game.Rules,game.PlayerProgress,game.PlayerStore.Data);
            Recovery.DisplayRequested+=Display;entry.CountdownRequested+=Recovery.Begin;
            entry.SpinCountDisplayRequested+=Recovery.ShowCount;
            moreSpinButton.onClick.AddListener(OpenMore);Recovery.Initialize(Now());
        }
        private void OpenMore(){SoundRequested?.Invoke("click");showMore?.Invoke();}
        private static int Now()=>unchecked((int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        private void Update()=>Recovery?.Advance(Time.deltaTime,Now());
        private void Display(int count,int? remaining)
        {
            label.text=remaining.HasValue?string.Format(countdownFormat,count,
                string.Format(timeFormat,remaining.Value%3600/60,remaining.Value%60)):string.Format(countFormat,count);
        }
        public void Unbind()
        {
            if(Recovery!=null){entry.CountdownRequested-=Recovery.Begin;entry.SpinCountDisplayRequested-=Recovery.ShowCount;Recovery.Dispose();Recovery=null;}
            moreSpinButton.onClick.RemoveListener(OpenMore);entry=null;showMore=null;
        }
        private void OnDestroy()=>Unbind();
    }
}
