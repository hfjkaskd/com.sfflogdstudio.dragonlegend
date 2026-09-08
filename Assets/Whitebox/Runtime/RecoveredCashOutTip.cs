using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredCashOutTip : MonoBehaviour
    {
        [SerializeField] private TMP_Text tips;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TMP_Text progressText;
        [SerializeField, TextArea] private string readyFormat;
        [SerializeField, TextArea] private string remainingFormat;
        public TMP_Text Tips => tips;
        public TMP_Text ProgressText => progressText;
        public Slider ProgressSlider => progressSlider;

        // CashOutTip.Init 23d7c40: first absent id, irrespective of record status.
        // Caller schedules PlayBtnAnim(.5). Native hide branches do not reactivate.
        public bool Initialize(RecoveredPlayerProgress player, RecoveredGameplayRules rules, bool isA, int language)
        {
            if (isA) { gameObject.SetActive(false); return false; }
            int selected = -1;
            for (int i = 0; i < rules.GetCashOutCount(); i++)
            {
                bool found = false;
                for (int j = 0; j < player.CashOutRecords.Count; j++)
                    if (player.CashOutRecords[j].id == i) { found = true; break; }
                if (!found) { selected = i; break; }
            }
            if (selected < 0) { gameObject.SetActive(false); return false; }
            float target = rules.GetCashOutCash(selected);
            progressSlider.minValue = 0; progressSlider.maxValue = 1;
            progressSlider.value = target <= player.GreenCount ? 1 : player.GreenCount / target;
            string goal = RecoveredCurrency.Format(target, language, 0);
            tips.text = target <= player.GreenCount ? string.Format(readyFormat, goal) :
                string.Format(remainingFormat, RecoveredCurrency.Format(target - player.GreenCount, language, 2), goal);
            progressText.text = RecoveredCurrency.Format(player.GreenCount, language, 2) + "/" + goal;
            return true;
        }
    }
}
