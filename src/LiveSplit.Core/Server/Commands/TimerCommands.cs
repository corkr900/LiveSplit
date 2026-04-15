using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LiveSplit.Model;
using LiveSplit.Options;

using static System.Windows.Forms.AxHost;

namespace LiveSplit.Server.Commands;
internal class TimerCommands : ICommands
{
    private CommandServer server;

    public IEnumerable<KeyValuePair<string, Func<string[], string>>> GetCommands(CommandServer server)
    {
        this.server = server;
        yield return new KeyValuePair<string, Func<string[], string>>("setgametime", SetGameTime);
        yield return new KeyValuePair<string, Func<string[], string>>("setloadingtimes", SetLoadingTimes);
        yield return new KeyValuePair<string, Func<string[], string>>("addloadingtimes", AddLoadingTimes);
        yield return new KeyValuePair<string, Func<string[], string>>("pausegametime", PauseGameTime);
        yield return new KeyValuePair<string, Func<string[], string>>("unpausegametime", UnpauseGameTime);
        yield return new KeyValuePair<string, Func<string[], string>>("alwayspausegametime", AlwaysPauseGameTime);
    }

    private string SetGameTime(string[] args)
    {
        try
        {
            TimeSpan? time = server.ParseTime(args[1]);
            server.State.SetGameTime(time);
        }
        catch (Exception e)
        {
            Log.Error(e);
            Log.Error($"[Server] Failed to parse time while setting game time: {args[1]}");
        }
        return null;
    }

    private string SetLoadingTimes(string[] args)
    {
        try {
            TimeSpan? time = server.ParseTime(args[1]);
            server.State.LoadingTimes = time ?? TimeSpan.Zero;
        }
        catch (Exception e)
        {
            Log.Error(e);
            Log.Error($"[Server] Failed to parse time while setting loading times: {args[1]}");
        }
        return null;
    }

    private string AddLoadingTimes(string[] args)
    {
        try
        {
            TimeSpan? time = server.ParseTime(args[1]);
            server.State.LoadingTimes = server.State.LoadingTimes.Add(time ?? TimeSpan.Zero);
        }
        catch (Exception e)
        {
            Log.Error(e);
            Log.Error($"[Server] Failed to parse time while adding loading times: {args[1]}");
        }
        return null;
    }

    private string PauseGameTime(string[] args)
    {
        server.State.IsGameTimePaused = true;
        return null;
    }

    private string UnpauseGameTime(string[] args)
    {
        server.AlwaysPauseGameTime = false;
        server.State.IsGameTimePaused = false;
        return null;
    }

    private string AlwaysPauseGameTime(string[] args)
    {
        server.AlwaysPauseGameTime = true;
        server.State.IsGameTimePaused = true;
        return null;
    }

}
