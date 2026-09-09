using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredCollectTip : MonoBehaviour
    {
        [SerializeField] private TMP_Text tips, rewardText, progressText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Image collectImage;
        [SerializeField,TextArea] private string remainingFormat;
        [SerializeField] private string progressFormat;
        [SerializeField] private float revealDelay, revealDuration;
        [SerializeField] private AnimationCurve revealEase;
        public TMP_Text Tips => tips;
        public TMP_Text RewardText => rewardText;
        public TMP_Text ProgressText => progressText;
        public Slider ProgressSlider => progressSlider;
        public Image CollectImage => collectImage;

        // CollectTip.Init 23db440 counts records, including duplicates/received records.
        public void Initialize(RecoveredPlayerProgress player,RecoveredGameplayRules rules,bool isA,int language)
        {
            if(isA){gameObject.SetActive(false);return;}
            gameObject.SetActive(false);
            RecoveredTreasureCardRunner.Run(Reveal());
            int total=rules.GetCollectInfos().Count, count=player.CollectRecords.Count;
            tips.text=string.Format(remainingFormat,unchecked(total-count));
            progressSlider.minValue=0;progressSlider.maxValue=1;
            progressSlider.value=(float)count/total;
            progressText.text=string.Format(progressFormat,count,total);
            rewardText.text=rules.GetCollectReward(language);
            // CollectImg is deliberately unchanged by the original Init.
        }
        private IEnumerator Reveal()
        {
            float elapsed=0;bool shown=false;
            while(this!=null)
            {
                elapsed+=Time.deltaTime;
                if(elapsed>=revealDelay)
                {
                    if(!shown){gameObject.SetActive(true);transform.localScale=Vector3.zero;shown=true;}
                    float t=Mathf.Clamp01((elapsed-revealDelay)/revealDuration);
                    transform.localScale=Vector3.one*revealEase.Evaluate(t);
                    if(t>=1)yield break;
                }
                yield return null;
            }
        }
    }
}
