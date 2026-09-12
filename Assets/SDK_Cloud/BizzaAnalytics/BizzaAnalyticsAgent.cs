using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

public enum BizzaAnalyticsInitializationState
{
  Uninitialized = 0,
  Loading = 1,
  Ready = 2,
  Disabled = 3,
  Failed = 4,
}

[DisallowMultipleComponent]
public sealed class BizzaAnalyticsAgent : MonoBehaviour
{
  private const string HostObjectName = "[BizzaAnalyticsHost]";
  private const int MaxPendingOperations = 256;
  private const int MaxActiveLoadStages = 64;
  private const string GameStartEventName = "game_start";
  private const string GameLoadStageStartEventName = "game_load_stage_start";
  private const string GameLoadStageCompleteEventName = "game_load_stage_complete";
  private const string GameLoadCompleteEventName = "game_load_complete";

  private static BizzaAnalyticsAgent _instance;

  private readonly List<PendingOperation> _pendingOperations = new List<PendingOperation>();
  private readonly Dictionary<string, LoadStageState> _activeLoadStages
    = new Dictionary<string, LoadStageState>();
  private BizzaAnalyticsRuntime _runtime;
  private Coroutine _initCoroutine;
  private string _configuredAppId = string.Empty;
  private string _lastError = string.Empty;
  private bool _gameStartAccepted;
  private bool _gameLoadCompleteAccepted;
  private string _launchId = string.Empty;
  private string _launchStartedAt = string.Empty;
  private double _launchStartedRealtime;
  private int _loadStageSequence;

  public static BizzaAnalyticsAgent Instance => _instance;
  public static bool IsCreated => _instance != null;

  public BizzaAnalyticsInitializationState InitializationState { get; private set; }
    = BizzaAnalyticsInitializationState.Uninitialized;

  public string InitializedAppId => _configuredAppId;
  public bool IsReady => _runtime != null && _runtime.IsReady;
  public bool IsReportingEnabled => _runtime != null && _runtime.IsReportingEnabled;
  public bool IsDisabled => InitializationState == BizzaAnalyticsInitializationState.Disabled
    || InitializationState == BizzaAnalyticsInitializationState.Failed;
  public string LastError => !string.IsNullOrEmpty(_lastError)
    ? _lastError
    : (_runtime?.LastError ?? string.Empty);

  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
  private static void CreateRuntimeAgent()
  {
    EnsureAgent().EnsureGameStartTracked();
  }

  private static BizzaAnalyticsAgent EnsureAgent()
  {
    if (_instance != null)
    {
      return _instance;
    }

    GameObject host = new GameObject(HostObjectName);
    DontDestroyOnLoad(host);
    return host.AddComponent<BizzaAnalyticsAgent>();
  }

  private void Awake()
  {
    if (_instance != null && _instance != this)
    {
      Debug.LogError("[BizzaAnalytics] Duplicate agent detected. The duplicate component was ignored.");
      Destroy(this);
      return;
    }

    _instance = this;
    DontDestroyOnLoad(gameObject);
  }

  private void Update()
  {
    _runtime?.Tick(Time.unscaledDeltaTime);
  }

  private void OnApplicationPause(bool pauseStatus)
  {
    if (pauseStatus)
    {
      _runtime?.FlushLifecycleNow("application_pause");
    }
  }

  private void OnApplicationQuit()
  {
    _runtime?.FlushLifecycleNow("application_quit");
  }

  private void OnDestroy()
  {
    if (_instance == this)
    {
      _runtime?.FlushLifecycleNow("agent_destroy");
      _instance = null;
    }
  }

  private bool Initialize()
  {
    return StartInitializationIfNeeded();
  }

  private bool StartInitializationIfNeeded()
  {
    if (InitializationState == BizzaAnalyticsInitializationState.Loading
      || InitializationState == BizzaAnalyticsInitializationState.Ready)
    {
      return true;
    }

    if (InitializationState == BizzaAnalyticsInitializationState.Disabled
      || InitializationState == BizzaAnalyticsInitializationState.Failed)
    {
      return false;
    }

    _lastError = string.Empty;
    InitializationState = BizzaAnalyticsInitializationState.Loading;

    if (_initCoroutine == null)
    {
      _initCoroutine = StartCoroutine(LoadConfigAndInitialize());
    }

    return true;
  }

  public bool BTrack(string eventName, Dictionary<string, object> data)
  {
    if (_runtime != null && _runtime.IsReady)
    {
      _runtime.Track(eventName, data);
      return true;
    }

    bool accepted = QueueOperation(PendingOperation.Track(eventName, data));
    if (accepted)
    {
      Initialize();
    }
    return accepted;
  }

  public bool BUserProp(Dictionary<string, object> data)
  {
    if (_runtime != null && _runtime.IsReady)
    {
      _runtime.UpdateUserProperties(data);
      return true;
    }

    bool accepted = QueueOperation(PendingOperation.UserProp(data));
    if (accepted)
    {
      Initialize();
    }
    return accepted;
  }

  public static void Track(string eventName, Dictionary<string, object> data = null)
  {
    EnsureAgent().BTrack(eventName, data);
  }

  public static void UserProp(Dictionary<string, object> data)
  {
    EnsureAgent().BUserProp(data);
  }

  /// <summary>
  /// Reports that all game loading flows have completed and the player can enter gameplay.
  /// The event is accepted once per process and shares launch_id with game_start.
  /// </summary>
  public static bool MarkGameLoadComplete()
  {
    return EnsureAgent().TrackGameLoadComplete();
  }

  /// <summary>
  /// Reports the start of one game-loading stage and returns its unique stageId.
  /// Parallel stages and repeated stage names are supported.
  /// </summary>
  public static string MarkGameLoadStageStart(string stageName)
  {
    return EnsureAgent().TrackGameLoadStageStart(stageName);
  }

  /// <summary>
  /// Reports the completion result of a stage created by MarkGameLoadStageStart.
  /// </summary>
  public static bool MarkGameLoadStageComplete(
    string stageId,
    bool success = true,
    string failureReason = null)
  {
    return EnsureAgent().TrackGameLoadStageComplete(stageId, success, failureReason);
  }

  internal Coroutine RunCoroutine(IEnumerator routine)
  {
    return StartCoroutine(routine);
  }

  private IEnumerator LoadConfigAndInitialize()
  {
    string configPath = BizzaAnalyticsConfigSerializer.GetConfigPath();
    using (UnityWebRequest request = UnityWebRequest.Get(configPath))
    {
      yield return request.SendWebRequest();

      if (request.result != UnityWebRequest.Result.Success)
      {
        DisableWithError(
          "Analytics 初始化失败：读取配置文件失败。Path=" + configPath
          + " Error=" + request.error);
        _initCoroutine = null;
        yield break;
      }

      byte[] data = request.downloadHandler == null ? null : request.downloadHandler.data;
      BizzaAnalyticsConfig config;
      string deserializeError;
      if (!BizzaAnalyticsConfigSerializer.TryDeserialize(data, out config, out deserializeError))
      {
        DisableWithError("Analytics 初始化失败：配置文件解密或校验失败。" + deserializeError);
        _initCoroutine = null;
        yield break;
      }

      config.appId = config.appId.Trim();
      _configuredAppId = config.appId;
      string configError;
      if (!BizzaAnalyticsConfigSerializer.ValidateConfig(config, out configError))
      {
        DisableWithError("Analytics 初始化失败：配置字段不完整或无效。" + configError);
        _initCoroutine = null;
        yield break;
      }

      _runtime = new BizzaAnalyticsRuntime(this, config);
      if (_runtime == null || !_runtime.IsReady)
      {
        DisableWithError(
          "Analytics 初始化失败：Runtime 未就绪。"
          + (_runtime == null ? string.Empty : _runtime.LastError));
        _runtime = null;
        _initCoroutine = null;
        yield break;
      }

      InitializationState = BizzaAnalyticsInitializationState.Ready;
      _lastError = string.Empty;
      _initCoroutine = null;
      FlushPendingOperations();
    }
  }

  private bool QueueOperation(PendingOperation operation)
  {
    if (operation == null
      || InitializationState == BizzaAnalyticsInitializationState.Disabled
      || InitializationState == BizzaAnalyticsInitializationState.Failed)
    {
      return false;
    }

    if (_pendingOperations.Count >= MaxPendingOperations)
    {
      Debug.LogWarning(
        "[BizzaAnalytics] 初始化前事件缓存已达到上限，本次事件未被接受。请检查配置中的 AppId 和初始化错误。");
      return false;
    }

    _pendingOperations.Add(operation);
    return true;
  }

  private bool EnsureGameStartTracked()
  {
    if (_gameStartAccepted)
    {
      return true;
    }

    if (string.IsNullOrEmpty(_launchId))
    {
      _launchId = Guid.NewGuid().ToString("N");
      _launchStartedRealtime = Time.realtimeSinceStartupAsDouble;
      _launchStartedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
    }

    Dictionary<string, object> data = new Dictionary<string, object>(2)
    {
      { "launch_id", _launchId },
      { "launch_time", _launchStartedAt },
    };
    _gameStartAccepted = BTrack(GameStartEventName, data);
    LogLifecycleEvent(GameStartEventName, _gameStartAccepted, _launchId);
    return _gameStartAccepted;
  }

  private bool TrackGameLoadComplete()
  {
    if (_gameLoadCompleteAccepted)
    {
      LogLifecycleEvent(GameLoadCompleteEventName, false, _launchId, "duplicate");
      return false;
    }

    if (!EnsureGameStartTracked())
    {
      return false;
    }

    if (_activeLoadStages.Count > 0)
    {
      LogLifecycleEvent(GameLoadCompleteEventName, false, _launchId, "active_load_stages");
      return false;
    }

    double elapsedSeconds = Math.Max(
      0d,
      Time.realtimeSinceStartupAsDouble - _launchStartedRealtime);
    long durationMilliseconds = (long)Math.Round(
      elapsedSeconds * 1000d,
      MidpointRounding.AwayFromZero);
    Dictionary<string, object> data = new Dictionary<string, object>(3)
    {
      { "launch_id", _launchId },
      { "load_complete_time", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) },
      { "load_duration_ms", durationMilliseconds },
    };

    _gameLoadCompleteAccepted = BTrack(GameLoadCompleteEventName, data);
    LogLifecycleEvent(GameLoadCompleteEventName, _gameLoadCompleteAccepted, _launchId);
    return _gameLoadCompleteAccepted;
  }

  private string TrackGameLoadStageStart(string stageName)
  {
    string normalizedStageName = NormalizeLoadStageName(stageName);
    if (string.IsNullOrEmpty(normalizedStageName))
    {
      LogLoadStageEvent(GameLoadStageStartEventName, false, _launchId, null, null, 0, "invalid_stage_name");
      return string.Empty;
    }

    if (_gameLoadCompleteAccepted)
    {
      LogLoadStageEvent(GameLoadStageStartEventName, false, _launchId, null, normalizedStageName, 0, "game_load_complete_already_sent");
      return string.Empty;
    }

    if (!EnsureGameStartTracked() || _activeLoadStages.Count >= MaxActiveLoadStages)
    {
      LogLoadStageEvent(
        GameLoadStageStartEventName,
        false,
        _launchId,
        null,
        normalizedStageName,
        0,
        _activeLoadStages.Count >= MaxActiveLoadStages ? "too_many_active_stages" : "game_start_not_accepted");
      return string.Empty;
    }

    string stageId = Guid.NewGuid().ToString("N");
    int stageOrder = _loadStageSequence + 1;
    string startedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
    double startedRealtime = Time.realtimeSinceStartupAsDouble;
    Dictionary<string, object> data = new Dictionary<string, object>(5)
    {
      { "launch_id", _launchId },
      { "stage_id", stageId },
      { "stage_name", normalizedStageName },
      { "stage_order", stageOrder },
      { "stage_start_time", startedAt },
    };

    bool accepted = BTrack(GameLoadStageStartEventName, data);
    if (accepted)
    {
      _loadStageSequence = stageOrder;
      _activeLoadStages.Add(
        stageId,
        new LoadStageState(normalizedStageName, stageOrder, startedRealtime));
    }

    LogLoadStageEvent(
      GameLoadStageStartEventName,
      accepted,
      _launchId,
      stageId,
      normalizedStageName,
      stageOrder);
    return accepted ? stageId : string.Empty;
  }

  private bool TrackGameLoadStageComplete(
    string stageId,
    bool success,
    string failureReason)
  {
    string normalizedStageId = (stageId ?? string.Empty).Trim();
    LoadStageState stage;
    if (string.IsNullOrEmpty(normalizedStageId)
      || !_activeLoadStages.TryGetValue(normalizedStageId, out stage))
    {
      LogLoadStageEvent(
        GameLoadStageCompleteEventName,
        false,
        _launchId,
        normalizedStageId,
        null,
        0,
        "unknown_or_completed_stage_id");
      return false;
    }

    double elapsedSeconds = Math.Max(
      0d,
      Time.realtimeSinceStartupAsDouble - stage.StartedRealtime);
    long durationMilliseconds = (long)Math.Round(
      elapsedSeconds * 1000d,
      MidpointRounding.AwayFromZero);
    Dictionary<string, object> data = new Dictionary<string, object>(8)
    {
      { "launch_id", _launchId },
      { "stage_id", normalizedStageId },
      { "stage_name", stage.Name },
      { "stage_order", stage.Order },
      { "stage_complete_time", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) },
      { "stage_duration_ms", durationMilliseconds },
      { "success", success },
    };
    if (!success)
    {
      data["failure_reason"] = NormalizeFailureReason(failureReason);
    }

    bool accepted = BTrack(GameLoadStageCompleteEventName, data);
    if (accepted)
    {
      _activeLoadStages.Remove(normalizedStageId);
    }

    LogLoadStageEvent(
      GameLoadStageCompleteEventName,
      accepted,
      _launchId,
      normalizedStageId,
      stage.Name,
      stage.Order,
      success ? null : "failed");
    return accepted;
  }

  private static string NormalizeLoadStageName(string value)
  {
    string normalized = (value ?? string.Empty).Trim();
    return normalized.Length > 0 && normalized.Length <= 128
      ? normalized
      : string.Empty;
  }

  private static string NormalizeFailureReason(string value)
  {
    string normalized = (value ?? string.Empty).Trim();
    if (normalized.Length == 0)
    {
      return "unknown";
    }

    return normalized.Length <= 256 ? normalized : normalized.Substring(0, 256);
  }

  [System.Diagnostics.Conditional("UNITY_EDITOR")]
  [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
  private static void LogLoadStageEvent(
    string eventName,
    bool accepted,
    string launchId,
    string stageId,
    string stageName,
    int stageOrder,
    string reason = null)
  {
    Debug.Log(
      "[BizzaAnalytics] load_stage_event=" + eventName
      + " accepted=" + accepted
      + " launch_id=" + launchId
      + " stage_id=" + (stageId ?? string.Empty)
      + " stage_name=" + (stageName ?? string.Empty)
      + " stage_order=" + stageOrder
      + (string.IsNullOrEmpty(reason) ? string.Empty : " reason=" + reason));
  }

  [System.Diagnostics.Conditional("UNITY_EDITOR")]
  [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
  private static void LogLifecycleEvent(
    string eventName,
    bool accepted,
    string launchId,
    string reason = null)
  {
    Debug.Log(
      "[BizzaAnalytics] lifecycle_event=" + eventName
      + " accepted=" + accepted
      + " launch_id=" + launchId
      + (string.IsNullOrEmpty(reason) ? string.Empty : " reason=" + reason));
  }

  private void FlushPendingOperations()
  {
    if (_runtime == null || !_runtime.IsReady || _pendingOperations.Count == 0)
    {
      return;
    }

    for (int i = 0; i < _pendingOperations.Count; i++)
    {
      PendingOperation operation = _pendingOperations[i];
      if (operation.Kind == PendingOperationKind.Track)
      {
        _runtime.Track(operation.EventName, operation.Data);
      }
      else
      {
        _runtime.UpdateUserProperties(operation.Data);
      }
    }

    _pendingOperations.Clear();
  }

  private void DisableWithError(string error)
  {
    _runtime = null;
    _initCoroutine = null;
    _pendingOperations.Clear();
    InitializationState = BizzaAnalyticsInitializationState.Disabled;
    ReportError(error);
  }

  private void ReportError(string error)
  {
    _lastError = string.IsNullOrWhiteSpace(error) ? "Analytics 未知初始化错误。" : error;
    Debug.LogError("[BizzaAnalytics] " + _lastError);
  }

  private enum PendingOperationKind
  {
    Track,
    UserProp,
  }

  private sealed class LoadStageState
  {
    public readonly string Name;
    public readonly int Order;
    public readonly double StartedRealtime;

    public LoadStageState(string name, int order, double startedRealtime)
    {
      Name = name;
      Order = order;
      StartedRealtime = startedRealtime;
    }
  }

  private sealed class PendingOperation
  {
    public PendingOperationKind Kind;
    public string EventName;
    public Dictionary<string, object> Data;

    public static PendingOperation Track(string eventName, Dictionary<string, object> data)
    {
      return new PendingOperation
      {
        Kind = PendingOperationKind.Track,
        EventName = eventName,
        Data = CloneData(data),
      };
    }

    public static PendingOperation UserProp(Dictionary<string, object> data)
    {
      return new PendingOperation
      {
        Kind = PendingOperationKind.UserProp,
        Data = CloneData(data),
      };
    }

    private static Dictionary<string, object> CloneData(Dictionary<string, object> data)
    {
      return BizzaValueUtil.NormalizeDictionary(data);
    }
  }
}
