using TMPro;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // GiftItem.InitUI 23a6a44: raw record count, no ratio clamp, no iconName lookup.
    public sealed class RecoveredGiftItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text rewardText,nameText,alternateNameText,progressText;
        [SerializeField] private RectTransform fill;
        [SerializeField] private string displayName,progressFormat;
        [SerializeField] private int id;
        [SerializeField] private float progressWidth;
        public TMP_Text RewardText=>rewardText;
        public TMP_Text NameText=>nameText;
        public TMP_Text AlternateNameText=>alternateNameText;
        public TMP_Text ProgressText=>progressText;
        public RectTransform Fill=>fill;
        public void Refresh(RecoveredGameplayRules rules,RecoveredPlayerProgress player,int language,int selection,RectTransform frame)
        {
            nameText.text=displayName;alternateNameText.text=displayName;
            rewardText.text=rules.GetCollectReward(language);
            int count=player.CollectRecords.Count,total=rules.GetCollectInfoCount();
            progressText.text=string.Format(progressFormat,count,total);
            fill.sizeDelta=new Vector2(count*progressWidth/total,fill.rect.height);
            if(selection!=id)return;
            frame.SetParent(transform,false);frame.gameObject.SetActive(true);frame.localPosition=Vector3.zero;
        }
    }
}
