using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using LiveSplit.Model;
using LiveSplit.Options;
using LiveSplit.Server.Commands;
using LiveSplit.TimeFormatters;

using WebSocketSharp.Server;

namespace LiveSplit.Server;

public class CommandServer
{
    public TcpListener Server { get; set; }
    public WebSocketServer WsServer { get; set; }
    public List<Connection> PipeConnections { get; set; }
    public List<TcpConnection> TcpConnections { get; set; }
    public ServerStateType ServerState { get; protected set; } = ServerStateType.Off;
    private CommandHandler commandHandler { get; set; }

    public LiveSplitState State { get; protected set; }
    public Form Form { get; protected set; }
    public TimerModel Model { get; protected set; }
    public ITimeFormatter TimeFormatter { get; protected set; }
    protected NamedPipeServerStream WaitingServerPipe { get; set; }

    public bool AlwaysPauseGameTime { get; set; }

    public CommandServer(LiveSplitState state)
    {
        Model = new TimerModel();
        PipeConnections = [];
        TcpConnections = [];
        TimeFormatter = new PreciseTimeFormatter();

        State = state;
        Form = state.Form;

        Model.CurrentState = State;
        State.OnStart += State_OnStart;

        commandHandler = new CommandHandler(this,
            new ControlCommands(),
            new SplitCommands(),
            new TimerCommands(),
            new GetTimeCommands()
        );
    }

    public void StartTcp()
    {
        StopTcp();
        Server = new TcpListener(IPAddress.Any, State.Settings.ServerPort);
        Server.Start();
        Server.BeginAcceptTcpClient(AcceptTcpClient, null);
        ServerState = ServerStateType.TCP;
    }

    public void StartWs()
    {
        StopWs();
        WsServer = new WebSocketServer(State.Settings.ServerPort);
        WsServer.AddWebSocketService("/livesplit", () => new WsConnection(connection_MessageReceived));
        WsServer.Start();
        ServerState = ServerStateType.Websocket;
    }

    public void StartNamedPipe()
    {
        StopPipe();
        WaitingServerPipe = CreateServerPipe();
        WaitingServerPipe.BeginWaitForConnection(AcceptPipeClient, null);
    }

    public void StopAll()
    {
        StopTcp();
        StopPipe();
        StopWs();
        ServerState = ServerStateType.Off;
    }

    public void StopTcp()
    {
        foreach (TcpConnection connection in TcpConnections)
        {
            connection.Dispose();
        }

        TcpConnections.Clear();
        Server?.Stop();
        if (ServerState == ServerStateType.TCP)
        {
            ServerState = ServerStateType.Off;
        }
    }

    public void StopWs()
    {
        WsServer?.Stop();
        if (ServerState == ServerStateType.Websocket)
        {
            ServerState = ServerStateType.Off;
        }
    }

    public void StopPipe()
    {
        WaitingServerPipe?.Dispose();

        foreach (Connection connection in PipeConnections)
        {
            connection.Dispose();
        }

        PipeConnections.Clear();
    }

    public void AcceptTcpClient(IAsyncResult result)
    {
        try
        {
            TcpClient client = Server.EndAcceptTcpClient(result);
            var connection = new TcpConnection(client);
            connection.MessageReceived += connection_MessageReceived;
            connection.Disconnected += tcpConnection_Disconnected;
            TcpConnections.Add(connection);

            Server.BeginAcceptTcpClient(AcceptTcpClient, null);
        }
        catch { }
    }

    public void AcceptPipeClient(IAsyncResult result)
    {
        try
        {
            WaitingServerPipe.EndWaitForConnection(result);
            var connection = new Connection(WaitingServerPipe);
            connection.MessageReceived += connection_MessageReceived;
            connection.Disconnected += pipeConnection_Disconnected;
            PipeConnections.Add(connection);

            WaitingServerPipe = CreateServerPipe();
            WaitingServerPipe.BeginWaitForConnection(AcceptPipeClient, null);
        }
        catch { }
    }

    private void pipeConnection_Disconnected(object sender, EventArgs e)
    {
        var connection = (Connection)sender;
        connection.MessageReceived -= connection_MessageReceived;
        connection.Disconnected -= pipeConnection_Disconnected;
        PipeConnections.Remove(connection);
        connection.Dispose();
    }

    private NamedPipeServerStream CreateServerPipe()
    {
        var pipe = new NamedPipeServerStream("LiveSplit", PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        return pipe;
    }

    internal TimeSpan? ParseTime(string timeString)
    {
        if (timeString == "-")
        {
            return null;
        }

        return TimeSpanParser.Parse(timeString);
    }

    private void connection_MessageReceived(object sender, MessageEventArgs e)
    {
        ProcessMessage(e.Message, e.Connection);
    }

    private void ProcessMessage(string message, IConnection clientConnection)
    {
        string[] args = message.Split([' '], 2);
        string command = args[0];

        if (!commandHandler.TryHandleCommand(command, args, out string response))
        {
            Log.Error($"[Server] Invalid command: {message}");
        }
        else if (!string.IsNullOrEmpty(response))
        {
            clientConnection.SendMessage(response);
        }
    }

    private void tcpConnection_Disconnected(object sender, EventArgs e)
    {
        var connection = (TcpConnection)sender;
        connection.MessageReceived -= connection_MessageReceived;
        connection.Disconnected -= tcpConnection_Disconnected;
        TcpConnections.Remove(connection);
        connection.Dispose();
    }

    private void State_OnStart(object sender, EventArgs e)
    {
        if (AlwaysPauseGameTime)
        {
            State.IsGameTimePaused = true;
        }
    }

    internal TimeSpan? PredictTime(LiveSplitState state, string comparison)
    {
        if (state.CurrentPhase is TimerPhase.Running or TimerPhase.Paused)
        {
            TimeSpan? delta = LiveSplitStateHelper.GetLastDelta(state, state.CurrentSplitIndex, comparison, State.CurrentTimingMethod) ?? TimeSpan.Zero;
            TimeSpan? liveDelta = state.CurrentTime[State.CurrentTimingMethod] - state.CurrentSplit.Comparisons[comparison][State.CurrentTimingMethod];
            if (liveDelta > delta)
            {
                delta = liveDelta;
            }

            return delta + state.Run.Last().Comparisons[comparison][State.CurrentTimingMethod];
        }
        else if (state.CurrentPhase == TimerPhase.Ended)
        {
            return state.Run.Last().SplitTime[State.CurrentTimingMethod];
        }
        else
        {
            return state.Run.Last().Comparisons[comparison][State.CurrentTimingMethod];
        }
    }

    internal TimeSpan? GetCurrentTime(LiveSplitState state, TimingMethod timingMethod)
    {
        if (state.CurrentPhase == TimerPhase.NotRunning)
        {
            return state.Run.Offset;
        }
        else
        {
            return state.CurrentTime[timingMethod];
        }
    }

    public void Dispose()
    {
        State.OnStart -= State_OnStart;
        StopAll();
        WaitingServerPipe.Dispose();
    }
}
