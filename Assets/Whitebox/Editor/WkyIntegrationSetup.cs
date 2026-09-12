using System;
using System.IO;
using System.Linq;
using Obfuz.Settings;
using Obfuz.Unity;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Player;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace DragonLegend.Integration.Editor
{
    // Authoring/validation only: does not replace any runtime initialization or data path.
    [InitializeOnLoad]
    public static class WkyIntegrationSetup
    {
        private const string Root = "Artifacts/WKYIntegration";
        private const string Phase = Root + "/setup-phase.txt";
        private static TestRunnerApi runner;
        static WkyIntegrationSetup()
        {
            if (File.Exists(Phase) && File.ReadAllText(Phase) == "test")
                EditorApplication.delayCall += RunRegressionTests;
            if (File.Exists(Phase) && File.ReadAllText(Phase) == "configure")
                EditorApplication.delayCall += ConfigureWhenReady;
            if (File.Exists(Phase) && File.ReadAllText(Phase) == "validate")
                EditorApplication.delayCall += ValidateWhenReady;
        }

        [MenuItem("Dragon Legend/Validation/WKY Regression Tests")]
        public static void RunRegressionTests()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += RunRegressionTests; return; }
            File.WriteAllText(Phase, "testing");
            runner = ScriptableObject.CreateInstance<TestRunnerApi>();
            runner.RegisterCallbacks(new Results());
            runner.Execute(new ExecutionSettings(new Filter
            { testMode = TestMode.EditMode, assemblyNames = new[] { "WKY.Integration.EditorTests" } }));
        }

        private static void ConfigureWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += ConfigureWhenReady; return; }
            if (File.Exists(Phase) && File.ReadAllText(Phase) == "configure") Run();
        }

        public static void Run()
        {
            Directory.CreateDirectory(Root);
            var settings = ObfuzSettings.Instance;
            settings.buildPipelineSettings ??= new BuildPipelineSettings();
            settings.compatibilitySettings ??= new CompatibilitySettings();
            settings.assemblySettings ??= new AssemblySettings();
            settings.obfuscationPassSettings ??= new ObfuscationPassSettings();
            settings.secretSettings ??= new SecretSettings();
            settings.encryptionVMSettings ??= new EncryptionVMSettings();
            settings.symbolObfusSettings ??= new SymbolObfuscationSettings();
            settings.constEncryptSettings ??= new ConstEncryptionSettings();
            settings.removeConstFieldSettings ??= new RemoveConstFieldSettings();
            settings.evalStackObfusSettings ??= new EvalStackObfuscationSettings();
            settings.fieldEncryptSettings ??= new FieldEncryptionSettings();
            settings.callObfusSettings ??= new CallObfuscationSettings();
            settings.exprObfusSettings ??= new ExprObfuscationSettings();
            settings.controlFlowObfusSettings ??= new ControlFlowObfuscationSettings();
            settings.garbageCodeGenerationSettings ??= new GarbageCodeGenerationSettings();
            settings.watermarkSettings ??= new WatermarkSettings();
            settings.polymorphicDllSettings ??= new PolymorphicDllSettings();
            settings.assemblySettings.assembliesToObfuscate = new[] { "WKY_SDK" };
            settings.assemblySettings.nonObfuscatedButReferencingObfuscatedAssemblies =
                new[] { "Whitebox.Runtime", "Assembly-CSharp" };
            if (!File.Exists("ProjectSettings/Obfuz.asset"))
            {
                settings.secretSettings.defaultStaticSecretKey = Guid.NewGuid().ToString("N");
                settings.secretSettings.defaultDynamicSecretKey = Guid.NewGuid().ToString("N");
                settings.encryptionVMSettings.codeGenerationSecretKey = Guid.NewGuid().ToString("N");
            }
            ObfuzSettings.Save();
            ObfuzMenu.GenerateEncryptionVM();
            ObfuzMenu.SaveSecretFile();
            Directory.CreateDirectory("Assets/Obfuz");
            File.Copy("Tools/WKYIntegration/ObfuzBootstrap.cs.txt", "Assets/Obfuz/ObfuzBootstrap.cs", true);

            var addressables = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var group = addressables.FindGroup("WKY SDK") ?? addressables.CreateGroup(
                "WKY SDK", false, false, false, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            schema.BuildPath.SetVariableByName(addressables, AddressableAssetSettings.kLocalBuildPath);
            schema.LoadPath.SetVariableByName(addressables, AddressableAssetSettings.kLocalLoadPath);
            var entry = addressables.CreateOrMoveEntry("cfbd09e5e54b907438baa39e45662d36", group);
            entry.address = "SDKPanel/CommonConfirmTipsPanel";
            EditorUtility.SetDirty(addressables);
            AssetDatabase.SaveAssets();

            File.WriteAllText(Phase, "validate");
            AssetDatabase.Refresh();
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
        }

        private static void ValidateWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += ValidateWhenReady; return; }
            if (!File.Exists(Phase) || File.ReadAllText(Phase) != "validate") return;
            File.WriteAllText(Phase, "validating");
            try
            {
                string output = Root + "/AndroidScripts";
                Directory.CreateDirectory(output);
                var result = PlayerBuildInterface.CompilePlayerScripts(new ScriptCompilationSettings
                { group = BuildTargetGroup.Android, target = BuildTarget.Android,
                  options = ScriptCompilationOptions.None }, output);
                if (result.assemblies == null || !result.assemblies.Any(x => x.EndsWith("WKY_SDK.dll")))
                    throw new InvalidOperationException("Android WKY_SDK assembly was not produced.");
                File.WriteAllText(Root + "/android-compile.txt", string.Join("\n", result.assemblies));
                AddressableAssetSettings.BuildPlayerContent(out var build);
                if (!string.IsNullOrEmpty(build.Error))
                    throw new InvalidOperationException("Addressables build: " + build.Error);
                File.WriteAllText(Root + "/addressables-build.txt", "SUCCESS");
                runner = ScriptableObject.CreateInstance<TestRunnerApi>();
                runner.RegisterCallbacks(new Results());
                runner.Execute(new ExecutionSettings(new Filter
                { testMode = TestMode.EditMode, assemblyNames = new[] { "WKY.Integration.EditorTests" } }));
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                File.WriteAllText(Root + "/validation-error.txt", error.ToString());
                File.WriteAllText(Phase, "failed");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                File.WriteAllText(Root + "/tests.xml", result.ToXml().OuterXml);
                File.WriteAllText(Phase, result.FailCount == 0 && result.PassCount > 0 ? "passed" : "failed");
                if (Application.isBatchMode) EditorApplication.Exit(result.FailCount == 0 && result.PassCount > 0 ? 0 : 1);
            }
        }
    }
}
