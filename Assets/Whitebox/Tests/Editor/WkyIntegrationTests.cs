using System;
using DragonLegend.Whitebox;
using NUnit.Framework;

public sealed class WkyIntegrationTests
{
    [TestCase(null, AccountModule.E_CountryType.US, 0, "en-US")]
    [TestCase("", AccountModule.E_CountryType.BR, 1, "pt-BR")]
    [TestCase("   ", AccountModule.E_CountryType.BR, 1, "pt-BR")]
    [TestCase("BR", AccountModule.E_CountryType.US, 1, "pt-BR")]
    [TestCase(" US ", AccountModule.E_CountryType.BR, 0, "en-US")]
    [TestCase(null, AccountModule.E_CountryType.ID, 0, "en-US")]
    [TestCase("ID", AccountModule.E_CountryType.BR, 0, "en-US")]
    public void MissingServerCountryUsesConfiguredDefaultAndServerCountryTakesPriority(
        string serverCountry, AccountModule.E_CountryType defaultCountry, int language, string locale)
    {
        var prior = Bizza.Sdk.ChannelConfig.Instance;
        try
        {
            var config = new Bizza.Sdk.ChannelConfig();
            config.real_CustomConfig.DefaultCountry = defaultCountry;
            Assert.IsTrue(WkyRuntime.TryResolveLanguage(serverCountry, config, out int actual, out string selected));
            Assert.AreEqual(language, actual);
            Assert.AreEqual(locale, selected);
        }
        finally { Bizza.Sdk.ChannelConfig.Instance = prior; }
    }

    [Test]
    public void RemovingRetiredTypesPreservesTheSuppliedChannelConfigBytes()
    {
        var prior = Bizza.Sdk.ChannelConfig.Instance;
        try
        {
            byte[] original = System.IO.File.ReadAllBytes(
                System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "ChannelConfig.bytes"));
            var config = Bizza.Sdk.ChannelConfigBinarySerializer.Deserialize(original);
            UnityEngine.Debug.Log("[WKY country regression] Configured default country value: " +
                (int)config.real_CustomConfig.DefaultCountry);
            Assert.IsTrue(WkyRuntime.TryResolveLanguage(null, config, out _, out _),
                "The shipped ChannelConfig must allow startup without a server country.");
            byte[] saved = Bizza.Sdk.ChannelConfigBinarySerializer.Serialize(config);
            CollectionAssert.AreEqual(original, saved,
                "Removing the retired feature must preserve all existing serialized parameters.");
        }
        finally { Bizza.Sdk.ChannelConfig.Instance = prior; }
    }

    private sealed class Transport : IAdTransport
    {
        public Action<AdOutcome> Reward;
        public Action<bool> Interstitial;
        public string Placement;
        public string Scene;
        public void ShowReward(string placement, string scene, Action<AdOutcome> completed)
        { Placement = placement; Scene = scene; Reward = completed; }
        public void ShowInterstitial(string placement, string scene, Action<bool> completed)
        { Placement = placement; Scene = scene; Interstitial = completed; }
    }

    [Test]
    public void SdkRewardRequiresCallbackAndIgnoresDuplicateAndStaleCallbacks()
    {
        var transport = new Transport();
        var ads = new LocalAdFacade(transport: transport);
        int rewards = 0, failures = 0;
        ads.PlayRewardAd(() => rewards++, () => failures++, "cash_bonus", "treasure");
        Assert.AreEqual("cash_bonus", transport.Placement);
        Assert.AreEqual("treasure", transport.Scene);
        Assert.IsFalse(ads.Complete(AdOutcome.Rewarded));
        Assert.AreEqual(0, rewards);
        var oldCallback = transport.Reward;
        oldCallback(AdOutcome.Rewarded);
        oldCallback(AdOutcome.Rewarded);
        Assert.AreEqual(1, rewards);
        ads.PlayRewardAd(() => rewards++, () => failures++, "retry", "treasure");
        oldCallback(AdOutcome.Failed);
        Assert.IsTrue(ads.Pending);
        transport.Reward(AdOutcome.Failed);
        Assert.AreEqual(1, failures);
        Assert.IsFalse(ads.Pending);
    }

    [Test]
    public void InterstitialBlocksRewardAndCannotBeCompletedBySimulator()
    {
        var transport = new Transport();
        var ads = new LocalAdFacade(transport: transport);
        int failures = 0, completed = 0;
        ads.InterstitialCompleted += shown => { if (shown) completed++; };
        ads.PlayInterAd("level_end", "main");
        Assert.IsFalse(ads.CompleteInterstitial(true));
        ads.PlayRewardAd(() => Assert.Fail("Overlapping ad rewarded"), () => failures++, "reward", "main");
        Assert.AreEqual(1, failures);
        transport.Interstitial(true);
        transport.Interstitial(true);
        Assert.AreEqual(1, completed);
        Assert.IsFalse(ads.InterstitialPending);
    }

    [TestCase("US", 0, "en-US")]
    [TestCase("br", 1, "pt-BR")]
    public void ServerCountryMapsToExistingGameLanguages(string country, int language, string locale)
    {
        Assert.IsTrue(WkyRuntime.TryGetLanguage(country, out int actual, out string selected));
        Assert.AreEqual(language, actual);
        Assert.AreEqual(locale, selected);
    }

    [TestCase("ID")]
    [TestCase("")]
    [TestCase(null)]
    public void UnsupportedServerCountryIsNotSilentlyConverted(string country)
    { Assert.IsFalse(WkyRuntime.TryGetLanguage(country, out _, out _)); }
}
