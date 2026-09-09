using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // Main.RefreshMoreWild 23baa28 and MoreWild button branch 23bdce4..23bddac.
    public sealed class RecoveredMoreWildEntry : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private GameObject progress;
        [SerializeField] private Image fill;
        [SerializeField] private TMP_Text label, word;
        [SerializeField] private string countFormat;
        private RecoveredPlayerProgress player;
        private RecoveredGameplayRules rules;
        private Action open;
        public Button Button=>button;
        public GameObject Progress=>progress;
        public Image Fill=>fill;
        public TMP_Text Label=>label;
        public TMP_Text Word=>word;
        public event Action<string> SoundRequested;
        public void Bind(GameEntry game,Action show)
        {
            Unbind();player=game.PlayerProgress;rules=game.Rules;open=show;
            player.MoreWildChanged+=Refresh;button.onClick.AddListener(Click);Refresh();
        }
        private void Refresh()
        {
            if(player.MoreWild>0)
            {
                word.gameObject.SetActive(false);progress.SetActive(true);
                label.text=string.Format(countFormat,player.MoreWild,rules.GetMoreWild());
                fill.fillAmount=(float)player.MoreWild/rules.GetMoreWild();
            }
            else {word.gameObject.SetActive(true);progress.SetActive(false);}
        }
        private void Click(){SoundRequested?.Invoke("click");open?.Invoke();}
        public void Unbind()
        {
            if(player!=null)player.MoreWildChanged-=Refresh;
            button.onClick.RemoveListener(Click);player=null;rules=null;open=null;
        }
        private void OnDestroy()=>Unbind();
    }
}
