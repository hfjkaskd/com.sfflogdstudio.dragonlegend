using System;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // UICashOutView.CheckLayOut 23a9858 / CheckTag 23a99e8.
    public sealed class RecoveredCashOutModeView : MonoBehaviour
    {
        [SerializeField] private Button cashButton,giftButton;
        [SerializeField] private GameObject cashSelection,giftSelection,cashRect,giftRect,bottom;
        [SerializeField] private RecoveredCashOutPaymentHeader header;
        public bool IsGift { get; private set; }
        public Button CashButton=>cashButton;
        public Button GiftButton=>giftButton;
        public event Action<string> SoundRequested;
        public event Action<bool> ItemsRefreshRequested;
        private void Awake()
        {
            cashButton.onClick.AddListener(SelectCash);
            giftButton.onClick.AddListener(SelectGift);
        }
        public void Bind(RecoveredGameplayRules rules,RecoveredPlayerProgress player)=>header.Bind(rules,player);
        public void ResetForShow(){IsGift=false;Refresh();}
        private void SelectCash()=>Select(false);
        private void SelectGift()=>Select(true);
        private void Select(bool gift)
        {
            // Native plays click even when the selected tab is unchanged.
            SoundRequested?.Invoke("click");if(IsGift==gift)return;
            IsGift=gift;Refresh();
        }
        public void Refresh()
        {
            header.RefreshMode(IsGift);
            bottom.SetActive(!IsGift);giftRect.SetActive(IsGift);cashRect.SetActive(!IsGift);
            cashSelection.SetActive(!IsGift);giftSelection.SetActive(IsGift);
            ItemsRefreshRequested?.Invoke(IsGift);
        }
    }
}
