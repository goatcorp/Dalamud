using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using Dalamud.Game;
using Newtonsoft.Json;
using Reloaded.Memory.Buffers;
using Serilog;
using Serilog.Core;
using Serilog.Events;

using static Dalamud.Injector.NativeFunctions;

namespace Dalamud.Injector
{
    /// <summary>
    /// Entrypoint to the program.
    /// </summary>
    public sealed class EntryPoint
    {
        /// <summary>
        /// A delegate used during initialization of the CLR from Dalamud.Injector.Boot.
        /// </summary>
        /// <param name="argc">Count of arguments.</param>
        /// <param name="argvPtr">char** string arguments.</param>
        public delegate void MainDelegate(int argc, IntPtr argvPtr);

        /// <summary>
        /// Start the Dalamud injector.
        /// </summary>
        /// <param name="argc">Count of arguments.</param>
        /// <param name="argvPtr">byte** string arguments.</param>
        public static void Main(int argc, IntPtr argvPtr)
        {
            Init();

            List<string> args = new(argc);
            unsafe
            {
                var argv = (IntPtr*)argvPtr;
                for (var i = 0; i < argc; i++)
                    args.Add(Marshal.PtrToStringUni(argv[i]));
            }

            if (args.Count >= 2 && args[1].ToLowerInvariant() == "launch-test")
            {
                Environment.Exit(ProcessLaunchTestCommand(args));
                return;
            }

            DalamudStartInfo startInfo = null;
            if (args.Count == 1)
            {
                // No command defaults to inject
                args.Add("inject");
                args.Add("--all");
                args.Add("--warn");
            }
            else if (int.TryParse(args[1], out var _))
            {
                // Assume that PID has been passed.
                args.Insert(1, "inject");

                // If originally second parameter exists, then assume that it's a base64 encoded start info.
                // Dalamud.Injector.exe inject [pid] [base64]
                if (args.Count == 4)
                {
                    startInfo = JsonConvert.DeserializeObject<DalamudStartInfo>(Encoding.UTF8.GetString(Convert.FromBase64String(args[3])));
                    args.RemoveAt(3);
                }
            }

            startInfo = ExtractAndInitializeStartInfoFromArguments(startInfo, args);

            var mainCommand = args[1].ToLowerInvariant();
            if (mainCommand.Length > 0 && mainCommand.Length <= 6 && "inject"[..mainCommand.Length] == mainCommand)
            {
                Environment.Exit(ProcessInjectCommand(args, startInfo));
            }
            else if (mainCommand.Length > 0 && mainCommand.Length <= 6 && "launch"[..mainCommand.Length] == mainCommand)
            {
                Environment.Exit(ProcessLaunchCommand(args, startInfo));
            }
            else if (mainCommand.Length > 0 && mainCommand.Length <= 4 && "help"[..mainCommand.Length] == mainCommand)
            {
                Environment.Exit(ProcessHelpCommand(args, args.Count >= 3 ? args[2] : null));
            }
            else
            {
                Console.WriteLine("Invalid command: {0}", mainCommand);
                ProcessHelpCommand(args);
                Environment.Exit(-1);
            }
        }

        private static void Init()
        {
            InitUnhandledException();
            InitLogging();

            var cwd = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
            if (cwd.FullName != Directory.GetCurrentDirectory())
            {
                Log.Debug($"Changing cwd to {cwd}");
                Directory.SetCurrentDirectory(cwd.FullName);
            }
        }

        private static void InitUnhandledException()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                if (Log.Logger == null)
                {
                    Console.WriteLine($"A fatal error has occurred: {eventArgs.ExceptionObject}");
                }
                else
                {
                    var exObj = eventArgs.ExceptionObject;
                    if (exObj is Exception ex)
                    {
                        Log.Error(ex, "A fatal error has occurred.");
                    }
                    else
                    {
                        Log.Error($"A fatal error has occurred: {eventArgs.ExceptionObject}");
                    }
                }

#if DEBUG
                var caption = "Debug Error";
                var message =
                    $"Couldn't inject.\nMake sure that Dalamud was not injected into your target process " +
                    $"as a release build before and that the target process can be accessed with VM_WRITE permissions.\n\n" +
                    $"{eventArgs.ExceptionObject}";
#else
                var caption = "XIVLauncher Error";
                var message =
                    "Failed to inject the XIVLauncher in-game addon.\nPlease try restarting your game and your PC.\n" +
                    "If this keeps happening, please report this error.";
#endif
                _ = MessageBoxW(IntPtr.Zero, message, caption, MessageBoxType.IconError | MessageBoxType.Ok);

                Environment.Exit(0);
            };
        }

        private static void InitLogging()
        {
            var baseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

#if DEBUG
            var logPath = Path.Combine(baseDirectory, "dalamud.injector.log");
#else
            var logPath = Path.Combine(baseDirectory, "..", "..", "..", "dalamud.injector.log");
#endif

            var levelSwitch = new LoggingLevelSwitch();

#if DEBUG
            levelSwitch.MinimumLevel = LogEventLevel.Verbose;
#else
            levelSwitch.MinimumLevel = LogEventLevel.Information;
#endif

            CullLogFile(logPath, 1 * 1024 * 1024);

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(standardErrorFromLevel: LogEventLevel.Verbose)
                .WriteTo.Async(a => a.File(logPath))
                .MinimumLevel.ControlledBy(levelSwitch)
                .CreateLogger();
        }

        private static void CullLogFile(string logPath, int cullingFileSize)
        {
            try
            {
                var bufferSize = 4096;

                var logFile = new FileInfo(logPath);

                if (!logFile.Exists)
                    logFile.Create();

                if (logFile.Length <= cullingFileSize)
                    return;

                var amountToCull = logFile.Length - cullingFileSize;

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
            catch (Exception)
            {
                /*
                var caption = "XIVLauncher Error";
                var message = $"Log cull threw an exception: {ex.Message}\n{ex.StackTrace ?? string.Empty}";
                _ = MessageBoxW(IntPtr.Zero, message, caption, MessageBoxType.IconError | MessageBoxType.Ok);
                */
            }
        }

        private static DalamudStartInfo ExtractAndInitializeStartInfoFromArguments(DalamudStartInfo? startInfo, List<string> args)
        {
            if (startInfo == null)
                startInfo = new();

            var workingDirectory = startInfo.WorkingDirectory;
            var configurationPath = startInfo.ConfigurationPath;
            var pluginDirectory = startInfo.PluginDirectory;
            var defaultPluginDirectory = startInfo.DefaultPluginDirectory;
            var assetDirectory = startInfo.AssetDirectory;
            var delayInitializeMs = startInfo.DelayInitializeMs;

            for (var i = 2; i < args.Count; i++)
            {
                string key;
                if (args[i].StartsWith(key = "--dalamud-working-directory="))
                    workingDirectory = args[i][key.Length..];
                else if (args[i].StartsWith(key = "--dalamud-configuration-path="))
                    configurationPath = args[i][key.Length..];
                else if (args[i].StartsWith(key = "--dalamud-plugin-directory="))
                    pluginDirectory = args[i][key.Length..];
                else if (args[i].StartsWith(key = "--dalamud-dev-plugin-directory="))
                    defaultPluginDirectory = args[i][key.Length..];
                else if (args[i].StartsWith(key = "--dalamud-asset-directory="))
                    assetDirectory = args[i][key.Length..];
                else if (args[i].StartsWith(key = "--dalamud-delay-initialize="))
                    delayInitializeMs = int.Parse(args[i][key.Length..]);
                else
                    continue;

                args.RemoveAt(i);
                i--;
            }

            var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var xivlauncherDir = Path.Combine(appDataDir, "XIVLauncher");

            workingDirectory ??= Directory.GetCurrentDirectory();
            configurationPath ??= Path.Combine(xivlauncherDir, "dalamudConfig.json");
            pluginDirectory ??= Path.Combine(xivlauncherDir, "installedPlugins");
            defaultPluginDirectory ??= Path.Combine(xivlauncherDir, "devPlugins");
            assetDirectory ??= Path.Combine(xivlauncherDir, "dalamudAssets", "dev");

            return new()
            {
                WorkingDirectory = workingDirectory,
                ConfigurationPath = configurationPath,
                PluginDirectory = pluginDirectory,
                DefaultPluginDirectory = defaultPluginDirectory,
                AssetDirectory = assetDirectory,
                Language = ClientLanguage.English,
                GameVersion = null,
                DelayInitializeMs = delayInitializeMs,
            };
        }

        private static int ProcessHelpCommand(List<string> args, string? particularCommand = default)
        {
            var exeName = Path.GetFileName(args[0]);

            var exeSpaces = string.Empty;
            for (var i = exeName.Length; i > 0; i--)
                exeSpaces += " ";

            if (particularCommand is null or "help")
                Console.WriteLine("{0} help [command]", exeName);

            if (particularCommand is null or "inject")
                Console.WriteLine("{0} inject [-h/--help] [-a/--all] [--warn] [pid1] [pid2] [pid3] ...", exeName);

            if (particularCommand is null or "launch")
            {
                Console.WriteLine("{0} launch [-h/--help] [-f/--fake-arguments]", exeName);
                Console.WriteLine("{0}        [-g path/to/ffxiv_dx11.exe] [--game=path/to/ffxiv_dx11.exe]", exeSpaces);
                Console.WriteLine("{0}        [-m entrypoint|inject] [--mode=entrypoint|inject]", exeSpaces);
                Console.WriteLine("{0}        [--handle-owner=inherited-handle-value]", exeSpaces);
                Console.WriteLine("{0}        [-- game_arg1=value1 game_arg2=value2 ...]", exeSpaces);
            }

            Console.WriteLine("Specifying dalamud start info: [--dalamud-working-directory path] [--dalamud-configuration-path path]");
            Console.WriteLine("                               [--dalamud-plugin-directory path] [--dalamud-dev-plugin-directory path]");
            Console.WriteLine("                               [--dalamud-asset-directory path] [--dalamud-delay-initialize 0(ms)]");

            return 0;
        }

        private static int ProcessInjectCommand(List<string> args, DalamudStartInfo dalamudStartInfo)
        {
            List<Process> processes = new();

            var targetProcessSpecified = false;
            var warnManualInjection = false;
            var showHelp = args.Count <= 2;

            for (var i = 2; i < args.Count; i++)
            {
                if (int.TryParse(args[i], out int pid))
                {
                    targetProcessSpecified = true;
                    try
                    {
                        processes.Add(Process.GetProcessById(pid));
                    }
                    catch (ArgumentException)
                    {
                        Log.Error("Could not find process with PID: {Pid}", pid);
                    }

                    continue;
                }

                if (args[i] == "-h" || args[i] == "--help")
                {
                    showHelp = true;
                }
                else if (args[i] == "-a" || args[i] == "--all")
                {
                    targetProcessSpecified = true;
                    processes.AddRange(Process.GetProcessesByName("ffxiv_dx11"));
                }
                else if (args[i] == "--warn")
                {
                    warnManualInjection = true;
                }
                else
                {
                    Log.Error("\"{0}\" is not a valid argument.", args[i]);
                    return -1;
                }
            }

            if (showHelp)
            {
                ProcessHelpCommand(args, "inject");
                return args.Count <= 2 ? -1 : 0;
            }

            if (!targetProcessSpecified)
            {
                Log.Error("No target process has been specified.");
                return -1;
            }
            else if (!processes.Any())
            {
                Log.Error("No suitable target process has been found.");
                return -1;
            }

            if (warnManualInjection)
            {
                var result = MessageBoxW(IntPtr.Zero, $"Take care: you are manually injecting Dalamud into FFXIV({string.Join(", ", processes.Select(x => $"{x.Id}"))}).\n\nIf you are doing this to use plugins before they are officially whitelisted on patch days, things may go wrong and you may get into trouble.\nWe discourage you from doing this and you won't be warned again in-game.", "Dalamud", MessageBoxType.IconWarning | MessageBoxType.OkCancel);

                // IDCANCEL
                if (result == 2)
                {
                    Log.Information("User cancelled injection");
                    return -2;
                }
            }

            foreach (var process in processes)
                Inject(process, AdjustStartInfo(dalamudStartInfo, process.MainModule.FileName));

            return 0;
        }

        private static int ProcessLaunchCommand(List<string> args, DalamudStartInfo dalamudStartInfo)
        {
            string? gamePath = null;
            List<string> gameArguments = new();
            string? mode = null;
            var useFakeArguments = false;
            var showHelp = args.Count <= 2;
            var handleOwner = IntPtr.Zero;

            var parsingGameArgument = false;
            for (var i = 2; i < args.Count; i++)
            {
                if (parsingGameArgument)
                {
                    gameArguments.Add(args[i]);
                    continue;
                }

                if (args[i] == "-h" || args[i] == "--help")
                {
                    showHelp = true;
                }
                else if (args[i] == "-f" || args[i] == "--fake-arguments")
                {
                    useFakeArguments = true;
                }
                else if (args[i] == "-g")
                {
                    gamePath = args[++i];
                }
                else if (args[i].StartsWith("--game="))
                {
                    gamePath = args[i].Split('=', 2)[1];
                }
                else if (args[i] == "-m")
                {
                    mode = args[++i];
                }
                else if (args[i].StartsWith("--mode="))
                {
                    gamePath = args[i].Split('=', 2)[1];
                }
                else if (args[i].StartsWith("--handle-owner="))
                {
                    handleOwner = IntPtr.Parse(args[i].Split('=', 2)[1]);
                }
                else if (args[i] == "--")
                {
                    parsingGameArgument = true;
                }
                else
                {
                    Log.Error("No such command found: {0}", args[i]);
                    return -1;
                }
            }

            if (showHelp)
            {
                ProcessHelpCommand(args, "launch");
                return args.Count <= 2 ? -1 : 0;
            }

            mode = mode == null ? "entrypoint" : mode.ToLowerInvariant();
            if (mode.Length > 0 && mode.Length <= 10 && "entrypoint"[0..mode.Length] == mode)
            {
                mode = "entrypoint";
            }
            else if (mode.Length > 0 && mode.Length <= 6 && "inject"[0..mode.Length] == mode)
            {
                mode = "inject";
            }
            else
            {
                Log.Error("Invalid mode: {0}", mode);
                return -1;
            }

            if (gamePath == null)
            {
                try
                {
                    var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var xivlauncherDir = Path.Combine(appDataDir, "XIVLauncher");
                    var launcherConfigPath = Path.Combine(xivlauncherDir, "launcherConfigV3.json");
                    gamePath = Path.Combine(JsonSerializer.CreateDefault().Deserialize<Dictionary<string, string>>(new JsonTextReader(new StringReader(File.ReadAllText(launcherConfigPath))))["GamePath"], "game", "ffxiv_dx11.exe");
                    Log.Information("Using game installation path configuration from from XIVLauncher: {0}", gamePath);
                }
                catch (Exception)
                {
                    gamePath = @"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\ffxiv_dx11.exe";
                    Log.Warning("Failed to read launcherConfigV3.json. Using default game installation path: {0}", gamePath);
                }

                if (!File.Exists(gamePath))
                {
                    Log.Error("File not found: {0}", gamePath);
                    return -1;
                }
            }

            if (useFakeArguments)
            {
                var gameVersion = File.ReadAllText(Path.Combine(Directory.GetParent(gamePath).FullName, "ffxivgame.ver"));
                var sqpackPath = Path.Combine(Directory.GetParent(gamePath).FullName, "sqpack");
                var maxEntitledExpansionId = 0;
                while (File.Exists(Path.Combine(sqpackPath, $"ex{maxEntitledExpansionId + 1}", $"ex{maxEntitledExpansionId + 1}.ver")))
                    maxEntitledExpansionId++;

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
                    "language=1",
                    $"ver={gameVersion}",
                    $"DEV.MaxEntitledExpansionID={maxEntitledExpansionId}",
                    "DEV.GMServerHost=127.0.0.100",
                    "DEV.GameQuitMessageBox=0",
                });
            }

            var gameArgumentString = string.Join(" ", gameArguments.Select(x => EncodeParameterArgument(x)));
            var process = NativeAclFix.LaunchGame(Path.GetDirectoryName(gamePath), gamePath, gameArgumentString, (Process p) =>
            {
                if (mode == "entrypoint")
                {
                    var startInfo = AdjustStartInfo(dalamudStartInfo, gamePath);
                    Log.Information("Using start info: {0}", JsonConvert.SerializeObject(startInfo));
                    if (RewriteRemoteEntryPointW(p.Handle, gamePath, JsonConvert.SerializeObject(startInfo)) != 0)
                    {
                        Log.Error("[HOOKS] RewriteRemoteEntryPointW failed");
                        throw new Exception("RewriteRemoteEntryPointW failed");
                    }
                }
            });

            if (mode == "inject")
            {
                var startInfo = AdjustStartInfo(dalamudStartInfo, gamePath);
                Log.Information("Using start info: {0}", JsonConvert.SerializeObject(startInfo));
                Inject(process, startInfo);
            }

            var processHandleForOwner = IntPtr.Zero;
            if (handleOwner != IntPtr.Zero)
            {
                if (!DuplicateHandle(Process.GetCurrentProcess().Handle, process.Handle, handleOwner, out processHandleForOwner, 0, false, DuplicateOptions.SameAccess))
                    Log.Warning("Failed to call DuplicateHandle: Win32 error code {0}", Marshal.GetLastWin32Error());
            }

            Console.WriteLine($"{{\"pid\": {process.Id}, \"handle\": {processHandleForOwner}}}");

            return 0;
        }

        private static Process GetInheritableCurrentProcessHandle()
        {
            if (!DuplicateHandle(Process.GetCurrentProcess().Handle, Process.GetCurrentProcess().Handle, Process.GetCurrentProcess().Handle, out var inheritableCurrentProcessHandle, 0, true, DuplicateOptions.SameAccess))
            {
                Log.Error("Failed to call DuplicateHandle: Win32 error code {0}", Marshal.GetLastWin32Error());
                return null;
            }

            return new NativeAclFix.ExistingProcess(inheritableCurrentProcessHandle);
        }

        private static int ProcessLaunchTestCommand(List<string> args)
        {
            Console.WriteLine("Testing launch command.");
            args[0] = Process.GetCurrentProcess().MainModule.FileName;
            args[1] = "launch";

            var inheritableCurrentProcess = GetInheritableCurrentProcessHandle(); // so that it closes the handle when it's done
            args.Insert(2, $"--handle-owner={inheritableCurrentProcess.Handle}");

            for (var i = 0; i < args.Count; i++)
                Console.WriteLine("Argument {0}: {1}", i, args[i]);

            Process helperProcess = new();
            helperProcess.StartInfo.FileName = args[0];
            for (var i = 1; i < args.Count; i++)
                helperProcess.StartInfo.ArgumentList.Add(args[i]);
            helperProcess.StartInfo.RedirectStandardOutput = true;
            helperProcess.StartInfo.RedirectStandardError = true;
            helperProcess.StartInfo.UseShellExecute = false;
            helperProcess.ErrorDataReceived += new DataReceivedEventHandler((sendingProcess, errLine) => Console.WriteLine($"stderr: \"{errLine.Data}\""));
            helperProcess.Start();
            helperProcess.BeginErrorReadLine();
            helperProcess.WaitForExit();
            if (helperProcess.ExitCode != 0)
                return -1;

            var result = JsonSerializer.CreateDefault().Deserialize<Dictionary<string, int>>(new JsonTextReader(helperProcess.StandardOutput));
            var pid = result["pid"];
            var handle = (IntPtr)result["handle"];
            var resultProcess = new NativeAclFix.ExistingProcess(handle);
            Console.WriteLine("PID: {0}, Handle: {1}", pid, handle);
            Console.WriteLine("Press Enter to force quit");
            Console.ReadLine();
            resultProcess.Kill();
            return 0;
        }

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
                Language = ClientLanguage.English,
                GameVersion = gameVer,
                DelayInitializeMs = startInfo.DelayInitializeMs,
            };
        }

        private static void Inject(Process process, DalamudStartInfo startInfo)
        {
            var nethostName = "nethost.dll";
            var bootName = "Dalamud.Boot.dll";

            var nethostPath = Path.GetFullPath(nethostName);
            var bootPath = Path.GetFullPath(bootName);

            // ======================================================

            using var injector = new Injector(process);

            injector.LoadLibrary(nethostPath, out _);
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

            if (exitCode > 0)
            {
                Log.Error($"Dalamud.Boot::Initialize returned {exitCode}");
                return;
            }

            Log.Information("Done");
        }

        [DllImport("Dalamud.Boot.dll")]
        private static extern int RewriteRemoteEntryPointW(IntPtr hProcess, [MarshalAs(UnmanagedType.LPWStr)] string gamePath, [MarshalAs(UnmanagedType.LPWStr)] string loadInfoJson);

        /// <summary>
        ///     This routine appends the given argument to a command line such that
        ///     CommandLineToArgvW will return the argument string unchanged. Arguments
        ///     in a command line should be separated by spaces; this function does
        ///     not add these spaces.
        ///
        ///     Taken from https://stackoverflow.com/questions/5510343/escape-command-line-arguments-in-c-sharp
        ///     and https://blogs.msdn.microsoft.com/twistylittlepassagesallalike/2011/04/23/everyone-quotes-command-line-arguments-the-wrong-way/.
        /// </summary>
        /// <param name="argument">Supplies the argument to encode.</param>
        /// <param name="force">
        ///     Supplies an indication of whether we should quote the argument even if it 
        ///     does not contain any characters that would ordinarily require quoting.
        /// </param>
        private static string EncodeParameterArgument(string argument, bool force = false)
        {
            if (argument == null) throw new ArgumentNullException(nameof(argument));

            // Unless we're told otherwise, don't quote unless we actually
            // need to do so --- hopefully avoid problems if programs won't
            // parse quotes properly
            if (force == false
                && argument.Length > 0
                && argument.IndexOfAny(" \t\n\v\"".ToCharArray()) == -1)
            {
                return argument;
            }

            var quoted = new StringBuilder();
            quoted.Append('"');

            var numberBackslashes = 0;

            foreach (var chr in argument)
            {
                switch (chr)
                {
                    case '\\':
                        numberBackslashes++;
                        continue;
                    case '"':
                        // Escape all backslashes and the following
                        // double quotation mark.
                        quoted.Append('\\', (numberBackslashes * 2) + 1);
                        quoted.Append(chr);
                        break;
                    default:
                        // Backslashes aren't special here.
                        quoted.Append('\\', numberBackslashes);
                        quoted.Append(chr);
                        break;
                }

                numberBackslashes = 0;
            }

            // Escape all backslashes, but let the terminating
            // double quotation mark we add below be interpreted
            // as a metacharacter.
            quoted.Append('\\', numberBackslashes * 2);
            quoted.Append('"');

            return quoted.ToString();
        }
    }
}
