using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace SaturaSpace
{

public abstract class TddScenario : MonoBehaviour
{
    public static int PlayerIndex { get; internal set; } = 0;

    public static int PlayerCount { get; internal set; } = 1;

    public static string Role { get; internal set; } = "host";

    public static bool IsHost => Role == "host";

    public virtual float StallTimeoutSeconds => 30f;

    public abstract IEnumerator Run();
}

static class TddScenarioRunner
{
    const string TriggerFile = ".mcp_scenario.json";
    const string ResultFileMain = ".mcp_scenario_results.json";

    [Serializable]
    class ScenarioTrigger
    {
        public string scenario = "";
        public int playerIndex = 0;
        public int playerCount = 1;
        public string role = "host";
    }

    [Serializable]
    class ScenarioResult
    {
        public string status = "";
        public string error = "";
        public string scenario = "";
        public string side = "";
        public long timestamp;
        public int steps;
        public int frame;
        public string pendingYield = "";
        public float pendingSeconds;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        string root = RuntimeRoot();

        string side = "main";
        string resultPath = Path.Combine(root, ResultFileMain);

        string scenarioName = GetArg("-mcpScenario");
        if (string.IsNullOrEmpty(scenarioName))
        {
            string triggerPath = Path.Combine(root, TriggerFile);
            if (!File.Exists(triggerPath)) return;
            string json;
            try
            {
                json = File.ReadAllText(triggerPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TddScenarioRunner] ({side}) Failed to read trigger: {e.Message}");
                return;
            }
            var trigger = JsonUtility.FromJson<ScenarioTrigger>(json);
            scenarioName = trigger?.scenario;
            if (trigger != null)
            {
                TddScenario.PlayerIndex = trigger.playerIndex;
                TddScenario.PlayerCount = trigger.playerCount;
                TddScenario.Role = string.IsNullOrEmpty(trigger.role) ? "host" : trigger.role;
                side = TddScenario.Role;
            }
        }
        if (string.IsNullOrEmpty(scenarioName)) return;

        Debug.Log($"[TddScenarioRunner] ({side}) Starting scenario: {scenarioName}");

        Type scenarioType = FindType(scenarioName);
        if (scenarioType == null)
        {
            Debug.LogError($"[TddScenarioRunner] ({side}) Type not found: {scenarioName}");
            WriteResult(resultPath, scenarioName, side, "error", $"Type not found: {scenarioName}");
            return;
        }

        if (!typeof(TddScenario).IsAssignableFrom(scenarioType))
        {
            Debug.LogError($"[TddScenarioRunner] ({side}) {scenarioName} does not extend TddScenario");
            WriteResult(resultPath, scenarioName, side, "error", $"{scenarioName} does not extend TddScenario");
            return;
        }

        var go = new GameObject($"[TddScenario:{scenarioName}]");
        UnityEngine.Object.DontDestroyOnLoad(go);
        var instance = (TddScenario)go.AddComponent(scenarioType);
        var host = go.AddComponent<TddCoroutineHost>();
        host.Begin(instance, resultPath, scenarioName, side);
        host.StartCoroutine(RunWrapper(instance, host, go, resultPath, scenarioName, side));
    }

    static IEnumerator RunWrapper(TddScenario scenario, TddCoroutineHost host, GameObject go, string resultPath, string scenarioName, string side)
    {
        yield return null;

        string error = null;
        var stack = new Stack<IEnumerator>();

        try
        {
            var enumerator = scenario.Run();
            if (enumerator != null) stack.Push(enumerator);
        }
        catch (Exception ex)
        {
            error = ex.ToString();
        }

        while (error == null && stack.Count > 0)
        {
            bool moveNext;
            try
            {
                moveNext = stack.Peek().MoveNext();
            }
            catch (Exception ex)
            {
                error = ex.ToString();
                break;
            }
            if (!moveNext)
            {
                stack.Pop();
                host.NoteStep();
                continue;
            }
            var current = stack.Peek().Current;
            if (current is IEnumerator nested && !(current is CustomYieldInstruction))
            {
                stack.Push(nested);
                continue;
            }
            host.NotePending(current);
            yield return current;
            host.NoteStep();
        }

        host.Finish();
        LogTdd.Flush();

        string status = error == null ? "completed" : "error";
        WriteResult(resultPath, scenarioName, side, status, error ?? "", host.Steps);
        Debug.Log($"[TddScenarioRunner] ({side}) Scenario {scenarioName} finished: {status}");

        UnityEngine.Object.Destroy(go);

        if (!Application.isEditor)
            Application.Quit(status == "completed" ? 0 : 1);
    }

    internal static void WriteResult(string resultPath, string scenarioName, string side, string status, string error,
                                     int steps = 0, string pendingYield = "", float pendingSeconds = 0f)
    {
        var result = new ScenarioResult
        {
            status = status,
            error = error,
            scenario = scenarioName,
            side = side,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            steps = steps,
            frame = Time.frameCount,
            pendingYield = pendingYield ?? "",
            pendingSeconds = pendingSeconds
        };
        try
        {
            File.WriteAllText(resultPath, JsonUtility.ToJson(result, true));
        }
        catch (Exception e)
        {
            if (status != "running")
                Debug.LogError($"[TddScenarioRunner] ({side}) Failed to write result: {e.Message}");
        }
    }

    static Type FindType(string typeName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var type = asm.GetType(typeName, false, false);
                if (type != null) return type;
            }
            catch { }
        }
        return null;
    }

    static string GetProjectRoot()
    {
        var over = GetArg("-mcpRoot");
        if (!string.IsNullOrEmpty(over))
            return over;

        return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    }

    static string RuntimeRoot()
    {
        var root = GetProjectRoot();
        if (string.IsNullOrEmpty(GetArg("-mcpRoot")) && Application.isEditor)
        {
            var dir = Path.Combine(root, ".sspace");
            try { Directory.CreateDirectory(dir); } catch { }
            return dir;
        }
        return root;
    }

    static string GetArg(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name) return args[i + 1];
        return null;
    }
}

class TddCoroutineHost : MonoBehaviour
{
    static readonly FieldInfo WaitSecondsField =
        typeof(WaitForSeconds).GetField("m_Seconds", BindingFlags.Instance | BindingFlags.NonPublic);

    string _resultPath;
    string _name;
    string _side;
    float _stallLimit;
    float _lastProgress;
    float _pendingSince;
    float _pendingBudget;
    string _pendingYield = "";
    int _steps;
    bool _running;
    float _lastHeartbeat;
    float _lastTick;
    readonly Queue<string> _recentErrors = new Queue<string>();

    public int Steps => _steps;

    public void Begin(TddScenario scenario, string resultPath, string name, string side)
    {
        _resultPath = resultPath;
        _name = name;
        _side = side;
        _stallLimit = Mathf.Max(1f, scenario.StallTimeoutSeconds);
        _lastProgress = Time.realtimeSinceStartup;
        _pendingSince = _lastProgress;
        _running = true;
        LogTdd.LineLogged += OnTddLine;
    }

    public void NoteStep()
    {
        _steps++;
        _lastProgress = Time.realtimeSinceStartup;
        _pendingSince = _lastProgress;
        _pendingYield = "";
        _pendingBudget = 0f;
    }

    public void NotePending(object yielded)
    {
        _pendingYield = Describe(yielded);
        _pendingSince = Time.realtimeSinceStartup;
        _pendingBudget = TimedWait(yielded);
    }

    public void Finish()
    {
        if (!_running) return;
        _running = false;
        LogTdd.LineLogged -= OnTddLine;
    }

    void OnDestroy() => Finish();

    void OnTddLine(string tag, string message)
    {
        if (tag == "_error")
        {
            if (_recentErrors.Count >= 8) _recentErrors.Dequeue();
            _recentErrors.Enqueue(message);
            return;
        }
        if (tag == "_log" || tag == "_warn") return;
        _lastProgress = Time.realtimeSinceStartup;
    }

    void Update()
    {
        if (!_running) return;
        float now = Time.realtimeSinceStartup;
        if (_lastTick > 0f && now - _lastTick > 2f)
        {
            _lastProgress += now - _lastTick;
            _pendingSince += now - _lastTick;
        }
        _lastTick = now;
        if (now - _lastProgress > _stallLimit + _pendingBudget)
        {
            Abort(now - _lastProgress);
            return;
        }
        if (now - _lastHeartbeat >= 1f)
        {
            _lastHeartbeat = now;
            TddScenarioRunner.WriteResult(_resultPath, _name, _side, "running", "",
                                          _steps, _pendingYield, now - _pendingSince);
        }
    }

    void Abort(float stalledFor)
    {
        Finish();
        StopAllCoroutines();

        var sb = new StringBuilder();
        sb.Append($"STALLED: no scenario progress (coroutine step or LogTdd line) for {stalledFor:F0}s ");
        sb.Append($"(limit {_stallLimit:F0}s — override StallTimeoutSeconds for legitimately long quiet waits).");
        sb.Append($"\nWedged after step {_steps}, waiting on {(_pendingYield.Length == 0 ? "next frame" : _pendingYield)} ");
        sb.Append($"for {Time.realtimeSinceStartup - _pendingSince:F0}s (frame {Time.frameCount}).");
        if (_recentErrors.Count > 0)
        {
            sb.Append("\nErrors logged during the run (an exception outside the scenario coroutine often breaks the condition it waits on):");
            foreach (var e in _recentErrors)
                sb.Append("\n  ").Append(e);
        }
        var error = sb.ToString();

        LogTdd.Flush();
        TddScenarioRunner.WriteResult(_resultPath, _name, _side, "error", error, _steps, _pendingYield);
        Debug.LogError($"[TddScenarioRunner] ({_side}) Scenario {_name} aborted: {error}");

        Destroy(gameObject);

        if (!Application.isEditor)
            Application.Quit(1);
    }

    static string Describe(object yielded)
    {
        if (yielded == null) return "";
        if (yielded is WaitForSeconds ws)
        {
            var s = WaitSecondsField?.GetValue(ws);
            return s is float f ? $"WaitForSeconds({f:F1})" : "WaitForSeconds";
        }
        if (yielded is WaitForSecondsRealtime wsr)
            return $"WaitForSecondsRealtime({wsr.waitTime:F1})";
        return yielded.GetType().Name;
    }

    static float TimedWait(object yielded)
    {
        if (yielded is WaitForSeconds ws && WaitSecondsField?.GetValue(ws) is float f)
            return Mathf.Max(0f, f);
        if (yielded is WaitForSecondsRealtime wsr)
            return Mathf.Max(0f, wsr.waitTime);
        return 0f;
    }
}
}
