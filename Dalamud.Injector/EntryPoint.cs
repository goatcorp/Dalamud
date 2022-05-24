using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Game;
using Dalamud.Injector.Exceptions;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Abstractions;
using Newtonsoft.Json;
using Reloaded.Memory.Buffers;
using Serilog;
using Serilog.Core;
using Serilog.Events;

using static Dalamud.Injector.NativeFunctions;

namespace Dalamud.Injector;

/// <summary>
/// Entrypoint to the program.
/// </summary>
public sealed class EntryPoint
{
    private static LoggingLevelSwitch? levelSwitch = null;

    /// <summary>
    /// A delegate used during initialization of the CLR from Dalamud.Injector.Boot.
    /// </summary>
    /// <param name="argc">Count of arguments.</param>
    /// <param name="argvPtr">wchar** string arguments.</param>
    public delegate void MainDelegate(int argc, IntPtr argvPtr);

    /// <summary>
    /// Start the Dalamud injector.
    /// </summary>
    /// <param name="argc">Count of arguments.</param>
    /// <param name="argvPtr">wchar** string arguments.</param>
    public static void Main(int argc, IntPtr argvPtr)
    {
        var args = Environment.GetCommandLineArgs()[1..].ToList();

        InitUnhandledException(args);
        InitLogging();
        InitWorkspace();

        var startInfo = default(DalamudStartInfo);

        if (args.Count == 0)
        {
            // No command defaults to inject
            args.Add("inject");
            args.Add("--all");

#if !DEBUG
            args.Add("--warn");
#endif
        }
        else if (args.Count == 2 && int.TryParse(args[0], out var _) && args[1].StartsWith("{"))
        {
            // Legacy format: Dalamud.Injector.exe [pid] [b64]
            // Convert to:    Dalamud.Injector.exe inject [pid]
            args.Insert(0, "inject");

            var encoded = args[2]; // was at 1
            var b64 = Convert.FromBase64String(args[2]);
            var utf8 = Encoding.UTF8.GetString(b64);
            startInfo = JsonConvert.DeserializeObject<DalamudStartInfo>(utf8);
            args.RemoveAt(2);
        }

        var app = new CommandLineApplication();
        app.Name = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
        app.Description = "Dalamud injection/launch utility.";
        app.HelpOption("-h|--help");

        var optLogLevel = app.Option<LogEventLevel?>("-L|--log-level", "Set the logging level.", CommandOptionType.SingleValue, inherited: true);
        optLogLevel.Accepts().Enum<LogEventLevel>(ignoreCase: true);

        void SetLogLevel()
        {
            var level = optLogLevel.ParsedValue;
            if (level is not null)
            {
                levelSwitch.MinimumLevel = level.Value;
                Log.Debug($"Logging level set to {level.Value}");
            }
        }

        app.OnExecute(() =>
        {
            SetLogLevel();
        });

        app.Command("help", cmd =>
        {
            cmd.Description = "Show help about a specific command.";
            cmd.HelpOption("-h|--help");

            var optCommand = cmd.Argument<string?>("command", "The command to display help for.");
            optCommand.Accepts().Values("help", "inject", "launch");

            cmd.OnExecute(() =>
            {
                SetLogLevel();

                var targetName = optCommand.ParsedValue;
                if (targetName == default)
                    targetName = "help";

                var target = app.Commands.FirstOrDefault(c => c.Name == targetName);
                if (target == default)
                    throw new CommandLineException($"Unknown command with name '{targetName}'");

                target.ShowHelp();
            });
        });

        app.Command("inject", cmd =>
        {
            cmd.Description = "Inject into an existing process.";
            cmd.HelpOption("-h|--help");

            var optAll = cmd.Option<bool>("-a|--all", "Inject into all ffxiv_dx11.exe processes.", CommandOptionType.NoValue);
            var optWarn = cmd.Option<bool>("-w|--warn", "Display a warning about manual injection.", CommandOptionType.NoValue);
            var optPids = cmd.Argument<int>("pid", "Specific PIDs to inject into. Required if --all is not used.", multipleValues: true);

            var parseDalamudOptions = AddDalamudOptions(cmd, startInfo);

            cmd.OnExecute(() =>
            {
                SetLogLevel();
                startInfo = parseDalamudOptions();

                var all = optAll.ParsedValue;
                var warn = optWarn.ParsedValue;
                var pids = optPids.ParsedValues;

                ProcessInjectCommand(all, warn, pids, startInfo);
            });
        });

        app.Command("launch", cmd =>
        {
            cmd.Description = "Launch FFXIV and then optionally inject.";
            cmd.AllowArgumentSeparator = true;
            cmd.HelpOption("-h|--help");

            var optFake = cmd.Option<bool>("-f|--fake-arguments", "Use a specific set of game arguments for testing.", CommandOptionType.NoValue);
            var optGame = cmd.Option<string?>("-g|--game", "The path to a ffxiv_dx11 executable.", CommandOptionType.SingleValue);
            var optMode = cmd.Option<DalamudLoadMode>("-m|--mode", "Injection method. Value is case-insensitive.", CommandOptionType.SingleValue);
            var optHandle = cmd.Option<int>("--handle-owner", "An inherited handle value.", CommandOptionType.SingleValue);
            var optWithoutDalamud = cmd.Option<bool>("--without-dalamud", "Launch without Dalamud.", CommandOptionType.NoValue);

            optGame.Accepts().ExistingFile();
            optMode.Accepts().Enum<DalamudLoadMode>(ignoreCase: true);

            var parseDalamudOptions = AddDalamudOptions(cmd, startInfo);

            cmd.OnExecute(() =>
            {
                SetLogLevel();
                startInfo = parseDalamudOptions();

                var fake = optFake.ParsedValue;
                var game = optGame.ParsedValue;
                var mode = optMode.ParsedValue;
                var handle = new IntPtr(optHandle.ParsedValue);
                var withoutDalamud = optWithoutDalamud.ParsedValue;
                var passthru = cmd.RemainingArguments;

                ProcessLaunchCommand(fake, game, mode, handle, withoutDalamud, passthru, startInfo);
            });
        });

        app.Command("launch-test", cmd =>
        {
            cmd.Description = "Test the launch command";
            cmd.ShowInHelpText = false;
            cmd.UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.CollectAndContinue;

            cmd.OnExecute(() =>
            {
                SetLogLevel();

                ProcessLaunchTestCommand(args);
            });
        });

        var result = app.Execute(args.ToArray());

        Environment.Exit(result);
    }

    /// <summary>
    /// Initialize the AppDomain UnhandledException handler.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    private static void InitUnhandledException(List<string> args)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
        {
            var exObj = eventArgs.ExceptionObject;

            if (Log.Logger == null)
            {
                Console.Error.WriteLine($"[!] A fatal error has occurred: {exObj}");
            }
            else if (exObj is CommandParsingException cpEx)
            {
                Log.Error($"Command line error: {cpEx.Message}");
                Environment.Exit(-1);
            }
            else if (exObj is CommandLineException clex)
            {
                Log.Error($"Command line error: {clex.Message}");
                Environment.Exit(-1);
            }
            else if (exObj is Exception ex)
            {
                Log.Error(ex, "A fatal error has occurred.");
            }
            else
            {
                Log.Error($"A fatal error has occurred: {exObj}");
            }

#if DEBUG
            var caption = "Debug Error";
            var message =
                "Couldn't inject.\n" +
                "Make sure that Dalamud was not injected into your target process as a release build " +
                "before and that the target process can be accessed with VM_WRITE permissions.\n\n" +
                $"{eventArgs.ExceptionObject}";
#else
            var caption = "XIVLauncher Error";
            var message =
                "Failed to inject the XIVLauncher in-game addon.\n" +
                "Please try restarting your game and your PC.\n" +
                "If this keeps happening, please report this error.";
#endif

            _ = MessageBoxW(IntPtr.Zero, message, caption, MB_ICONERROR | MB_OK);

            Environment.Exit(-1);
        };
    }

    /// <summary>
    /// Initialize the logging systems.
    /// </summary>
    private static void InitLogging()
    {
        var baseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

#if DEBUG
        var logPath = Path.Combine(baseDirectory, "dalamud.injector.log");
#else
        var logPath = Path.Combine(baseDirectory, "..", "..", "..", "dalamud.injector.log");
#endif

        levelSwitch = new LoggingLevelSwitch();

#if DEBUG
        levelSwitch.MinimumLevel = LogEventLevel.Verbose;
#else
        levelSwitch.MinimumLevel = LogEventLevel.Information;
#endif

        try
        {
            CullLogFile(logPath, 1 * 1024 * 1024);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[!] Could not cull log file: {ex.Message}");
            // var caption = "XIVLauncher Error";
            // var message = $"Log cull threw an exception: {ex.Message}\n{ex.StackTrace ?? string.Empty}";
            // _ = MessageBoxW(IntPtr.Zero, message, caption, MessageBoxType.IconError | MessageBoxType.Ok);
        }

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(standardErrorFromLevel: LogEventLevel.Verbose)
            .WriteTo.Async(a => a.File(logPath))
            .MinimumLevel.ControlledBy(levelSwitch)
            .CreateLogger();
    }

    /// <summary>
    /// Initialize any workspace/directory settings necessary.
    /// </summary>
    private static void InitWorkspace()
    {
        var cwd = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
        if (cwd.FullName != Directory.GetCurrentDirectory())
        {
            Log.Debug($"Changing cwd to {cwd}");
            Directory.SetCurrentDirectory(cwd.FullName);
        }
    }

    /// <summary>
    /// Shrink a log file by reading from the (FILELEN - MAXLEN) position and writing to the beginning.
    /// </summary>
    /// <param name="logPath">Path to the log file.</param>
    /// <param name="maxFileSize">Maximum file size to keep.</param>
    private static void CullLogFile(string logPath, int maxFileSize)
    {
        const int bufferSize = 4096;

        var logFile = new FileInfo(logPath);

        if (!logFile.Exists)
            logFile.Create();

        if (logFile.Length <= maxFileSize)
            return;

        var amountToCull = logFile.Length - maxFileSize;

        if (amountToCull < bufferSize)
            return;

        using var reader = new BinaryReader(logFile.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        using var writer = new BinaryWriter(logFile.Open(FileMode.Open, FileAccess.Write, FileShare.ReadWrite));

        reader.BaseStream.Seek(amountToCull, SeekOrigin.Begin);

        var read = -1;
        var total = 0;
        var buffer = new byte[bufferSize];
        while (read != 0)
        {
            read = reader.Read(buffer, 0, buffer.Length);
            writer.Write(buffer, 0, read);
            total += read;
        }

        writer.BaseStream.SetLength(total);
    }

    /// <summary>
    /// Add the DalamudStartInfo options to the given command and return a function to parse them.
    /// </summary>
    /// <param name="cmd">Application or command to add the options to.</param>
    /// <param name="startInfo">Existing startInfo for fallback values.</param>
    /// <returns>A function to parse the options and existing startInfo.</returns>
    private static Func<DalamudStartInfo> AddDalamudOptions(CommandLineApplication cmd, DalamudStartInfo? startInfo)
    {
        var optDalamudWorkingDirectory = cmd.Option<string?>("--dalamud-working-directory", "Dalamud working directory.", CommandOptionType.SingleValue);
        var optDalamudConfigurationPath = cmd.Option<string?>("--dalamud-configuration-path", "Dalamud configuration path.", CommandOptionType.SingleValue);
        var optDalamudPluginDirectory = cmd.Option<string?>("--dalamud-plugin-directory", "Dalamud plugin directory.", CommandOptionType.SingleValue);
        var optDalamudDevPluginDirectory = cmd.Option<string?>("--dalamud-dev-plugin-directory", "Dalamud DEV plugin directory.", CommandOptionType.SingleValue);
        var optDalamudAssetDirectory = cmd.Option<string?>("--dalamud-asset-directory", "Dalamud asset directory.", CommandOptionType.SingleValue);
        var optDalamudDelayInitialize = cmd.Option<int?>("--dalamud-delay-initialize", "Dalamud initialization delay in milliseconds.", CommandOptionType.SingleValue);
        var optDalamudClientLanguage = new CommandOption<ClientLanguage?>(ClientLanguageParser.Instance, "--dalamud-client-language", CommandOptionType.SingleValue)
        {
            Description = "Dalamud client language. Value may be a submatch, case-insensitive, localized, or a number from 0 to 3.",
        };
        cmd.AddOption(optDalamudClientLanguage);

        return () =>
        {
            var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var xivlauncherDir = Path.Combine(appDataDir, "XIVLauncher");

            var workingDirectory = optDalamudWorkingDirectory.ParsedValue ?? startInfo?.WorkingDirectory ?? Directory.GetCurrentDirectory();
            var configurationPath = optDalamudConfigurationPath.ParsedValue ?? startInfo?.ConfigurationPath ?? Path.Combine(xivlauncherDir, "dalamudConfig.json");
            var pluginDirectory = optDalamudPluginDirectory.ParsedValue ?? startInfo?.PluginDirectory ?? Path.Combine(xivlauncherDir, "installedPlugins");
            var defaultPluginDirectory = optDalamudDevPluginDirectory.ParsedValue ?? startInfo?.DefaultPluginDirectory ?? Path.Combine(xivlauncherDir, "devPlugins");
            var assetDirectory = optDalamudAssetDirectory.ParsedValue ?? startInfo?.AssetDirectory ?? Path.Combine(xivlauncherDir, "dalamudAssets", "dev");
            var delayInitializeMs = optDalamudDelayInitialize.ParsedValue ?? startInfo?.DelayInitializeMs ?? 0;
            var clientLanguage = optDalamudClientLanguage.ParsedValue ?? startInfo?.Language ?? ClientLanguage.English;

            return new()
            {
                WorkingDirectory = workingDirectory,
                ConfigurationPath = configurationPath,
                PluginDirectory = pluginDirectory,
                DefaultPluginDirectory = defaultPluginDirectory,
                AssetDirectory = assetDirectory,
                Language = clientLanguage,
                GameVersion = null,
                DelayInitializeMs = delayInitializeMs,
            };
        };
    }

    /// <summary>
    /// Process the "inject" command.
    /// </summary>
    /// <param name="all">Target all FFXIV processes.</param>
    /// <param name="warn">Warn about manual injection.</param>
    /// <param name="pids">Specific process PIDs.</param>
    /// <param name="startInfo">Dalamud start info.</param>
    private static void ProcessInjectCommand(bool all, bool warn, IReadOnlyList<int> pids, DalamudStartInfo startInfo)
    {
        if (!all && pids.Count == 0)
            throw new CommandLineException("No target process has been specified. Use the -a(--all) option to inject to all ffxiv_dx11 processes.");

        var processes = new List<Process>();

        if (all)
        {
            processes.AddRange(Process.GetProcessesByName("ffxiv_dx11"));
        }

        foreach (var pid in pids)
        {
            try
            {
                processes.Add(Process.GetProcessById(pid));
            }
            catch (ArgumentException)
            {
                Log.Error($"Could not find process with PID {pid}");
            }
        }

        if (!processes.Any())
        {
            Log.Error("No suitable target process was found.");
            Environment.Exit(-1);
        }

        if (warn)
        {
            var pidStr = string.Join(", ", processes.Select(p => $"{p.Id}"));

            var caption = "Dalamud";
            var message =
                $"Take care: you are manually injecting Dalamud into FFXIV({pidStr}).\n\n" +
                "If you are doing this to use plugins before they are officially whitelisted on patch days, things may go wrong and you may get into trouble.\n" +
                "We discourage you from doing this and you won't be warned again in-game.";

            var result = MessageBoxW(IntPtr.Zero, message, caption, MB_ICONWARNING | MB_OKCANCEL);

            if (result == IDCANCEL)
            {
                Log.Information("User cancelled injection");
                Environment.Exit(-2);
            }
        }

        foreach (var process in processes)
        {
            Inject(process, AdjustStartInfo(startInfo, process.MainModule.FileName));
        }
    }

    /// <summary>
    /// Launch ffxiv_dx11.exe and (optionally) inject Dalamud.
    /// </summary>
    /// <param name="useFakeArguments">Use a series of fake game arguments.</param>
    /// <param name="gamePath">Path to the ffxiv_dx11 executable.</param>
    /// <param name="mode">Injection mode.</param>
    /// <param name="handleOwner">Process handle owner.</param>
    /// <param name="withoutDalamud">Launch without Dalamud.</param>
    /// <param name="passthru">Passthrough arguments for the game.</param>
    /// <param name="startInfo">Dalamud start info.</param>
    private static void ProcessLaunchCommand(bool useFakeArguments, string? gamePath, DalamudLoadMode mode, IntPtr handleOwner, bool withoutDalamud, IReadOnlyList<string> passthru, DalamudStartInfo startInfo)
    {
        if (gamePath == null)
        {
            try
            {
                var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var launcherConfigPath = Path.Combine(appDataDir, "XIVLauncher", "launcherConfigV3.json");
                var launcherConfigContent = File.ReadAllText(launcherConfigPath);
                var launcherConfig = JsonConvert.DeserializeObject<Dictionary<string, string>>(launcherConfigContent);
                gamePath = Path.Combine(launcherConfig["GamePath"], "game", "ffxiv_dx11.exe");
                Log.Information($"Using game installation path from XIVLauncher: {gamePath}");
            }
            catch (Exception)
            {
                gamePath = @"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\ffxiv_dx11.exe";
                Log.Warning($"Failed to read launcherConfigV3.json. Using default game installation path: {gamePath}");
            }

            if (!File.Exists(gamePath))
            {
                Log.Error($"File not found: {gamePath}");
                Environment.Exit(-1);
            }
        }

        var gameDir = Path.GetDirectoryName(gamePath);
        var gameArguments = new List<string>(passthru);

        if (useFakeArguments)
        {
            var gameVerPath = Path.Combine(gameDir, "ffxivgame.ver");
            var gameVersion = File.ReadAllText(gameVerPath);

            var maxEntitledExpansionId = 0;
            var sqpackDir = Path.Combine(gameDir, "sqpack");
            while (File.Exists(Path.Combine(sqpackDir, $"ex{maxEntitledExpansionId + 1}", $"ex{maxEntitledExpansionId + 1}.ver")))
                maxEntitledExpansionId++; // Increment for each <gameDir>/sqpack/ex#/ex#.ver

            gameArguments.InsertRange(0, new string[]
            {
                "DEV.TestSID=0",
                "DEV.UseSqPack=1",
                "DEV.DataPathType=1",
                "DEV.LobbyHost01=127.0.0.1",
                "DEV.LobbyPort01=54994",
                "DEV.LobbyHost02=127.0.0.2",
                "DEV.LobbyPort02=54994",
                "DEV.LobbyHost03=127.0.0.3",
                "DEV.LobbyPort03=54994",
                "DEV.LobbyHost04=127.0.0.4",
                "DEV.LobbyPort04=54994",
                "DEV.LobbyHost05=127.0.0.5",
                "DEV.LobbyPort05=54994",
                "DEV.LobbyHost06=127.0.0.6",
                "DEV.LobbyPort06=54994",
                "DEV.LobbyHost07=127.0.0.7",
                "DEV.LobbyPort07=54994",
                "DEV.LobbyHost08=127.0.0.8",
                "DEV.LobbyPort08=54994",
                "DEV.LobbyHost09=127.0.0.9",
                "DEV.LobbyPort09=54994",
                "SYS.Region=0",
                $"language={(int)startInfo.Language}",
                $"ver={gameVersion}",
                $"DEV.MaxEntitledExpansionID={maxEntitledExpansionId}",
                "DEV.GMServerHost=127.0.0.100",
                "DEV.GameQuitMessageBox=0",
            });
        }

        startInfo = AdjustStartInfo(startInfo, gamePath);
        var startInfoJson = JsonConvert.SerializeObject(startInfo);
        Log.Information($"Using start info: {startInfoJson}");

        var gameArgumentString = ArgumentEscaper.EscapeAndConcatenate(gameArguments);
        var process = NativeAclFix.LaunchGame(gameDir, gamePath, gameArgumentString, (Process p) =>
        {
            if (!withoutDalamud && mode == DalamudLoadMode.Entrypoint)
            {
                try
                {
                    using var rewrite = new RewriteOriginalEntrypoint(p, false);
                    rewrite.Rewrite(gamePath, startInfoJson);
                }
                catch (Exception ex)
                {
                    Log.Error("[HOOKS] RewriteRemoteEntryPoint failed");
                    throw new Exception("RewriteRemoteEntryPoint failed", ex);
                }
            }
        });

        if (!withoutDalamud && mode == DalamudLoadMode.Inject)
        {
            Inject(process, startInfo);
        }

        var processHandleForOwner = IntPtr.Zero;
        if (handleOwner != IntPtr.Zero)
        {
            var currentProcess = Process.GetCurrentProcess();
            if (!DuplicateHandle(currentProcess.Handle, process.Handle, handleOwner, out processHandleForOwner, 0, false, DUPLICATE_SAME_ACCESS))
            {
                var ex = new Win32Exception(Marshal.GetLastWin32Error());
                Log.Warning($"DuplicateHandle failed: 0x{ex.NativeErrorCode:X} {ex.Message}");
            }
        }

        var output = JsonConvert.SerializeObject(new LaunchOutput { Pid = process.Id, Handle = processHandleForOwner });
        Console.WriteLine(output);
    }

    /// <summary>
    /// Test the launch command.
    /// </summary>
    private static void ProcessLaunchTestCommand(List<string> args)
    {
        Log.Information("Testing launch command");

        var helperProcess = new Process();
        helperProcess.StartInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;

        var launchIndex = args.IndexOf("launch-test");
        args.RemoveAt(launchIndex);
        args.InsertRange(launchIndex, new[]
        {
            "launch",
            $"--handle-owner={GetInheritableCurrentProcessHandle().Handle}", // so that it closes the handle when it's done
        });

        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            Log.Debug($"Argument {i}: {arg}");
            helperProcess.StartInfo.ArgumentList.Add(arg);
        }

        helperProcess.StartInfo.RedirectStandardOutput = true;
        helperProcess.StartInfo.RedirectStandardError = true;
        helperProcess.StartInfo.UseShellExecute = false;
        helperProcess.ErrorDataReceived += (sendingProcess, errLine) => Log.Information($"stderr: \"{errLine.Data}\"");
        helperProcess.Start();
        helperProcess.BeginErrorReadLine();
        helperProcess.WaitForExit();

        if (helperProcess.ExitCode != 0)
            Environment.Exit(-1);

        var serializer = JsonSerializer.CreateDefault();
        using var jtr = new JsonTextReader(helperProcess.StandardOutput);
        var result = serializer.Deserialize<LaunchOutput>(jtr);

        var resultProcess = new ExistingProcess(result.Handle);

        Log.Information($"PID: {result.Pid}, Handle: {result.Handle}");
        Log.Information("Press Enter to force quit");
        Console.ReadLine();

        resultProcess.Kill();
    }

    /// <summary>
    /// Get a process handle that can be inherited by duplicating it.
    /// </summary>
    /// <returns>An inheritable process handle.</returns>
    private static Process? GetInheritableCurrentProcessHandle()
    {
        var currentProcess = Process.GetCurrentProcess();
        if (!DuplicateHandle(currentProcess.Handle, currentProcess.Handle, currentProcess.Handle, out var inheritableCurrentProcessHandle, 0, true, DUPLICATE_SAME_ACCESS))
        {
            var ex = new Win32Exception(Marshal.GetLastWin32Error());
            Log.Error($"DuplicateHandle failed: 0x{ex.NativeErrorCode:X} {ex.Message}");
            return null;
        }

        return new ExistingProcess(inheritableCurrentProcessHandle);
    }

    /// <summary>
    /// Update the startInfo with the game version of the target process.
    /// </summary>
    /// <param name="startInfo">Dalamud start info.</param>
    /// <param name="gamePath">Path to the ffxiv_dx11 executable.</param>
    /// <returns>Updated startInfo.</returns>
    private static DalamudStartInfo AdjustStartInfo(DalamudStartInfo startInfo, string gamePath)
    {
        var ffxivDir = Path.GetDirectoryName(gamePath);
        var gameVerStr = File.ReadAllText(Path.Combine(ffxivDir, "ffxivgame.ver"));
        var gameVer = GameVersion.Parse(gameVerStr);

        return new()
        {
            WorkingDirectory = startInfo.WorkingDirectory,
            ConfigurationPath = startInfo.ConfigurationPath,
            PluginDirectory = startInfo.PluginDirectory,
            DefaultPluginDirectory = startInfo.DefaultPluginDirectory,
            AssetDirectory = startInfo.AssetDirectory,
            Language = startInfo.Language,
            GameVersion = gameVer,
            DelayInitializeMs = startInfo.DelayInitializeMs,
        };
    }

    /// <summary>
    /// Inject into the given process.
    /// </summary>
    /// <param name="process">Target process.</param>
    /// <param name="startInfo">Dalamud start info.</param>
    private static uint Inject(Process process, DalamudStartInfo startInfo)
    {
        var bootName = "Dalamud.Boot.dll";
        var bootPath = Path.GetFullPath(bootName);

        // ======================================================

        using var injector = new Injector(process, false);

        injector.LoadLibrary(bootPath, out var bootModule);

        // ======================================================

        var startInfoJson = JsonConvert.SerializeObject(startInfo);
        var startInfoBytes = Encoding.UTF8.GetBytes(startInfoJson);

        using var startInfoBuffer = new MemoryBufferHelper(process).CreatePrivateMemoryBuffer(startInfoBytes.Length + 0x8);
        var startInfoAddress = startInfoBuffer.Add(startInfoBytes);

        if (startInfoAddress == IntPtr.Zero)
            throw new Exception("Unable to allocate start info JSON");

        injector.GetFunctionAddress(bootModule, "Initialize", out var initAddress);
        injector.CallRemoteFunction(initAddress, startInfoAddress, out var exitCode);

        // ======================================================

        if (exitCode == 0)
        {
            Log.Debug($"Dalamud.Boot::Initialize successful (pid:{process.Id})");
        }
        else
        {
            Log.Error($"Dalamud.Boot::Initialize returned {exitCode} (pid:{process.Id})");
        }

        Log.Information("Done");
        return exitCode;
    }

    /// <summary>
    /// Stdout from the "launch" command.
    /// </summary>
    private struct LaunchOutput
    {
        public int Pid;
        public IntPtr Handle;
    }

    /// <summary>
    /// A parser for the ClientLanguage enum.
    /// </summary>
    private class ClientLanguageParser : IValueParser<ClientLanguage?>
    {
        private static ClientLanguageParser? instance;

        public static ClientLanguageParser Instance => instance ??= new();

        public Type TargetType => typeof(ClientLanguage);

        object? IValueParser.Parse(string? name, string? value, CultureInfo culture)
            => this.Parse(name, value, culture);

        public ClientLanguage? Parse(string? name, string? value, CultureInfo culture)
        {
            static bool CompareLanguage(string input, params string[] values)
            {
                // Compare input to a substring of value, the same length as input
                // i.e. "eng" == "english"[..3]
                return values.Any(value => input == value[..Math.Min(value.Length, input.Length)]);
            }

            if (int.TryParse(value, out var i))
            {
                // Parse enum
                var lang = (ClientLanguage)i;
                if (!Enum.IsDefined(lang))
                {
                    var max = Enum.GetValues<ClientLanguage>().Length - 1;
                    throw new CommandLineException($"Invalid value client language. '{value}' is not between [0..{max}].");
                }

                return lang;
            }
            else
            {
                // Parse language
                var lang = value.ToLowerInvariant() switch
                {
                    _ when CompareLanguage(value, "english") => ClientLanguage.English,
                    _ when CompareLanguage(value, "japanese", "日本語") => ClientLanguage.Japanese,
                    _ when CompareLanguage(value, "german", "deutsche") => ClientLanguage.German,
                    _ when CompareLanguage(value, "french", "français") => ClientLanguage.French,
                    _ => throw new CommandLineException($"Invalid client language. '{value}' is not a valid supported langauge."),
                };

                return lang;
            }
        }
    }
}
