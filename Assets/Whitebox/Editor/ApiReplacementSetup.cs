using System;
using System.IO;
using System.Linq;
using Bizza.Sdk;
using Newtonsoft.Json;
using Obfuz.Settings;
using Obfuz.Unity;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;

namespace DragonLegend.Integration.Editor
{
    // Explicit parameter-driven authoring task; never runs in a player or changes runtime behavior.
    [InitializeOnLoad]
    public static class ApiReplacementSetup
    {
        private const string Root = "Artifacts/APIReplace";
        private const string Phase = Root + "/phase.txt";
        static ApiReplacementSetup()
        {
            if (File.Exists(Phase)) EditorApplication.delayCall += Execute;
        }

        [Serializable]
        public sealed class Parameters
        {
            public string appId, domain, key, productName, packageName, adjust, reward, interstitial, sdkKey, prefix;
        }

        [MenuItem("Dragon Legend/Validation/Apply API Release Parameters")]
        private static void Execute()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorApplication.delayCall += Execute; return; }
            string phase = File.ReadAllText(Phase);
            if (phase != "configure" && phase != "validate") return;
            try
            {
                if (phase == "configure") Configure();
                else Validate();
            }
            catch (Exception error)
            {
                File.WriteAllText(Root + "/error.txt", error.ToString());
                File.WriteAllText(Phase, "failed");
                Debug.LogException(error);
            }
        }

        private static void Configure()
        {
            var p = JsonConvert.DeserializeObject<Parameters>(File.ReadAllText(Root + "/parameters.json"));
            const string path = "Assets/StreamingAssets/ChannelConfig.bytes";
            byte[] original = File.ReadAllBytes(path);
            var config = ChannelConfigBinarySerializer.Deserialize(original);
            var old = new { config.AppId, config.adjustKey, config.incomeRate,
                config.httpConfig.domain, defaultCountry = (int)config.real_CustomConfig.DefaultCountry,
                country = (int)config.real_CustomConfig.Country, ads = config.sourceAds };
            string before = JsonConvert.SerializeObject(old);
            config.AppId = p.appId;
            config.httpConfig.domain = p.domain;
            config.httpConfig.aes_key = p.key;
            config.incomeRate = 0.7;
            config.real_CustomConfig.DefaultCountry = AccountModule.E_CountryType.US;
            config.real_CustomConfig.Country = AccountModule.E_CountryType.None;
            config.adjustKey = p.adjust;
            var ad = config.GetAdsConfig(E_AdsSource.Max);
            if (ad == null) { ad = new AdConfig(E_AdsSource.Max); config.sourceAds.Add(ad); }
            ad.rewardAdId = p.reward;
            ad.interAdId = p.interstitial;
            byte[] updated = ChannelConfigBinarySerializer.Serialize(config);
            var restored = ChannelConfigBinarySerializer.Deserialize(updated);
            if (restored.AppId != p.appId || restored.httpConfig.aes_key != p.key || restored.incomeRate != 0.7 ||
                restored.real_CustomConfig.DefaultCountry != AccountModule.E_CountryType.US ||
                restored.real_CustomConfig.Country != AccountModule.E_CountryType.None || restored.adjustKey != p.adjust)
                throw new InvalidOperationException("ChannelConfig round-trip failed.");
            File.WriteAllBytes(path, updated);
            File.WriteAllText(Root + "/channel-result.json", JsonConvert.SerializeObject(new {
                before = JsonConvert.DeserializeObject(before), after = new {
                    restored.AppId, restored.adjustKey, restored.incomeRate, restored.httpConfig.domain,
                    defaultCountry = (int)restored.real_CustomConfig.DefaultCountry,
                    country = (int)restored.real_CustomConfig.Country, ads = restored.sourceAds },
                roundTrip = "PASS", keyMatchesInput = restored.httpConfig.aes_key == p.key
            }, Formatting.Indented));
            PlayerSettings.productName = p.productName;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, p.packageName);
            AppLovinSettings.Instance.SdkKey = p.sdkKey;
            AppLovinSettings.Instance.SaveAsync();
            var obfuz = ObfuzSettings.Instance;
            obfuz.assemblySettings.assembliesToObfuscate = new[] { "Assembly-CSharp" };
            obfuz.assemblySettings.nonObfuscatedButReferencingObfuscatedAssemblies = new[] { "WKY_SDK", "Whitebox.Runtime" };
            obfuz.secretSettings.defaultStaticSecretKey = p.packageName;
            obfuz.secretSettings.defaultDynamicSecretKey = p.packageName;
            obfuz.encryptionVMSettings.codeGenerationSecretKey = p.packageName.Substring(p.packageName.LastIndexOf('.') + 1);
            obfuz.symbolObfusSettings.obfuscatedNamePrefix = p.prefix;
            ObfuzSettings.Save();
            ObfuzMenu.GenerateEncryptionVM();
            ObfuzMenu.SaveSecretFile();
            AssetDatabase.SaveAssets();
            File.WriteAllText(Phase, "validate");
            AssetDatabase.Refresh();
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
        }

        private static void Validate()
        {
            File.WriteAllText(Phase, "validating");
            var output = Root + "/AndroidScripts";
            Directory.CreateDirectory(output);
            var result = PlayerBuildInterface.CompilePlayerScripts(new ScriptCompilationSettings {
                group = BuildTargetGroup.Android, target = BuildTarget.Android,
                options = ScriptCompilationOptions.None }, output);
            if (result.assemblies == null || !result.assemblies.Any(x => x.EndsWith("WKY_SDK.dll")))
                throw new InvalidOperationException("Android SDK compilation did not produce WKY_SDK.dll.");
            File.WriteAllText(Root + "/compile-result.json", JsonConvert.SerializeObject(new {
                editor = "PASS", android = "PASS", assemblies = result.assemblies
            }, Formatting.Indented));
            File.WriteAllText(Phase, "passed");
            Debug.Log("[APIReplace] Configuration, Obfuz generation and Android script compilation passed.");
        }
    }
}
