using System;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // NEW integration harness. Original cash-out task conditions remain in reference evidence.
    public sealed class MockFlowView : MonoBehaviour
    {
        [SerializeField] private Button advertisement;
        [SerializeField] private Button completeAdvertisement;
        [SerializeField] private Button submitCash;
        [SerializeField] private Button resolveCash;
        [SerializeField] private MockFlowSettings settings;
        private readonly LocalAdFacade ads = new LocalAdFacade();
        private readonly LocalCashFacade cash = new LocalCashFacade();
        public event Action Rewarded;
        public event Action AdFailed;
        public event Action<MockCashOrder> CashChanged;
        private MockCashOrder order;

        // Authoring hook for the small integration fixture. Runtime uses saved Prefab references.
        public void Configure(Button ad, Button completeAd, Button submit, Button resolve, MockFlowSettings config)
        {
            advertisement = ad;
            completeAdvertisement = completeAd;
            submitCash = submit;
            resolveCash = resolve;
            settings = config;
        }

        private void OnEnable()
        {
            if (!advertisement || !completeAdvertisement || !submitCash || !resolveCash || !settings)
                throw new InvalidOperationException("Configure all buttons and settings in the Prefab.");
            advertisement.onClick.AddListener(ShowAd);
            completeAdvertisement.onClick.AddListener(CompleteAd);
            submitCash.onClick.AddListener(SubmitCash);
            resolveCash.onClick.AddListener(ResolveCash);
        }
        private void OnDisable()
        {
            if (advertisement) advertisement.onClick.RemoveListener(ShowAd);
            if (completeAdvertisement) completeAdvertisement.onClick.RemoveListener(CompleteAd);
            if (submitCash) submitCash.onClick.RemoveListener(SubmitCash);
            if (resolveCash) resolveCash.onClick.RemoveListener(ResolveCash);
        }
        private void ShowAd() { if (!ads.Pending) ads.PlayRewardAd(OnRewarded, OnAdFailed, settings.placement, settings.scene); }
        private void OnRewarded() { Rewarded?.Invoke(); }
        private void OnAdFailed() { AdFailed?.Invoke(); }
        private void CompleteAd() { ads.Complete(settings.adOutcome); }
        private void SubmitCash() { order = cash.Submit(settings.requestId, settings.amount); CashChanged?.Invoke(order); }
        private void ResolveCash()
        {
            if (order != null && cash.Resolve(order.RequestId, settings.cashOutcome)) CashChanged?.Invoke(order);
        }
    }
}
