using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using LiveSplit.Model;
using LiveSplit.Options;

namespace LiveSplit.Server.Commands;
internal class ControlCommands : ICommands
{
    private CommandServer server;
    public IEnumerable<KeyValuePair<string, Func<string[], string>>> GetCommands(CommandServer server)
    {
        this.server = server;
        yield return new KeyValuePair<string, Func<string[], string>>("ping", Ping);
        yield return new KeyValuePair<string, Func<string[], string>>("setcomparison", SetComparison);
        yield return new KeyValuePair<string, Func<string[], string>>("switchto", SwitchTo);
        yield return new KeyValuePair<string, Func<string[], string>>("setsplitname", SetSplitName);
        yield return new KeyValuePair<string, Func<string[], string>>("setcurrentsplitname", SetSplitName);
        yield return new KeyValuePair<string, Func<string[], string>>("getcustomvariablevalue", GetCustomVariableValue);
        yield return new KeyValuePair<string, Func<string[], string>>("setcustomvariable", SetCustomVariable);
        yield return new KeyValuePair<string, Func<string[], string>>("getattemptcount", GetAttemptCount);
        yield return new KeyValuePair<string, Func<string[], string>>("getcompletedcount", GetCompletedCount);
    }

    private string Ping(string[] args)
    {
        return "pong";
    }

    private string SetComparison(string[] args)
    {
        server.State.CurrentComparison = args[1];
        return null;
    }

    private string SwitchTo(string[] args)
    {
        switch (args[1])
        {
            case "gametime":
                server.State.CurrentTimingMethod = TimingMethod.GameTime;
                break;
            case "realtime":
                server.State.CurrentTimingMethod = TimingMethod.RealTime;
                break;
        }
        return null;
    }

    private string SetSplitName(string[] args)
    {
        if (args.Length < 2)
        {
            Log.Error($"[Server] Command setsplitname incorrect usage: missing one or more arguments.");
            return null;
        }
        string[] options = args[1].Split([' '], 2);
        if (options.Length < 2)
        {
            Log.Error($"[Server] Command setsplitname incorrect usage: missing one or more arguments.");
            return null;
        }
        if (!int.TryParse(options[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
        {
            Log.Error($"[Server] Could not parse {options[0]} as a split index while setting split name.");
            return null;
        }
        string title = options[1];
        if (index >= 0 && index < server.State.Run.Count)
        {
            server.State.Run[index].Name = title;
            server.State.Run.HasChanged = true;
        }
        else
        {
            Log.Warning($"[Sever] Split index {index} out of bounds for command setsplitname");
        }
        return null;
    }

    private string GetCustomVariableValue(string[] args)
    {
        string value = server.State.Run.Metadata.CustomVariableValue(args[1]);
        // make sure response isn't null or empty, and doesn't contain line endings
        return string.IsNullOrEmpty(value) ? "-" : Regex.Replace(value, @"\r\n?|\n", " ");
    }

    private string SetCustomVariable(string[] args)
    {
        if (args.Length < 2)
        {
            Log.Error($"[Server] Command setcustomvariable incorrect usage: missing one or more arguments.");
            return null;
        }
        string[] options;
        try
        {
            options = JsonSerializer.Deserialize<string[]>(args[1]);
        }
        catch (Exception e)
        {
            Log.Error(e);
            Log.Error($"[Server] Failed to parse JSON: {args[1]}");
            return null;
        }
        if (options == null || options.Length < 2)
        {
            Log.Error($"[Server] Command setcustomvariable incorrect usage: missing one or more arguments.");
            return null;
        }
        server.State.Run.Metadata.SetCustomVariable(options[0], options[1]);
        return null;
    }

    private string GetAttemptCount(string[] args)
    {
        return server.State.Run.AttemptCount.ToString();
    }

    private string GetCompletedCount(string[] args)
    {
        return server.State.Run.AttemptHistory.Count(x => x.Time.RealTime != null).ToString();
    }

}
