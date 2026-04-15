using System;
using System.Collections.Generic;
using LiveSplit.Model;

namespace LiveSplit.Server.Commands;
internal class SplitCommands : ICommands
{
    private CommandServer server;

    public IEnumerable<KeyValuePair<string, Func<string[], string>>> GetCommands(CommandServer server)
    {
        this.server = server;
        yield return new KeyValuePair<string, Func<string[], string>>("startorsplit", StartOrSplit);
        yield return new KeyValuePair<string, Func<string[], string>>("split", Split);
        yield return new KeyValuePair<string, Func<string[], string>>("undosplit", UndoSplit);
        yield return new KeyValuePair<string, Func<string[], string>>("unsplit", UndoSplit);
        yield return new KeyValuePair<string, Func<string[], string>>("skipsplit", SkipSplit);
        yield return new KeyValuePair<string, Func<string[], string>>("pause", Pause);
        yield return new KeyValuePair<string, Func<string[], string>>("resume", Resume);
        yield return new KeyValuePair<string, Func<string[], string>>("reset", Reset);
        yield return new KeyValuePair<string, Func<string[], string>>("start", Start);
        yield return new KeyValuePair<string, Func<string[], string>>("starttimer", Start);
    }

    private string StartOrSplit(string[] args)
    {
        if (server.State.CurrentPhase == TimerPhase.Running)
        {
            server.Model.Split();
        }
        else
        {
            server.Model.Start();
        }
        return null;
    }

    private string Split(string[] args)
    {
        server.Model.Split();
        return null;
    }

    private string UndoSplit(string[] args)
    {
        server.Model.UndoSplit();
        return null;
    }

    private string SkipSplit(string[] args)
    {
        server.Model.SkipSplit();
        return null;
    }

    private string Pause(string[] args)
    {
        if (server.State.CurrentPhase != TimerPhase.Paused)
        {
            server.Model.Pause();
        }
        return null;
    }

    private string Resume(string[] args)
    {
        if (server.State.CurrentPhase == TimerPhase.Paused)
        {
            server.Model.Pause();
        }
        return null;
    }

    private string Reset(string[] args)
    {
        server.Model.Reset();
        return null;
    }

    private string Start(string[] args)
    {
        server.Model.Start();
        return null;
    }

}
