using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredDailyTaskItem:MonoBehaviour
    {
        [SerializeField] private TMP_Text reward,description,progress;
        [SerializeField] private Image fill;
        [SerializeField] private Button claim;
        [SerializeField] private GameObject finished,black;
        private RecoveredDailyTaskWindow window;
        private RecoveredTaskInfo info;
        public Button ClaimButton=>claim;
        private void Awake()=>claim.onClick.AddListener(Click);
        public void Bind(RecoveredDailyTaskWindow owner,RecoveredTaskInfo task){window=owner;info=task;Refresh();}
        public void Refresh()
        {
            var game=window.Game;var record=game.PlayerProgress.FindTask(info.id);
            int count=record==null?0:Mathf.Min(record.count,info.taskAmount);
            bool available=game.PlayerProgress.CanClaimTask(info.id);
            bool received=record!=null&&record.isRecieve;
            reward.text=RecoveredCurrency.Format(info.reward,game.CurrentProfile.languageType);
            description.text=game.Rules.GetTaskDescription(info.id,count,info.taskAmount,game.CurrentProfile.languageType);
            progress.text=count+"/"+info.taskAmount;fill.fillAmount=info.taskAmount<=0?1:(float)count/info.taskAmount;
            // Incomplete and already received are different states. Only a receipt shows the tick.
            finished.SetActive(received);black.SetActive(received);
            claim.gameObject.SetActive(!received);claim.interactable=available;
        }
        private void Click()=>window.Claim(info);
    }
}
