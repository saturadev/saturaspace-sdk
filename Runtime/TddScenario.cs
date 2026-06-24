using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SaturaSpace
{

/// <summary>
/// Base class for TDD scenario scripts.
/// Place scenario scripts in Assets/TddScenariosScripts/.
///
/// Usage:
///   public class MyScenario : TddScenario
///   {
///       public override IEnumerator Run()
///       {
///           var clickables = TddUI.GetClickables();
///           foreach (var c in clickables)
///               LogTdd.Log("ui", c.ToString());
///
///           yield return TddUI.ClickAndWait("Canvas/StartButton", 0.5f);
///           LogTdd.Log("result", TddUI.GetText("Canvas/ScoreText"));
///       }
///   }
/// </summary>
public abstract class TddScenario : MonoBehaviour
{
    /// <summary>Implement your scenario logic as a coroutine.</summary>
    public abstract IEnumerator Run();
}

/// <summary>
/// Runs a TddScenario automatically when Play Mode starts, when a trigger is present:
/// a .mcp_scenario.json file in the editor, or the -mcpScenario command-line arg in a player.
/// The result is written to .mcp_scenario_results.json. The trigger file is left in place;
/// cleanup happens externally before the next run.
/// </summary>
static class TddScenarioRunner
{
    const string TriggerFile = ".mcp_scenario.json";
    const string ResultFileMain = ".mcp_scenario_results.json";

    [Serializable]
    class ScenarioTrigger
    {
        public string scenario = "";
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

        // Player builds receive the scenario via command line (-mcpScenario <name>);
        // editor Play Mode uses the .mcp_scenario.json trigger file.
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
        // Wait one frame to let scene fully initialize
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
        // The player is intentionally left running after the scenario; it is stopped explicitly.
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
        // Player builds pass a writable output dir via -mcpRoot so results/logs land in a
        // readable location (the folder above Application.dataPath is inside the .app bundle).
        var over = GetArg("-mcpRoot");
        if (!string.IsNullOrEmpty(over))
            return over;

        return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    }

    // Editor: nest runtime files under .sspace so they stay out of the user's project tree.
    // Player builds (-mcpRoot) keep their own layout.
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

/// <summary>Minimal MonoBehaviour to host coroutines for the scenario runner.</summary>
class TddCoroutineHost : MonoBehaviour { }
}
