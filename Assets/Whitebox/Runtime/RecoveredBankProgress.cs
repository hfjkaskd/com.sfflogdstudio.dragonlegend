using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredBankProgress : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image fill;
        [SerializeField] private TMP_Text label;
        [SerializeField] private string countFormat, tip;
        private RecoveredPlayerProgress player;
        private RecoveredGameplayRules rules;
        private Action<string> showTip;
        public Button Button=>button;
        public Image Fill=>fill;
        public TMP_Text Label=>label;
        public event Action<string> SoundRequested;
        public void Bind(RecoveredPlayerProgress progress,RecoveredGameplayRules config,Action<string> tips)
        {
            Unbind();player=progress;rules=config;showTip=tips;
            player.BankReady+=Refresh;player.BankProgressChanged+=Refresh;button.onClick.AddListener(Click);Refresh();
        }
        // Main.InitBank 23bbb5c: the integer text is not clamped; Image handles fill clamping.
        public void Refresh()
        {
            label.text=string.Format(countFormat,player.BankCount,rules.GetBankSpinCD());
            fill.fillAmount=(float)player.BankCount/rules.GetBankSpinCD();
        }
        private void Click(){SoundRequested?.Invoke("click");showTip(tip);}
        public void Unbind()
        {
            if(player!=null){player.BankReady-=Refresh;player.BankProgressChanged-=Refresh;}
            button.onClick.RemoveListener(Click);player=null;rules=null;showTip=null;
        }
        private void OnDestroy()=>Unbind();
    }
}
