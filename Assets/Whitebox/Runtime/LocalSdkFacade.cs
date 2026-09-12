using System;
using System.Collections.Generic;

namespace DragonLegend.Whitebox
{
    // NEW mock infrastructure. Not recovered third-party implementation.
    public enum AdOutcome { Rewarded = 0, Cancelled = 1, Unavailable = 2, Failed = 3 }
    public enum CashOutcome { Pending = 0, SimulatedApproved = 1, Rejected = 2 }

    public interface IAdFacade
    {
        void PlayRewardAd(Action successfulBack, Action failedBack, string posId, string sceneId);
        void PlayInterAd(string posId, string sceneId);
    }

    public interface IAdTransport
    {
        void ShowReward(string placement, string scene, Action<AdOutcome> completed);
        void ShowInterstitial(string placement, string scene, Action<bool> completed);
    }

    public sealed class LocalAdFacade : IAdFacade
    {
        private Action success;
        private Action failure;
        public bool Pending { get; private set; }
        public string Placement { get; private set; }
        public string Scene { get; private set; }
        public int InterstitialCount { get; private set; }
        public event Action RewardAdStarted;
        public event Action<AdOutcome> RewardAdCompleted;
        private readonly IAdTransport transport;
        private int rewardRequest;
        private int interstitialRequest;
        public bool InterstitialPending { get; private set; }
        public event Action InterstitialStarted;
        public event Action<bool> InterstitialCompleted;
        public LocalAdFacade(IAdTransport transport=null)
        {this.transport=transport;}

        public void PlayRewardAd(Action successfulBack, Action failedBack, string posId, string sceneId)
        {
            if (Pending || InterstitialPending) { failedBack?.Invoke(); return; }
            Placement = posId; Scene = sceneId;
            success = successfulBack; failure = failedBack; Pending = true;
            RewardAdStarted?.Invoke();
            int request = ++rewardRequest;
            if (transport != null)
            {
                try { transport.ShowReward(posId, sceneId, outcome =>
                { if (request == rewardRequest) CompleteReward(outcome); }); }
                catch { CompleteReward(AdOutcome.Failed); throw; }
            }
        }

        public bool Complete(AdOutcome outcome)
        {
            if (transport != null && outcome == AdOutcome.Rewarded) return false;
            return CompleteReward(outcome);
        }

        private bool CompleteReward(AdOutcome outcome)
        {
            if (!Pending) return false;
            if (outcome < AdOutcome.Rewarded || outcome > AdOutcome.Failed)
                throw new ArgumentOutOfRangeException(nameof(outcome));
            Action callback = outcome == AdOutcome.Rewarded ? success : failure;
            Pending = false; success = null; failure = null;
            ++rewardRequest;
            RewardAdCompleted?.Invoke(outcome);
            callback?.Invoke();
            return true;
        }

        public void PlayInterAd(string posId, string sceneId)
        {
            // The local transport cannot display two ads at once.
            if(Pending||InterstitialPending)return;
            Placement = posId; Scene = sceneId; InterstitialCount++;
            InterstitialPending=true;InterstitialStarted?.Invoke();
            int request = ++interstitialRequest;
            if (transport != null)
            {
                try { transport.ShowInterstitial(posId, sceneId, shown =>
                { if (request == interstitialRequest) FinishInterstitial(shown); }); }
                catch { FinishInterstitial(false); throw; }
            }
        }
        public bool CompleteInterstitial(bool shown)
        {
            if (transport != null && shown) return false;
            return FinishInterstitial(shown);
        }
        private bool FinishInterstitial(bool shown)
        {
            if(!InterstitialPending)return false;
            InterstitialPending=false;
            ++interstitialRequest;
            InterstitialCompleted?.Invoke(shown);return true;
        }
    }

    public sealed class MockCashOrder
    {
        public string RequestId { get; }
        public long Amount { get; }
        public CashOutcome Outcome { get; internal set; }
        internal MockCashOrder(string id, long amount)
        { RequestId = id; Amount = amount; Outcome = CashOutcome.Pending; }
    }

    public interface ICashFacade
    {
        MockCashOrder Submit(string requestId, long amount);
    }

    public sealed class LocalCashFacade : ICashFacade
    {
        private readonly Dictionary<string, MockCashOrder> orders = new Dictionary<string, MockCashOrder>(StringComparer.Ordinal);
        public MockCashOrder Submit(string requestId, long amount)
        {
            if (string.IsNullOrEmpty(requestId)) throw new ArgumentException("Request id is required.");
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (orders.TryGetValue(requestId, out MockCashOrder prior))
            {
                if (prior.Amount != amount) throw new InvalidOperationException("Request id was reused with a different amount.");
                return prior;
            }
            var order = new MockCashOrder(requestId, amount);
            orders.Add(requestId, order);
            return order;
        }

        public bool Resolve(string requestId, CashOutcome outcome)
        {
            if (outcome != CashOutcome.SimulatedApproved && outcome != CashOutcome.Rejected)
                throw new ArgumentOutOfRangeException(nameof(outcome));
            if (!orders.TryGetValue(requestId, out MockCashOrder order) || order.Outcome != CashOutcome.Pending) return false;
            order.Outcome = outcome;
            return true;
        }
    }
}
