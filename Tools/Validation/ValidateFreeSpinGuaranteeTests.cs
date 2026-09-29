using System;
using System.Globalization;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

// Temporary Editor-only validation entry. Uses the installed public TestRunnerApi.
// SessionState survives the PlayMode test runner's domain reloads; InitializeOnLoad
// restores the callbacks because TestRunnerApi does not retain them across reloads.
[InitializeOnLoad]
public static class ValidateFreeSpinGuaranteeTests
{
    private static readonly string[] Fixtures = {
        "RecoveredFreeSpinGuaranteeTests", "RecoveredFreeSpinGuaranteeIntegrationTests",
        "RecoveredFreeSpinResultTests", "RecoveredFreeSpinEntryTests"
    };
    private const string Assembly = "Whitebox.PlayModeTests";
    private const string PendingKey = "DragonLegend.FreeSpinGuaranteeTests.Pending";
    private const string OutputKey = "DragonLegend.FreeSpinGuaranteeTests.Output";
    private const string CountKey = "DragonLegend.FreeSpinGuaranteeTests.ExpectedCount";
    private static TestRunnerApi api;
    private static ResultsCallback callbacks;

    static ValidateFreeSpinGuaranteeTests()
    {
        if (SessionState.GetBool(PendingKey, false)) EnsureCallbacks();
    }

    [MenuItem("Tools/Validation/Free Spin Guarantee Unit Tests")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Start Free Spin Guarantee validation outside Play mode.");
        if (SessionState.GetBool(PendingKey, false))
            throw new InvalidOperationException("Free Spin Guarantee unit test validation is already running.");

        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Artifacts/FreeSpinGuarantee"));
        Directory.CreateDirectory(output);
        SessionState.SetString(OutputKey, output);
        SessionState.SetInt(CountKey, 0);
        SessionState.SetBool(PendingKey, true);
        WriteStatus("discovering", 0, 0, 0, "");
        EnsureCallbacks();
        try
        {
            // This project's test asmdef has no Editor-only platform constraint,
            // so its public test list is PlayMode, even for synchronous [Test]s.
            api.RetrieveTestList(TestMode.PlayMode, root =>
            {
                try
                {
                    int expected = 0;
                    var groups = new string[Fixtures.Length];
                    for (int i = 0; i < Fixtures.Length; i++)
                    {
                        ITestAdaptor fixture = FindFixture(root, Fixtures[i]);
                        if (fixture == null || fixture.TestCaseCount == 0)
                            throw new InvalidOperationException("Missing PlayMode test fixture: " + Fixtures[i]);
                        expected += fixture.TestCaseCount;
                        groups[i] = "^" + Fixtures[i] + @"(?:\.|$)";
                    }
                    SessionState.SetInt(CountKey, expected);
                    WriteStatus("running", 0, 0, 0, "");
                    api.Execute(new ExecutionSettings(new Filter
                    {
                        testMode = TestMode.PlayMode,
                        assemblyNames = new[] { Assembly },
                        groupNames = groups
                    }));
                }
                catch (Exception error) { FailedToRun(error.ToString()); }
            });
        }
        catch (Exception error) { FailedToRun(error.ToString()); }
    }

    private static void EnsureCallbacks()
    {
        if (api == null)
        {
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.hideFlags = HideFlags.HideAndDontSave;
        }
        if (callbacks == null)
        {
            callbacks = new ResultsCallback();
            api.RegisterCallbacks(callbacks);
        }
    }

    private static ITestAdaptor FindFixture(ITestAdaptor node, string fixture)
    {
        if (node.FullName == fixture) return node;
        if (node.HasChildren)
            foreach (ITestAdaptor child in node.Children)
            {
                ITestAdaptor found = FindFixture(child, fixture);
                if (found != null) return found;
            }
        return null;
    }

    private static bool ContainsOnlyExpectedCases(ITestResultAdaptor node)
    {
        if (!node.HasChildren)
        {
            foreach (string fixture in Fixtures)
                if (node.FullName.StartsWith(fixture + ".", StringComparison.Ordinal)) return true;
            return false;
        }
        foreach (ITestResultAdaptor child in node.Children)
            if (!ContainsOnlyExpectedCases(child)) return false;
        return true;
    }

    private static void SaveXml(ITestResultAdaptor result)
    {
        string output = SessionState.GetString(OutputKey, "");
        Directory.CreateDirectory(output);
        int total = result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount;
        using (XmlWriter writer = XmlWriter.Create(Path.Combine(output, "unit-tests.xml"), new XmlWriterSettings { Indent = true }))
        {
            writer.WriteStartElement("test-run");
            writer.WriteAttributeString("testcasecount", total.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("total", total.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("result", result.ResultState);
            writer.WriteAttributeString("passed", result.PassCount.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("failed", result.FailCount.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("skipped", result.SkipCount.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("inconclusive", result.InconclusiveCount.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("duration", result.Duration.ToString(CultureInfo.InvariantCulture));
            result.ToXml().WriteTo(writer);
            writer.WriteEndElement();
        }
    }

    [Serializable]
    private sealed class Status
    {
        public string status, mode, error;
        public string[] fixtures;
        public int expected, passed, failed, skipped;
    }

    private static void WriteStatus(string state, int passed, int failed, int skipped, string error)
    {
        var status = new Status
        {
            status = state, fixtures = Fixtures, mode = "PlayMode",
            expected = SessionState.GetInt(CountKey, 0),
            passed = passed, failed = failed, skipped = skipped, error = error
        };
        File.WriteAllText(Path.Combine(SessionState.GetString(OutputKey, ""), "unit-tests-status.json"), JsonUtility.ToJson(status, true));
    }

    private static void Finish()
    {
        SessionState.SetBool(PendingKey, false);
        if (api != null && callbacks != null) api.UnregisterCallbacks(callbacks);
        callbacks = null;
        if (api != null) UnityEngine.Object.DestroyImmediate(api);
        api = null;
    }

    private static void FailedToRun(string message)
    {
        if (!SessionState.GetBool(PendingKey, false)) return;
        try { WriteStatus("ERROR", 0, 0, 0, message); }
        finally { Finish(); }
        Debug.LogError("FREE_SPIN_GUARANTEE_TESTS_ERROR: " + message);
    }

    private sealed class ResultsCallback : IErrorCallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
        public void OnError(string message) { FailedToRun(message); }
        public void RunFinished(ITestResultAdaptor result)
        {
            if (!SessionState.GetBool(PendingKey, false)) return;
            try
            {
                SaveXml(result);
                int expected = SessionState.GetInt(CountKey, 0);
                bool passed = expected > 0 && result.PassCount == expected && result.FailCount == 0
                    && result.SkipCount == 0 && result.InconclusiveCount == 0 && ContainsOnlyExpectedCases(result);
                WriteStatus(passed ? "PASS" : "FAIL", result.PassCount, result.FailCount,
                    result.SkipCount + result.InconclusiveCount, result.Message ?? "");
                Debug.Log(passed ? "FREE_SPIN_GUARANTEE_TESTS_PASS" : "FREE_SPIN_GUARANTEE_TESTS_FAIL");
                Finish();
            }
            catch (Exception error) { FailedToRun(error.ToString()); }
        }
    }
}
