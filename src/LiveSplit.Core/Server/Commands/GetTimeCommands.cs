using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LiveSplit.Model;
using LiveSplit.TimeFormatters;

using static System.Windows.Forms.AxHost;

namespace LiveSplit.Server.Commands;
internal class GetTimeCommands : ICommands
{
    private CommandServer server;
    public IEnumerable<KeyValuePair<string, Func<string[], string>>> GetCommands(CommandServer server)
    {
        this.server = server;
        yield return new KeyValuePair<string, Func<string[], string>>("getdelta", GetDelta);
        yield return new KeyValuePair<string, Func<string[], string>>("getlastsplitname", GetPreviousSplitName);
        yield return new KeyValuePair<string, Func<string[], string>>("getcurrentsplitname", GetCurrentSplitName);
        yield return new KeyValuePair<string, Func<string[], string>>("getlastsplitname", GetPreviousSplitName);
        yield return new KeyValuePair<string, Func<string[], string>>("getprevioussplitname", GetPreviousSplitName);
        yield return new KeyValuePair<string, Func<string[], string>>("getlastsplittime", GetPreviousSplitTime);
        yield return new KeyValuePair<string, Func<string[], string>>("getprevioussplittime", GetPreviousSplitTime);
        yield return new KeyValuePair<string, Func<string[], string>>("getcomparisonsplittime", GetCurrentSplitTime);
        yield return new KeyValuePair<string, Func<string[], string>>("getcurrentsplittime", GetCurrentSplitTime);
        yield return new KeyValuePair<string, Func<string[], string>>("getcurrentrealtime", GetCurrentRealTime);
        yield return new KeyValuePair<string, Func<string[], string>>("getcurrentgametime", GetCurrentGameTime);
        yield return new KeyValuePair<string, Func<string[], string>>("getcurrenttime", GetCurrentTime);
        yield return new KeyValuePair<string, Func<string[], string>>("getfinaltime", GetFinalTime);
        yield return new KeyValuePair<string, Func<string[], string>>("getfinalsplittime", GetFinalTime);
        yield return new KeyValuePair<string, Func<string[], string>>("getpredictedtime", GetPredictedTime);
        yield return new KeyValuePair<string, Func<string[], string>>("getbestpossibletime", GetPredictedTime);
        yield return new KeyValuePair<string, Func<string[], string>>("getcurrenttimerphase", GetCurrentTimerPhase);
        yield return new KeyValuePair<string, Func<string[], string>>("gettimerphase", GetCurrentTimerPhase);
    }

    private string GetDelta(string[] args)
    {
        string comparison = args.Length > 1 ? args[1] : server.State.CurrentComparison;
        TimeSpan? delta = null;
        if (server.State.CurrentPhase is TimerPhase.Running or TimerPhase.Paused)
        {
            delta = LiveSplitStateHelper.GetLastDelta(server.State, server.State.CurrentSplitIndex, comparison, server.State.CurrentTimingMethod);
        }
        else if (server.State.CurrentPhase == TimerPhase.Ended)
        {
            delta = server.State.Run.Last().SplitTime[server.State.CurrentTimingMethod] - server.State.Run.Last().Comparisons[comparison][server.State.CurrentTimingMethod];
        }
        // Defaults to "-" when delta is null, such as when State.CurrentPhase == TimerPhase.NotRunning
        return server.TimeFormatter.Format(delta);
    }

    private string GetCurrentSplitName(string[] args)
    {
        if (server.State.CurrentSplit != null)
        {
            return server.State.CurrentSplit.Name;
        }
        else
        {
            return "-";
        }
    }

    private string GetPreviousSplitName(string[] args)
    {
        if (server.State.CurrentSplitIndex > 0)
        {
            return server.State.Run[server.State.CurrentSplitIndex - 1].Name;
        }
        else
        {
            return "-";
        }
    }

    private string GetPreviousSplitTime(string[] args)
    {
        if (server.State.CurrentSplitIndex > 0)
        {
            TimeSpan? time = server.State.Run[server.State.CurrentSplitIndex - 1].SplitTime[server.State.CurrentTimingMethod];
            return server.TimeFormatter.Format(time);
        }
        else
        {
            return "-";
        }
    }

    private string GetCurrentSplitTime(string[] args)
    {
        if (server.State.CurrentSplit != null)
        {
            string comparison = args.Length > 1 ? args[1] : server.State.CurrentComparison;
            TimeSpan? time = server.State.CurrentSplit.Comparisons[comparison][server.State.CurrentTimingMethod];
            return server.TimeFormatter.Format(time);
        }
        else
        {
            return "-";
        }
    }

    private string GetCurrentRealTime(string[] args)
    {
        return server.TimeFormatter.Format(server.GetCurrentTime(server.State, TimingMethod.RealTime));
    }

    private string GetCurrentTime(string[] args)
    {
        TimingMethod timingMethod = server.State.CurrentTimingMethod;
        if (timingMethod == TimingMethod.GameTime && !server.State.IsGameTimeInitialized)
        {
            timingMethod = TimingMethod.RealTime;
        }
        return server.TimeFormatter.Format(server.GetCurrentTime(server.State, timingMethod));
    }

    private string GetCurrentGameTime(string[] args)
    {
        TimingMethod timingMethod = TimingMethod.GameTime;
        if (!server.State.IsGameTimeInitialized)
        {
            timingMethod = TimingMethod.RealTime;
        }
        return server.TimeFormatter.Format(server.GetCurrentTime(server.State, timingMethod));
    }

    private string GetFinalTime(string[] args)
    {
        string comparison = args.Length > 1 ? args[1] : server.State.CurrentComparison;
        TimeSpan? time = (server.State.CurrentPhase == TimerPhase.Ended)
            ? server.State.CurrentTime[server.State.CurrentTimingMethod]
            : server.State.Run.Last().Comparisons[comparison][server.State.CurrentTimingMethod];
        return server.TimeFormatter.Format(time);
    }

    private string GetPredictedTime(string[] args)
    {
        string comparison;
        if (args[0].Equals("getbestpossibletime", StringComparison.OrdinalIgnoreCase))
        {
            comparison = LiveSplit.Model.Comparisons.BestSegmentsComparisonGenerator.ComparisonName;
        }
        else
        {
            comparison = args.Length > 1 ? args[1] : server.State.CurrentComparison;
        }
        TimeSpan? prediction = server.PredictTime(server.State, comparison);
        return server.TimeFormatter.Format(prediction);
    }

    private string GetCurrentTimerPhase(string[] args)
    {
        return server.State.CurrentPhase.ToString();
    }

}
