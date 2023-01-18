using System.Reflection;
using CommandLine;
using Dalamud.Broker.Commands;
using Serilog;
using Serilog.Core;

namespace Dalamud.Broker;

internal static class Program
{
    private static async Task Main(string[] arguments)
    {
        InitializeLogging();

        try
        {
            var commands = GetCommands();
            await Parser.Default.ParseArguments(arguments, commands)
                        .WithParsedAsync(static async options => await InvokeCommand(options));
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Something went wrong");
#if DEBUG
            throw;
#endif
        }
    }

    private static Type[] GetCommands() => Assembly
                                           .GetExecutingAssembly()
                                           .GetTypes()
                                           .Where(t => t.GetCustomAttribute<VerbAttribute>() is not null)
                                           .ToArray();

    private static async Task InvokeCommand(object options)
    {
        switch (options)
        {
            case LaunchCommandOptions opt:
                await LaunchCommand.Run(opt);
                break;
            case SetupCommandOptions opt:
                SetupCommand.Run(opt);
                break;
            case DebugCommandOptions opt:
                await DebugCommand.Run(opt);
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
