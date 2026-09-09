using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredCashOutPaymentHeader : MonoBehaviour
    {
        [SerializeField] private Button[] paymentButtons;
        [SerializeField] private Button accountButton;
        [SerializeField] private RectTransform paymentLayout,paymentFrame;
        [SerializeField] private TMP_InputField accountInput;
        [SerializeField] private RecoveredCashOutList list;
        [SerializeField] private string visibleConfigType,promptPrefix,promptSuffix,giftPrompt;
        [SerializeField] private string[] providerNames;
        private RecoveredPlayerProgress player;private RecoveredGameplayRules rules;
        private bool gift;private int type=1;
        public int PaymentType=>type;
        public TMP_InputField AccountInput=>accountInput;
        public RectTransform PaymentFrame=>paymentFrame;
        public event Action<string> SoundRequested;
        public event Action<int> CashAccountRequested;
        public event Action GiftAccountRequested;
        private void Awake()
        {
            for(int i=0;i<paymentButtons.Length;i++){int paymentType=i+1;paymentButtons[i].onClick.AddListener(()=>{SoundRequested?.Invoke("click");RefreshPaymentType(paymentType);});}
            accountButton.onClick.AddListener(()=>{SoundRequested?.Invoke("click");if(gift)GiftAccountRequested?.Invoke();else CashAccountRequested?.Invoke(type);RefreshAccount(type);});
        }
        public void Bind(RecoveredGameplayRules config,RecoveredPlayerProgress progress)
        {rules=config;player=progress;}
        public void PrepareCashShow(){gift=false;type=1;RefreshAccount(type);}
        public void ResetPaymentFrame()
        {paymentFrame.SetParent(transform,false);paymentFrame.SetParent(paymentLayout.GetChild(type-1),false);}
        public void RefreshMode(bool giftMode)
        {
            gift=giftMode;paymentLayout.gameObject.SetActive(!gift&&rules.GetConfigType()==visibleConfigType);RefreshAccount(type);
        }
        // UICashOutView.RefreshCashOutItemType 23a7720. Its bottom refresh uses
        // the saved initial selection, not the most recently clicked card.
        public void RefreshPaymentType(int paymentType)
        {
            type=paymentType;ResetPaymentFrame();
            RefreshAccount(type);list.RefreshPaymentType(type);
        }
        public void RefreshAccount(int paymentType)
        {
            if(gift){accountInput.text=player.GiftDeliveryAccount==null?giftPrompt:player.GiftDeliveryAccount.address??string.Empty;return;}
            type=paymentType;
            foreach(var account in player.CashOutAccounts)if(account.type==type){accountInput.text=account.emailName;return;}
            string provider=providerNames[type>=1&&type<=3?type-1:3];accountInput.text=promptPrefix+provider+promptSuffix;
        }
    }
}
