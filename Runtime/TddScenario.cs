using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SaturaSpace
{

public abstract class TddScenario : MonoBehaviour
{
    public static int PlayerIndex { get; internal set; } = 0;

    public static int PlayerCount { get; internal set; } = 1;

    public static string Role { get; internal set; } = "host";

    public static bool IsHost => Role == "host";

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
        host.StartCoroutine(RunWrapper(instance, go, resultPath, scenarioName, side));
    }

    static IEnumerator RunWrapper(TddScenario scenario, GameObject go, string resultPath, string scenarioName, string side)
    {
        yield return null;

        string error = null;
        IEnumerator enumerator;

        try
        {
            enumerator = scenario.Run();
        }
        catch (Exception ex)
        {
            error = ex.ToString();
            enumerator = null;
        }

        if (enumerator != null)
        {
            while (true)
            {
                bool moveNext;
                try
                {
                    moveNext = enumerator.MoveNext();
                }
                catch (Exception ex)
                {
                    error = ex.ToString();
                    break;
                }
                if (!moveNext) break;
                yield return enumerator.Current;
            }
        }

        LogTdd.Flush();

        string status = error == null ? "completed" : "error";
        WriteResult(resultPath, scenarioName, side, status, error ?? "");
        Debug.Log($"[TddScenarioRunner] ({side}) Scenario {scenarioName} finished: {status}");

        UnityEngine.Object.Destroy(go);

        if (!Application.isEditor)
            Application.Quit(status == "completed" ? 0 : 1);
    }

    static void WriteResult(string resultPath, string scenarioName, string side, string status, string error)
    {
        var result = new ScenarioResult
        {
            status = status,
            error = error,
            scenario = scenarioName,
            side = side,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        try
        {
            File.WriteAllText(resultPath, JsonUtility.ToJson(result, true));
        }
        catch (Exception e)
        {
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

class TddCoroutineHost : MonoBehaviour { }
}
