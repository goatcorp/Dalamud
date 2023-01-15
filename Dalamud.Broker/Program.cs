using System.Reflection;
using CommandLine;
using Dalamud.Broker.Commands;
using Serilog;
using Serilog.Core;

namespace Dalamud.Broker;

internal static class Program
{
    private static void Main(string[] arguments)
    {
        InitializeLogging();
        
        var commands = GetCommands();
        Parser.Default.ParseArguments(arguments, commands)
              .WithParsed(static options => InvokeCommand(options));
    }

    private static Type[] GetCommands() => Assembly
                                         .GetExecutingAssembly()
                                         .GetTypes()
                                         .Where(t => t.GetCustomAttribute<VerbAttribute>() is not null)
                                         .ToArray();

    private static void InvokeCommand(object options)
    {
        switch (options)
        {
            case LaunchCommandOptions opt:
                LaunchCommand.Run(opt);
                break;
            case SetupCommandOptions opt:
                SetupCommand.Run(opt);
                break;
            case DebugCommandOptions opt:
                DebugCommand.Run(opt);
                break;
        }
    }

    private static void InitializeLogging()
    {
        // TODO: file logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();
    }
}
