using System;
using System.Collections.Generic;

namespace LiveSplit.Server.Commands;
internal interface ICommands
{
    IEnumerable<KeyValuePair<string, Func<string[], string>>> GetCommands(CommandServer server);
}

internal class CommandHandler
{
    private readonly Dictionary<string, Func<string[], string>> _commands;

    public CommandHandler(CommandServer server, params ICommands[] commandProviders)
    {
        _commands = new Dictionary<string, Func<string[], string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in commandProviders)
        {
            foreach (var command in provider.GetCommands(server))
            {
                _commands[command.Key] = command.Value;
            }
        }
    }

    public bool TryHandleCommand(string commandName, string[] args, out string response)
    {
        response = null;
        if (_commands.TryGetValue(commandName, out Func<string[], string> action))
        {
            try
            {
                response = action(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing command '{commandName}': {ex.Message}");
            }
            return true;
        }
        return false;
    }
}
