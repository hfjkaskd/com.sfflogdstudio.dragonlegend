using System;

namespace DragonLegend.Whitebox
{
    public sealed class WkyAdTransport : IAdTransport
    {
        public void ShowReward(string placement, string scene, Action<AdOutcome> completed)
        {
            if (!WKY_Flow.isInit || !BizzaSdk.Ad.Inited)
            { completed(AdOutcome.Unavailable); return; }
            BizzaSdk.Ad.ShowRewardAd(placement, 0f,
                result => completed(result.success ? AdOutcome.Rewarded : AdOutcome.Failed));
        }

        public void ShowInterstitial(string placement, string scene, Action<bool> completed)
        {
            if (!WKY_Flow.isInit || !BizzaSdk.Ad.Inited)
            { completed(false); return; }
            BizzaSdk.Ad.ShowInterAd(placement, 0f, result => completed(result.success), false);
        }
    }
}
