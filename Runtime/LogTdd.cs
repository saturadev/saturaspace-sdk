using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Text;
using UnityEngine;

namespace SaturaSpace
{

public static class LogTdd
{
    const int FlushEveryN = 256;
    const int StreamBufSize = 8192;

    static readonly string Dir = Path.Combine(RuntimeRoot(), ".mcp_logs");

    static readonly Dictionary<string, TagWriter> Writers = new(32);
    static readonly char[] InvalidChars = Path.GetInvalidFileNameChars();
    static bool _hooked;

    public static event Action<string, string> LineLogged;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Log(string tag, string message)
    {
        var w = GetWriter(tag);
        var s = w.Stream;
        s.Write("[F:");
        s.Write(Time.frameCount);
        s.Write(" T:");
        s.Write(Time.realtimeSinceStartup.ToString("F3"));
        s.Write("] ");
        s.WriteLine(message);
        if (++w.Pending >= FlushEveryN)
        {
            s.Flush();
            w.Pending = 0;
        }
        LineLogged?.Invoke(tag, message);
    }

    public static void LogRaw(string tag, int frame, float time, string message)
    {
        var w = GetWriter(tag);
        var s = w.Stream;
        s.Write("[F:");
        s.Write(frame);
        s.Write(" T:");
        s.Write(time.ToString("F3"));
        s.Write("] ");
        s.WriteLine(message);
        if (++w.Pending >= FlushEveryN)
        {
            s.Flush();
            w.Pending = 0;
        }
        LineLogged?.Invoke(tag, message);
    }

    public static void LogBatch(string tag, IList<string> messages)
    {
        if (messages == null || messages.Count == 0) return;
        var w = GetWriter(tag);
        var s = w.Stream;
        var frame = Time.frameCount;
        var time = Time.realtimeSinceStartup.ToString("F3");
        for (int i = 0; i < messages.Count; i++)
        {
            s.Write("[F:");
            s.Write(frame);
            s.Write(" T:");
            s.Write(time);
            s.Write("] ");
            s.WriteLine(messages[i]);
        }
        w.Pending += messages.Count;
        if (w.Pending >= FlushEveryN)
        {
            s.Flush();
            w.Pending = 0;
        }
        if (LineLogged != null)
            for (int i = 0; i < messages.Count; i++)
                LineLogged.Invoke(tag, messages[i]);
    }

    public static void Flush()
    {
        foreach (var w in Writers.Values)
        {
            w.Stream.Flush();
            w.Pending = 0;
        }
    }

    public static void Clear()
    {
        Shutdown();
        try
        {
            if (Directory.Exists(Dir))
                Directory.Delete(Dir, true);
        }
        catch (IOException) { }
    }

    public static void Clear(string tag)
    {
        var safe = Sanitize(tag);
        if (Writers.TryGetValue(safe, out var w))
        {
            w.Dispose();
            Writers.Remove(safe);
        }
        var path = Path.Combine(Dir, safe + ".log");
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException) { }
    }

    public static string LogDirectory => Dir;

    const string TagLog = "_log";
    const string TagWarn = "_warn";
    const string TagError = "_error";

    static void OnUnityLog(string message, string stackTrace, LogType type)
    {
        if (message != null && message.Length > 2000)
            message = message.Substring(0, 2000) + "...(truncated)";

        char severity;
        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
            case LogType.Assert:
                severity = 'E';
                Log(TagError, message);
                if (!string.IsNullOrEmpty(stackTrace))
                {
                    int nl = stackTrace.IndexOf('\n');
                    var firstLine = nl > 0 ? stackTrace.Substring(0, nl).TrimEnd() : stackTrace.TrimEnd();
                    if (firstLine.Length > 0)
                        Log(TagError, "  at " + firstLine);
                }
                break;
            case LogType.Warning:
                severity = 'W';
                Log(TagWarn, message);
                break;
            default:
                severity = 'L';
                Log(TagLog, message);
                break;
        }

        WriteConsole(severity, message, severity == 'E' ? stackTrace : null);
    }

    const string ConsoleFile = "console.log";
    const int ConsoleQueueCap = 50000;
    const int ConsoleFlushMs = 50;
    static StreamWriter _console;
    static readonly ConcurrentQueue<string> _consoleQueue = new ConcurrentQueue<string>();
    static Thread _consoleThread;
    static volatile bool _consoleRun;
    static int _consoleDropped;

    static void StartConsole()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            _console = new StreamWriter(Path.Combine(Dir, ConsoleFile), append: true,
                new UTF8Encoding(false), StreamBufSize)
            {
                AutoFlush = false,
                NewLine = "\n"
            };
            _consoleRun = true;
            _consoleThread = new Thread(ConsoleLoop) { IsBackground = true, Name = "LogTdd.Console" };
            _consoleThread.Start();
        }
        catch { _console = null; }
    }

    static void WriteConsole(char severity, string message, string stackTrace)
    {
        try
        {
            if (_consoleThread == null) StartConsole();
            if (_console == null) return;
            if (_consoleQueue.Count >= ConsoleQueueCap)
            {
                Interlocked.Increment(ref _consoleDropped);
                return;
            }
            var sb = new StringBuilder(96);
            sb.Append("[F:").Append(Time.frameCount)
              .Append(" T:").Append(Time.realtimeSinceStartup.ToString("F3"))
              .Append("] ").Append(severity).Append(' ').Append(message ?? string.Empty).Append('\n');
            if (!string.IsNullOrEmpty(stackTrace))
            {
                var stackLines = stackTrace.Split('\n');
                for (int i = 0; i < stackLines.Length; i++)
                {
                    var ln = stackLines[i].TrimEnd();
                    if (ln.Length == 0) continue;
                    sb.Append("    ").Append(ln).Append('\n');
                }
            }
            _consoleQueue.Enqueue(sb.ToString());
        }
        catch { }
    }

    static void ConsoleLoop()
    {
        while (_consoleRun)
        {
            if (DrainConsole()) { try { _console.Flush(); } catch { } }
            Thread.Sleep(ConsoleFlushMs);
        }
        try { if (DrainConsole()) _console.Flush(); } catch { }
    }

    static bool DrainConsole()
    {
        bool wrote = false;
        int dropped = Interlocked.Exchange(ref _consoleDropped, 0);
        if (dropped > 0)
        {
            try
            {
                _console.Write("[F:-1 T:0.000] W [console] dropped ");
                _console.Write(dropped);
                _console.Write(" line(s) during a log burst\n");
                wrote = true;
            }
            catch { }
        }
        while (_consoleQueue.TryDequeue(out var s))
        {
            try { _console.Write(s); wrote = true; } catch { }
        }
        return wrote;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook()
    {
        if (_hooked) return;
        _hooked = true;
        Application.logMessageReceived += OnUnityLog;
        Application.quitting += Shutdown;
        AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
        WriteConsole('P', "Entered Play Mode", null);
    }

    static void OnDomainUnload(object sender, EventArgs e) => Shutdown();

    static void Shutdown()
    {
        Application.logMessageReceived -= OnUnityLog;
        foreach (var w in Writers.Values)
            w.Dispose();
        Writers.Clear();
        _consoleRun = false;
        var th = _consoleThread;
        _consoleThread = null;
        if (th != null) { try { th.Join(500); } catch { } }
        if (_console != null)
        {
            try { DrainConsole(); _console.Flush(); } catch { }
            try { _console.Dispose(); } catch { }
            _console = null;
        }
        _hooked = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static TagWriter GetWriter(string tag)
    {
        var safe = Sanitize(tag);
        if (Writers.TryGetValue(safe, out var w))
            return w;
        Directory.CreateDirectory(Dir);
        var path = Path.Combine(Dir, safe + ".log");
        w = new TagWriter(path);
        Writers[safe] = w;
        return w;
    }

    static string Sanitize(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return "_default";

        int firstBad = -1;
        for (int i = 0; i < tag.Length && firstBad < 0; i++)
            for (int j = 0; j < InvalidChars.Length; j++)
                if (tag[i] == InvalidChars[j]) { firstBad = i; break; }
        if (firstBad < 0) return tag;

        var sb = new StringBuilder(tag.Length);
        sb.Append(tag, 0, firstBad);
        for (int i = firstBad; i < tag.Length; i++)
        {
            var c = tag[i];
            bool bad = false;
            for (int j = 0; j < InvalidChars.Length; j++)
            {
                if (c == InvalidChars[j]) { bad = true; break; }
            }
            sb.Append(bad ? '_' : c);
        }
        return sb.ToString();
    }

    static string GetArg(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name) return args[i + 1];
        return null;
    }

    static string GetProjectRoot()
    {
        var over = GetArg("-mcpRoot");
        if (!string.IsNullOrEmpty(over))
            return over;
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
        return Application.persistentDataPath;
#else
        return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
#endif
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

    sealed class TagWriter : IDisposable
    {
        public readonly StreamWriter Stream;
        public int Pending;

        public TagWriter(string path)
        {
            Stream = new StreamWriter(path, append: true,
                Encoding.UTF8, StreamBufSize)
            {
                AutoFlush = false,
                NewLine = "\n"
            };
        }

        public void Dispose()
        {
            try { Stream?.Flush(); } catch { }
            try { Stream?.Dispose(); } catch { }
        }
    }
}
}
