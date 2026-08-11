using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

using Dalamud.Common;
using Dalamud.Common.Game;
using Dalamud.Common.Util;
using Newtonsoft.Json;
using Reloaded.Memory.Buffers;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Dalamud.Injector
{
    /// <summary>
    /// Entrypoint to the program.
    /// </summary>
    public sealed class Program
    {
        /// <summary>
        /// Start the Dalamud injector.
        /// </summary>
        /// <param name="argsArray">Command line arguments.</param>
        /// <returns>Return value (HRESULT).</returns>
        public static int Main(string[] argsArray)
        {
            try
            {
                // API14 TODO: Refactor
                var args = argsArray.ToList();
                args.Insert(0, Assembly.GetExecutingAssembly().Location);

                Init(args);
                args.Remove("-v"); // Remove "verbose" flag

                if (args.Count >= 2 && args[1].ToLowerInvariant() == "launch-test")
                {
                    return ProcessLaunchTestCommand(args);
                }

                DalamudStartInfo? startInfo = null;
                if (args.Count == 1)
                {
                    // No command defaults to inject
                    args.Add("inject");
                    args.Add("--all");

    #if !DEBUG
                    args.Add("--warn");
    #endif

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
                // Remove already handled arguments
                args.Remove("--debug-directx");
                args.Remove("--console");
                args.Remove("--msgbox1");
                args.Remove("--msgbox2");
                args.Remove("--msgbox3");
                args.Remove("--etw");
                args.Remove("--no-legacy-corrupted-state-exceptions");
                args.Remove("--veh");
                args.Remove("--veh-full");
                args.Remove("--no-plugin");
                args.Remove("--no-3rd-plugin");
                args.Remove("--crash-handler-console");

                var mainCommand = args[1].ToLowerInvariant();
                if (mainCommand == "sandbox-prepare")
                {
                    return ProcessSandboxPrepareCommand(args, startInfo);
                }
                else if (mainCommand.Length > 0 && mainCommand.Length <= 6 && "inject"[..mainCommand.Length] == mainCommand)
                {
                    return ProcessInjectCommand(args, startInfo);
                }
                else if (mainCommand.Length > 0 && mainCommand.Length <= 6 &&
                         "launch"[..mainCommand.Length] == mainCommand)
                {
                    return ProcessLaunchCommand(args, startInfo);
                }
                else if (mainCommand.Length > 0 && mainCommand.Length <= 4 &&
                         "help"[..mainCommand.Length] == mainCommand)
                {
                    return ProcessHelpCommand(args, args.Count >= 3 ? args[2] : null);
                }
                else
                {
                    throw new CommandLineException($"\"{mainCommand}\" is not a valid command.");
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Operation failed.");
                return e.HResult;
            }
        }

        private static string GetLogPath(string? baseDirectory, string fileName, string? logName)
        {
            baseDirectory ??= Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            baseDirectory ??= Environment.CurrentDirectory;
            fileName = !string.IsNullOrEmpty(logName) ? $"{fileName}-{logName}.log" : $"{fileName}.log";

            // TODO(api9): remove
            var previousLogPath = Path.Combine(baseDirectory, "..", "..", "..", fileName);
            if (File.Exists(previousLogPath))
                File.Delete(previousLogPath);

            return Path.Combine(baseDirectory, fileName);
        }

        private static void Init(List<string> args)
        {
            InitLogging(args.Any(x => x == "-v"), args);
            InitUnhandledException(args);

            var cwd = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory
                      ?? throw new DirectoryNotFoundException("Could not determine binary location.");

            if (cwd.FullName != Directory.GetCurrentDirectory())
            {
                Log.Debug($"Changing cwd to {cwd}");
                Directory.SetCurrentDirectory(cwd.FullName);
            }
        }

        private static void InitUnhandledException(List<string> args)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                var exObj = eventArgs.ExceptionObject;

                if (exObj is CommandLineException clex)
                {
                    Console.WriteLine();
                    Console.WriteLine("Command line error: {0}", clex.Message);
                    Console.WriteLine();
                    ProcessHelpCommand(args);
                }
                else if (Log.Logger == null)
                {
                    Console.WriteLine($"A fatal error has occurred: {eventArgs.ExceptionObject}");
                }
                else if (exObj is Exception ex)
                {
                    Log.Error(ex, "A fatal error has occurred");
                }
                else
                {
                    Log.Error("A fatal error has occurred: {Exception}", eventArgs.ExceptionObject.ToString());
                }

                Log.CloseAndFlush();
                Environment.Exit(-1);
            };
        }

        private static void InitLogging(bool verbose, IEnumerable<string> args)
        {
            var levelSwitch = new LoggingLevelSwitch
            {
                MinimumLevel = verbose ? LogEventLevel.Verbose : LogEventLevel.Information,
            };

            var logName = args.FirstOrDefault(x => x.StartsWith("--logname="))?[10..];
            var logBaseDir = args.FirstOrDefault(x => x.StartsWith("--logpath="))?[10..];
            var logPath = GetLogPath(logBaseDir, "dalamud.injector", logName);

            CullLogFile(logPath, 1 * 1024 * 1024);

            const long maxLogSize = 100 * 1024 * 1024; // 100MB
            Log.Logger = new LoggerConfiguration()
                         .WriteTo.Console(standardErrorFromLevel: LogEventLevel.Debug)
                         .WriteTo.File(logPath, fileSizeLimitBytes: maxLogSize)
                         .MinimumLevel.ControlledBy(levelSwitch)
                         .CreateLogger();

            Log.Information(new string('-', 80));
            Log.Information("Dalamud.Injector, (c) 2023 XIVLauncher Contributors");
        }

        private static void CullLogFile(string logPath, int cullingFileSize)
        {
            try
            {
                var bufferSize = 4096;

                var logFile = new FileInfo(logPath);

                // Leave it to serilog
                if (!logFile.Exists)
                {
                    return;
                }

                if (logFile.Length <= cullingFileSize)
                {
                    return;
                }

                var amountToCull = logFile.Length - cullingFileSize;

                if (amountToCull < bufferSize)
                {
                    return;
                }

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

        private static OSPlatform DetectPlatformHeuristic()
        {
            var ntdll = Windows.Win32.PInvoke.GetModuleHandle("ntdll.dll");
            var wineServerCallPtr = Windows.Win32.PInvoke.GetProcAddress(ntdll, "wine_server_call");
            var wineGetHostVersionPtr = Windows.Win32.PInvoke.GetProcAddress(ntdll, "wine_get_host_version");
            var winePlatform = GetWinePlatform(wineGetHostVersionPtr);
            var isWine = wineServerCallPtr != nint.Zero;

            static unsafe string? GetWinePlatform(nint wineGetHostVersionPtr)
            {
                if (wineGetHostVersionPtr == nint.Zero) return null;

                var methodDelegate = (delegate* unmanaged[Cdecl]<out char*, out char*, void>)wineGetHostVersionPtr;
                methodDelegate(out var platformPtr, out var _);

                if (platformPtr == null) return null;

                return Marshal.PtrToStringAnsi((nint)platformPtr);
            }

            if (!isWine)
                return OSPlatform.Windows;

            if (winePlatform == "Darwin")
                return OSPlatform.OSX;

            return OSPlatform.Linux;
        }

        private static DalamudStartInfo ExtractAndInitializeStartInfoFromArguments(DalamudStartInfo? startInfo, List<string> args)
        {
            int len;
            string key;

            startInfo ??= new DalamudStartInfo();

            var workingDirectory = startInfo.WorkingDirectory;
            var configurationPath = startInfo.ConfigurationPath;
            var pluginDirectory = startInfo.PluginDirectory;
            var assetDirectory = startInfo.AssetDirectory;
            var tempDirectory = startInfo.TempDirectory;
            var delayInitializeMs = startInfo.DelayInitializeMs;
            var logName = startInfo.LogName;
            var logPath = startInfo.LogPath;
            var languageStr = startInfo.Language.ToString().ToLowerInvariant();
            var platformStr = startInfo.Platform.ToString().ToLowerInvariant();
            var unhandledExceptionStr = startInfo.UnhandledException.ToString().ToLowerInvariant();
            var troubleshootingData = "{\"empty\": true, \"description\": \"No troubleshooting data supplied.\"}";

            // env vars are brought in prior to launch args, since args can override them.
            if (EnvironmentUtils.TryGetEnvironmentVariable("XL_PLATFORM", out var xlPlatformEnv))
                platformStr = xlPlatformEnv.ToLowerInvariant();

            for (var i = 2; i < args.Count; i++)
            {
                if (args[i].StartsWith(key = "--dalamud-working-directory="))
                {
                    workingDirectory = args[i][key.Length..];
                }
                else if (args[i].StartsWith(key = "--dalamud-configuration-path="))
                {
                    configurationPath = args[i][key.Length..];
                }
                else if (args[i].StartsWith(key = "--dalamud-plugin-directory="))
                {
                    pluginDirectory = args[i][key.Length..];
                }
                else if (args[i].StartsWith(key = "--dalamud-asset-directory="))
                {
                    assetDirectory = args[i][key.Length..];
                }
                else if (args[i].StartsWith(key = "--dalamud-temp-directory="))
                {
                    tempDirectory = args[i][key.Length..];
                }
                else if (args[i].StartsWith(key = "--dalamud-delay-initialize="))
                {
                    delayInitializeMs = int.Parse(args[i][key.Length..]);
                }
                else if (args[i].StartsWith(key = "--dalamud-client-language="))
                {
                    languageStr = args[i][key.Length..].ToLowerInvariant();
                }
                else if (args[i].StartsWith(key = "--dalamud-platform="))
                {
                    platformStr = args[i][key.Length..].ToLowerInvariant();
                }
                else if (args[i].StartsWith(key = "--dalamud-tspack-b64="))
                {
                    troubleshootingData = Encoding.UTF8.GetString(Convert.FromBase64String(args[i][key.Length..]));
                }
                else if (args[i].StartsWith(key = "--logname="))
                {
                    logName = args[i][key.Length..];
                }
                else if (args[i].StartsWith(key = "--logpath="))
                {
                    logPath = args[i][key.Length..];
                }
                else if (args[i].StartsWith(key = "--unhandled-exception="))
                {
                    unhandledExceptionStr = args[i][key.Length..];
                }
                else
                {
                    continue;
                }

                args.RemoveAt(i);
                i--;
            }

            var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var xivlauncherDir = Path.Combine(appDataDir, "XIVLauncher");

            workingDirectory ??= Directory.GetCurrentDirectory();
            configurationPath ??= Path.Combine(xivlauncherDir, "dalamudConfig.json");
            pluginDirectory ??= Path.Combine(xivlauncherDir, "installedPlugins");
            assetDirectory ??= Path.Combine(xivlauncherDir, "dalamudAssets", "dev");

            ClientLanguage clientLanguage;
            if (languageStr[0..(len = Math.Min(languageStr.Length, (key = "english").Length))] == key[0..len])
            {
                clientLanguage = ClientLanguage.English;
            }
            else if (languageStr[0..(len = Math.Min(languageStr.Length, (key = "japanese").Length))] == key[0..len])
            {
                clientLanguage = ClientLanguage.Japanese;
            }
            else if (languageStr[0..(len = Math.Min(languageStr.Length, (key = "日本語").Length))] == key[0..len])
            {
                clientLanguage = ClientLanguage.Japanese;
            }
            else if (languageStr[0..(len = Math.Min(languageStr.Length, (key = "german").Length))] == key[0..len])
            {
                clientLanguage = ClientLanguage.German;
            }
            else if (languageStr[0..(len = Math.Min(languageStr.Length, (key = "deutsch").Length))] == key[0..len])
            {
                clientLanguage = ClientLanguage.German;
            }
            else if (languageStr[0..(len = Math.Min(languageStr.Length, (key = "french").Length))] == key[0..len])
            {
                clientLanguage = ClientLanguage.French;
            }
            else if (languageStr[0..(len = Math.Min(languageStr.Length, (key = "français").Length))] == key[0..len])
            {
                clientLanguage = ClientLanguage.French;
            }
            else if (int.TryParse(languageStr, out var languageInt) && Enum.IsDefined((ClientLanguage)languageInt))
            {
                clientLanguage = (ClientLanguage)languageInt;
            }
            else
            {
                throw new CommandLineException($"\"{languageStr}\" is not a valid supported language.");
            }

            OSPlatform platform;

            // covers both win32 and Windows
            if (platformStr[0..(len = Math.Min(platformStr.Length, (key = "win").Length))] == key[0..len])
            {
                platform = OSPlatform.Windows;
            }
            else if (platformStr[0..(len = Math.Min(platformStr.Length, (key = "linux").Length))] == key[0..len])
            {
                platform = OSPlatform.Linux;
            }
            else if (platformStr[0..(len = Math.Min(platformStr.Length, (key = "macos").Length))] == key[0..len])
            {
                platform = OSPlatform.OSX;
            }
            else if (platformStr[0..(len = Math.Min(platformStr.Length, (key = "osx").Length))] == key[0..len])
            {
                platform = OSPlatform.OSX;
            }
            else
            {
                platform = DetectPlatformHeuristic();
                Log.Warning("Heuristically determined host system platform as {platform}", platform);
            }

            startInfo.WorkingDirectory = workingDirectory;
            startInfo.ConfigurationPath = configurationPath;
            startInfo.PluginDirectory = pluginDirectory;
            startInfo.AssetDirectory = assetDirectory;
            startInfo.TempDirectory = tempDirectory;
            startInfo.Language = clientLanguage;
            startInfo.Platform = platform;
            startInfo.DelayInitializeMs = delayInitializeMs;
            startInfo.GameVersion = null;
            startInfo.TroubleshootingPackData = troubleshootingData;
            startInfo.LogName = logName;
            startInfo.LogPath = logPath;

            // TODO: XL should set --logpath to its roaming path. We are only doing this here until that's rolled out.
#if DEBUG
            startInfo.LogPath ??= startInfo.WorkingDirectory;
#else
            startInfo.LogPath ??= xivlauncherDir;
#endif
            startInfo.LogName ??= string.Empty;

            // Set boot defaults
            startInfo.BootDebugDirectX = args.Contains("--debug-directx");
            startInfo.BootShowConsole = args.Contains("--console");
            startInfo.BootEnableEtw = args.Contains("--etw");
            startInfo.BootLogPath = GetLogPath(startInfo.LogPath, "dalamud.boot", startInfo.LogName);
            startInfo.BootEnabledGameFixes = new()
            {
                // See: xivfixes.h, xivfixes.cpp
                "prevent_devicechange_crashes",
                "disable_game_openprocess_access_check",
                "redirect_openprocess",
                "backup_userdata_save",
                "prevent_icmphandle_crashes",
                "symbol_load_patches",
                "disable_game_debugging_protection",
                "faster_decompression",
                "appcontainer_known_folders",
            };
            startInfo.BootDotnetOpenProcessHookMode = 0;
            startInfo.BootWaitMessageBox |= args.Contains("--msgbox1") ? 1 : 0;
            startInfo.BootWaitMessageBox |= args.Contains("--msgbox2") ? 2 : 0;
            startInfo.BootWaitMessageBox |= args.Contains("--msgbox3") ? 4 : 0;
            // startInfo.BootVehEnabled = args.Contains("--veh");
            startInfo.BootVehEnabled = true;
            startInfo.BootVehFull = args.Contains("--veh-full");
            startInfo.NoLoadPlugins = args.Contains("--no-plugin");
            startInfo.NoLoadThirdPartyPlugins = args.Contains("--no-3rd-plugin");
            // startInfo.BootUnhookDlls = new List<string>() { "kernel32.dll", "ntdll.dll", "user32.dll" };
            startInfo.CrashHandlerShow = args.Contains("--crash-handler-console");
            startInfo.UnhandledException =
                Enum.TryParse<UnhandledExceptionHandlingMode>(
                    unhandledExceptionStr,
                    true,
                    out var parsedUnhandledException)
                    ? parsedUnhandledException
                    : throw new CommandLineException(
                          $"\"{unhandledExceptionStr}\" is not a valid unhandled exception handling mode.");

            return startInfo;
        }

        private static int ProcessHelpCommand(List<string> args, string? particularCommand = default)
        {
            var exeName = Path.GetFileName(args[0]);

            var exeSpaces = string.Empty;
            for (var i = exeName.Length; i > 0; i--)
                exeSpaces += " ";

            if (particularCommand is null or "help")
            {
                Console.WriteLine("{0} help [command]", exeName);
            }

            if (particularCommand is null or "inject")
            {
                Console.WriteLine("{0} inject [-h/--help] [-a/--all] [--warn] [--fix-acl] [--se-debug-privilege] [pid1] [pid2] [pid3] ...", exeName);
            }

            if (particularCommand is null or "launch")
            {
                Console.WriteLine("{0} launch [-h/--help] [-f/--fake-arguments]", exeName);
                Console.WriteLine("{0}        [-g path/to/ffxiv_dx11.exe] [--game=path/to/ffxiv_dx11.exe]", exeSpaces);
                Console.WriteLine("{0}        [-m entrypoint|inject] [--mode=entrypoint|inject]", exeSpaces);
                Console.WriteLine("{0}        [--handle-owner=inherited-handle-value]", exeSpaces);
                Console.WriteLine("{0}        [--without-dalamud] [--no-fix-acl]", exeSpaces);
                Console.WriteLine("{0}        [--no-wait]", exeSpaces);
                Console.WriteLine("{0}        [--sandbox] [--no-sandbox] [--sandbox-config=path/to/dalamudSandbox.json]", exeSpaces);
                Console.WriteLine("{0}        [-- game_arg1=value1 game_arg2=value2 ...]", exeSpaces);
            }

            if (particularCommand is null or "sandbox-prepare")
            {
                Console.WriteLine("{0} sandbox-prepare [-h/--help] [-g path/to/ffxiv_dx11.exe] [--game=path/to/ffxiv_dx11.exe]", exeName);
                Console.WriteLine("{0}                 [--sandbox-config=path/to/dalamudSandbox.json] [--write-config]", exeSpaces);
                Console.WriteLine("{0}   Prepares the environment to properly run with --sandbox. Run once from an", exeSpaces);
                Console.WriteLine("{0}   elevated command prompt. Paths you don't own and the loopback excemption require running with this at least once.", exeSpaces);
                Console.WriteLine("{0}   Sandboxing applies to every launch when \"enabledGlobally\" is set in the sandbox configuration.", exeSpaces);
                Console.WriteLine("{0}   --no-sandbox opts a single launch out of that.", exeSpaces);
                Console.WriteLine("{0}   --write-config creates a configuration at the default location, if there isn't one yet.", exeSpaces);
            }

            Console.WriteLine("Specifying dalamud start info: [--dalamud-working-directory=path] [--dalamud-configuration-path=path]");
            Console.WriteLine("                               [--dalamud-plugin-directory=path] [--dalamud-platform=win32|linux|macOS]");
            Console.WriteLine("                               [--dalamud-asset-directory=path] [--dalamud-delay-initialize=0(ms)]");
            Console.WriteLine("                               [--dalamud-client-language=0-3|j(apanese)|e(nglish)|d|g(erman)|f(rench)]");

            Console.WriteLine("Verbose logging:\t[-v]");
            Console.WriteLine("Show Console:\t[--console] [--crash-handler-console]");
            Console.WriteLine("Enable ETW:\t[--etw]");
            Console.WriteLine("Disable legacy corrupted state exceptions:\t[--no-legacy-corrupted-state-exceptions]");
            Console.WriteLine("Enable VEH:\t[--veh], [--veh-full], [--unhandled-exception=default|stalldebug|none]");
            Console.WriteLine("Show messagebox:\t[--msgbox1], [--msgbox2], [--msgbox3]");
            Console.WriteLine("No plugins:\t[--no-plugin] [--no-3rd-plugin]");
            Console.WriteLine("Logging:\t[--logname=<logfile suffix>] [--logpath=<log base directory>]");

            return 0;
        }

        private static int ProcessInjectCommand(List<string> args, DalamudStartInfo dalamudStartInfo)
        {
            List<Process> processes = new();

            var targetProcessSpecified = false;
            var warnManualInjection = false;
            var showHelp = args.Count <= 2;
            var tryFixAcl = false;
            var tryClaimSeDebugPrivilege = false;

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
                else if (args[i] == "--fix-acl" || args[i] == "--acl-fix")
                {
                    tryFixAcl = true;
                }
                else if (args[i] == "--se-debug-privilege")
                {
                    tryClaimSeDebugPrivilege = true;
                }
                else if (args[i] == "--warn")
                {
                    warnManualInjection = true;
                }
                else
                {
                    Log.Warning($"\"{args[i]}\" is not a valid command line argument, ignoring.");
                }
            }

            if (showHelp)
            {
                ProcessHelpCommand(args, "inject");
                return args.Count <= 2 ? -1 : 0;
            }

            if (!targetProcessSpecified)
            {
                throw new CommandLineException("No target process has been specified. Use -a(--all) option to inject to all ffxiv_dx11.exe processes.");
            }
            else if (!processes.Any())
            {
                Log.Error("No suitable target process has been found.");
                return -1;
            }

            if (warnManualInjection)
            {
                var result = Windows.Win32.PInvoke.MessageBox(
                    HWND.Null,
                    $"Take care: you are manually injecting Dalamud into FFXIV({string.Join(", ", processes.Select(x => $"{x.Id}"))}).\n\nIf you are doing this to use plugins before they are officially whitelisted on patch days, things may go wrong and you may get into trouble.\nWe discourage you from doing this and you won't be warned again in-game.",
                    "Dalamud",
                    MESSAGEBOX_STYLE.MB_ICONWARNING | MESSAGEBOX_STYLE.MB_OKCANCEL);

                if (result == MESSAGEBOX_RESULT.IDCANCEL)
                {
                    Log.Information("User cancelled injection");
                    return -2;
                }
            }

            if (tryClaimSeDebugPrivilege)
            {
                try
                {
                    GameStart.ClaimSeDebug();
                    Log.Information("SeDebugPrivilege claimed.");
                }
                catch (Win32Exception e2)
                {
                    Log.Warning(e2, "Failed to claim SeDebugPrivilege");
                }
            }

            foreach (var process in processes)
            {
                var processBinaryPath = process.MainModule?.FileName
                    ?? throw new CommandLineException($"Could not determine binary path for process {process.Id}.");

                Inject(process, AdjustStartInfo(dalamudStartInfo, processBinaryPath), tryFixAcl);
            }

            Log.CloseAndFlush();
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
            var withoutDalamud = false;
            var noFixAcl = false;
            var waitForGameWindow = true;
            var encryptArguments = false;

            // null = not specified on command line, use config
            bool? useSandbox = null;
            string? sandboxConfigPath = null;

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
                else if (args[i] == "--without-dalamud")
                {
                    withoutDalamud = true;
                }
                else if (args[i] == "--no-wait")
                {
                    waitForGameWindow = false;
                }
                else if (args[i] == "--no-fix-acl" || args[i] == "--no-acl-fix")
                {
                    noFixAcl = true;
                }
                else if (args[i] == "--sandbox")
                {
                    useSandbox = true;
                }
                else if (args[i] == "--no-sandbox")
                {
                    useSandbox = false;
                }
                else if (args[i].StartsWith("--sandbox-config="))
                {
                    // When using --sandbox-config assume sandboxing, but never when --no-sandbox
                    useSandbox ??= true;
                    sandboxConfigPath = args[i].Split('=', 2)[1];
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
                    mode = args[i].Split('=', 2)[1];
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
                    Log.Warning($"\"{args[i]}\" is not a valid command line argument, ignoring.");
                }
            }

            var checksumTable = "fX1pGtdS5CAP4_VL";
            var argDelimiterRegex = new Regex(" (?<!(?:^|[^ ])(?:  )*)/");
            var kvDelimiterRegex = new Regex(" (?<!(?:^|[^ ])(?:  )*)=");
            gameArguments = gameArguments.SelectMany(x =>
            {
                if (!x.StartsWith("//**sqex0003") || !x.EndsWith("**//"))
                {
                    return new List<string>() { x };
                }

                var checksum = checksumTable.IndexOf(x[x.Length - 5]);
                if (checksum == -1)
                {
                    return new List<string>() { x };
                }

                var encData = Convert.FromBase64String(x.Substring(12, x.Length - 12 - 5).Replace('-', '+').Replace('_', '/').Replace('*', '='));
                var rawData = new byte[encData.Length];

                for (var i = (uint)checksum; i < 0x10000u; i += 0x10)
                {
                    var bf = new LegacyBlowfish(Encoding.UTF8.GetBytes($"{i << 16:x08}"));
                    Buffer.BlockCopy(encData, 0, rawData, 0, rawData.Length);
                    bf.Decrypt(ref rawData);
                    var rawString = Encoding.UTF8.GetString(rawData).Split('\0', 2).First();
                    encryptArguments = true;
                    var args = argDelimiterRegex.Split(rawString).Skip(1).Select(y => string.Join('=', kvDelimiterRegex.Split(y, 2)).Replace("  ", " ")).ToList();
                    if (!args.Any())
                    {
                        continue;
                    }

                    if (!args.First().StartsWith("T="))
                    {
                        continue;
                    }

                    if (!uint.TryParse(args.First().Substring(2), out var tickCount))
                    {
                        continue;
                    }

                    if (tickCount >> 16 != i)
                    {
                        continue;
                    }

                    return args.Skip(1);
                }

                return new List<string>() { x };
            }).ToList();

            if (showHelp)
            {
                ProcessHelpCommand(args, "launch");
                return args.Count <= 2 ? -1 : 0;
            }

            mode = mode == null ? "entrypoint" : mode.ToLowerInvariant();
            if (mode.Length > 0 && mode.Length <= 10 && "entrypoint"[0..mode.Length] == mode)
            {
                dalamudStartInfo.LoadMethod = LoadMethod.Entrypoint;
            }
            else if (mode.Length > 0 && mode.Length <= 6 && "inject"[0..mode.Length] == mode)
            {
                dalamudStartInfo.LoadMethod = LoadMethod.DllInject;
            }
            else
            {
                throw new CommandLineException($"\"{mode}\" is not a valid Dalamud load mode.");
            }

            if (gamePath == null)
            {
                gamePath = ResolveGamePath(dalamudStartInfo);
                if (gamePath == null)
                    return -1;
            }

            if (useFakeArguments)
            {
                var gameParent = Directory.GetParent(gamePath)?.FullName
                    ?? throw new DirectoryNotFoundException($"Could not determine parent directory of {gamePath}.");

                var gameVersion = File.ReadAllText(Path.Combine(gameParent, "ffxivgame.ver"));
                var sqpackPath = Path.Combine(gameParent, "sqpack");
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
                    $"language={(int)dalamudStartInfo.Language}",
                    $"ver={gameVersion}",
                    $"DEV.MaxEntitledExpansionID={maxEntitledExpansionId}",
                    "DEV.GMServerHost=127.0.0.100",
                    "DEV.GameQuitMessageBox=0",
                });
            }

            string gameArgumentString;
            if (encryptArguments)
            {
                var rawTickCount = (uint)Environment.TickCount;
                var ticks = rawTickCount & 0xFFFF_FFFFu;
                var key = ticks & 0xFFFF_0000u;
                gameArguments.Insert(0, $"T={ticks}");

                var escapeValue = (string x) => x.Replace(" ", "  ");
                gameArgumentString = gameArguments.Select(x => x.Split('=', 2)).Aggregate(new StringBuilder(), (whole, part) => whole.Append($" /{escapeValue(part[0])} ={escapeValue(part.Length > 1 ? part[1] : string.Empty)}")).ToString();
                var bf = new LegacyBlowfish(Encoding.UTF8.GetBytes($"{key:x08}"));
                var ciphertext = bf.Encrypt(Encoding.UTF8.GetBytes(gameArgumentString));
                var base64Str = Convert.ToBase64String(ciphertext).Replace('+', '-').Replace('/', '_').Replace('=', '*');
                var checksum = checksumTable[(int)(key >> 16) & 0xF];
                gameArgumentString = $"//**sqex0003{base64Str}{checksum}**//";
            }
            else
            {
                gameArgumentString = string.Join(" ", gameArguments.Select(x => EncodeParameterArgument(x)));
            }

            AppContainerLaunchContext? sandboxContext = null;
            if (useSandbox != false)
            {
                var sandboxConfig = SandboxConfiguration.Load(
                    sandboxConfigPath ?? SandboxConfiguration.DefaultPath,
                    sandboxConfigPath != null);

                if (useSandbox != true && sandboxConfig.EnabledGlobally)
                    Log.Information("[SANDBOX] Enabled by configuration at '{DefaultConfigPath}'. Pass --no-sandbox to disable.", SandboxConfiguration.DefaultPath);

                if (useSandbox == true || sandboxConfig.EnabledGlobally)
                    sandboxContext = SetupSandbox(dalamudStartInfo, sandboxConfig, gamePath);
            }

            Process process;
            try
            {
                process = GameStart.LaunchGame(
                    Path.GetDirectoryName(gamePath) ?? throw new DirectoryNotFoundException($"Could not determine parent directory of {gamePath}."),
                    gamePath,
                    gameArgumentString,
                    noFixAcl,
                    p =>
                    {
                        if (!withoutDalamud && dalamudStartInfo.LoadMethod == LoadMethod.Entrypoint)
                        {
                            var startInfo = AdjustStartInfo(dalamudStartInfo, gamePath);
                            Log.Information("Using start info: {0}", JsonConvert.SerializeObject(startInfo));
                            Marshal.ThrowExceptionForHR(
                                RewriteRemoteEntryPointW(p.Handle, gamePath, JsonConvert.SerializeObject(startInfo)));
                            Log.Verbose("RewriteRemoteEntryPointW called!");
                        }
                    },
                    waitForGameWindow,
                    sandboxContext);
            }
            finally
            {
                sandboxContext?.Dispose();
            }

            Log.Verbose("Game process started with PID {0}", process.Id);

            if (!withoutDalamud && dalamudStartInfo.LoadMethod == LoadMethod.DllInject)
            {
                var startInfo = AdjustStartInfo(dalamudStartInfo, gamePath);
                Log.Information("Using start info: {0}", JsonConvert.SerializeObject(startInfo));
                Inject(process, startInfo, false);
            }

            var processHandleForOwner = HANDLE.Null;
            if (handleOwner != IntPtr.Zero)
            {
                unsafe
                {
                    if (!Windows.Win32.PInvoke.DuplicateHandle(
                            new HANDLE(Process.GetCurrentProcess().Handle.ToPointer()),
                            new HANDLE(process.Handle.ToPointer()),
                            new HANDLE(handleOwner),
                            &processHandleForOwner,
                            0,
                            false,
                            DUPLICATE_HANDLE_OPTIONS.DUPLICATE_SAME_ACCESS))
                    {
                        Log.Warning("Failed to call DuplicateHandle: Win32 error code {0}", Marshal.GetLastWin32Error());
                    }
                }
            }

            Console.WriteLine($"{{\"pid\": {process.Id}, \"handle\": {(IntPtr)processHandleForOwner}}}");

            Log.CloseAndFlush();
            return 0;
        }

        /// <summary>
        /// Prepare the AppContainer sandbox by loading the config and changing/migrating paths as necessary.
        /// </summary>
        private static SandboxLayout BuildSandboxLayout(DalamudStartInfo startInfo, SandboxConfiguration config, string gamePath)
        {
            var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var xivlauncherDir = Path.Combine(appDataDir, "XIVLauncher");

            // XL root must never be readable or writable from the sandbox, so we need to move all data
            // into a subfolder that we can grant access to
            var dataDir = Path.Combine(xivlauncherDir, "dalamudUserData");
            var logsDir = Path.Combine(dataDir, "logs");
            var tempDir = Path.Combine(dataDir, "temp");
            Directory.CreateDirectory(dataDir);
            Directory.CreateDirectory(logsDir);
            Directory.CreateDirectory(tempDir);

            var configDir = Path.GetDirectoryName(Path.GetFullPath(startInfo.ConfigurationPath!));
            if (PathsEqual(configDir, xivlauncherDir))
            {
                var configFileName = Path.GetFileName(startInfo.ConfigurationPath!);
                MigrateFileToSandbox(Path.Combine(xivlauncherDir, configFileName), Path.Combine(dataDir, configFileName));
                MigrateFileToSandbox(Path.Combine(xivlauncherDir, "dalamudVfs.db"), Path.Combine(dataDir, "dalamudVfs.db"));
                MigrateFileToSandbox(Path.Combine(xivlauncherDir, "dalamudUI.ini"), Path.Combine(dataDir, "dalamudUI.ini"));
                MigrateDirectoryToSandbox(Path.Combine(xivlauncherDir, "pluginConfigs"), Path.Combine(dataDir, "pluginConfigs"));

                startInfo.ConfigurationPath = Path.Combine(dataDir, configFileName);
                Log.Information("[SANDBOX] Redirected configuration path to {Path}", startInfo.ConfigurationPath);
            }

            if (PathsEqual(startInfo.LogPath, xivlauncherDir) ||
                IsAtOrUnder(startInfo.LogPath, startInfo.WorkingDirectory))
            {
                startInfo.LogPath = logsDir;
                Log.Information("[SANDBOX] Redirected log path to {Path}", startInfo.LogPath);
            }

            startInfo.BootLogPath = GetLogPath(startInfo.LogPath, "dalamud.boot", startInfo.LogName);

            var grants = new List<SandboxGrant>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Add(string? path, uint access, bool create = false)
            {
                if (string.IsNullOrEmpty(path))
                    return;

                path = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
                if (!seen.Add($"{path}|{access}"))
                    return;

                grants.Add(new SandboxGrant(path, access, create));
            }

            // Read/execute for binaries and static data
            var runtimeDir = Environment.GetEnvironmentVariable("DALAMUD_RUNTIME") ?? Path.Combine(xivlauncherDir, "runtime");
            Add(Path.GetDirectoryName(gamePath), AppContainerHelper.AccessReadExecute);
            Add(startInfo.WorkingDirectory, AppContainerHelper.AccessReadExecute);
            Add(runtimeDir, AppContainerHelper.AccessReadExecute);
            Add(startInfo.AssetDirectory, AppContainerHelper.AccessReadExecute);

            // Modify for state
            if (!string.IsNullOrEmpty(startInfo.AssetDirectory))
                Add(Path.Combine(startInfo.AssetDirectory, "..", "local"),  AppContainerHelper.AccessModify, true);

            Add(startInfo.PluginDirectory, AppContainerHelper.AccessModify, true);
            Add(dataDir, AppContainerHelper.AccessModify);
            Add(Path.GetDirectoryName(startInfo.ConfigurationPath!), AppContainerHelper.AccessModify);
            Add(startInfo.LogPath, AppContainerHelper.AccessModify);
            Add(Path.Combine(xivlauncherDir, "devPlugins"), AppContainerHelper.AccessModify);
            Add(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "FINAL FANTASY XIV - A Realm Reborn"),
                AppContainerHelper.AccessModify,
                true);
            if (!string.IsNullOrEmpty(startInfo.TempDirectory))
                Add(startInfo.TempDirectory, AppContainerHelper.AccessModify, true);

            foreach (var allowed in config.AllowedPaths)
            {
                Add(
                    Environment.ExpandEnvironmentVariables(allowed.Path),
                    allowed.Write ? AppContainerHelper.AccessModify : AppContainerHelper.AccessReadExecute);
            }

            // Sanity check all grants against folders that we should absolutely never grant modify to
            foreach (var grant in grants.Where(x => x.Access == AppContainerHelper.AccessModify))
            {
                string? violated = null;
                if (PathsEqual(grant.Path, xivlauncherDir))
                    violated = "the XIVLauncher root";
                else if (IsAtOrUnder(grant.Path, startInfo.WorkingDirectory))
                    violated = "Dalamud's working directory (holds Dalamud's own binaries)";
                else if (IsAtOrUnder(grant.Path, runtimeDir))
                    violated = "the .NET runtime directory";
                else if (IsAtOrUnder(grant.Path, Path.GetDirectoryName(gamePath)))
                    violated = "the game installation directory";

                if (violated != null)
                {
                    throw new CommandLineException(
                        $"Refusing to grant the sandbox write access to {grant.Path}: it is inside {violated}. " +
                        "This must stay read-only for the sandbox to be meaningful.");
                }
            }

            return new SandboxLayout(config, tempDir, runtimeDir, grants);
        }

        /// <summary>
        /// Apply or verify the sandbox layout's grants against the actual container.
        /// Paths whose DACL could not be written are returned rather than throwing, since we might not
        /// have permissions to set DACLs on some objects and need to tell the user that elevation is required.
        /// </summary>
        private static List<SandboxGrant> ApplySandboxGrants(SandboxLayout layout, AppContainerLaunchContext ctx, bool verbose)
        {
            var denied = new List<SandboxGrant>();

            foreach (var grant in layout.Grants)
            {
                if (grant.Create)
                    Directory.CreateDirectory(grant.Path);

                if (!Directory.Exists(grant.Path) && !File.Exists(grant.Path))
                {
                    Log.Verbose("[SANDBOX] Skipping nonexistent path {Path}", grant.Path);
                    continue;
                }

                var access = grant.Access == AppContainerHelper.AccessModify ? "modify" : "read/execute";
                switch (AppContainerHelper.EnsureAccess(grant.Path, ctx.ContainerSid, grant.Access))
                {
                    case GrantResult.AlreadyGranted:
                        if (verbose)
                            Log.Information("[SANDBOX] Already has {Access}: {Path}", access, grant.Path);
                        break;

                    case GrantResult.Granted:
                        Log.Information("[SANDBOX] Granted {Access} on {Path}", access, grant.Path);
                        break;

                    case GrantResult.AccessDenied:
                        denied.Add(grant);
                        break;
                }
            }

            return denied;
        }

        private static AppContainerLaunchContext? SetupSandbox(DalamudStartInfo startInfo, SandboxConfiguration config, string gamePath)
        {
            if (startInfo.Platform != OSPlatform.Windows)
            {
                Log.Warning("[SANDBOX] AppContainer sandboxing is only supported on Windows, launching without a sandbox");
                return null;
            }

            var layout = BuildSandboxLayout(startInfo, config, gamePath);

            var ctx = AppContainerHelper.CreateContext(
                layout.Config.ContainerName,
                "Dalamud",
                "FFXIV running under Dalamud's AppContainer sandbox",
                layout.Config.Capabilities);

            try
            {
                ctx.TempDirectoryOverride = layout.TempDirectory;
                ctx.RuntimeDirectoryOverride = layout.RuntimeDirectory;
                Log.Information("[SANDBOX] Using AppContainer {Name} ({Sid})", layout.Config.ContainerName, ctx.ContainerSidString);

                var denied = ApplySandboxGrants(layout, ctx, false);
                if (denied.Count > 0)
                {
                    throw new SandboxPreparationRequiredException(
                        denied.Select(x => x.Path).ToList(),
                        layout.Config.ContainerName);
                }

                if (layout.Config.LoopbackExempt)
                    AppContainerHelper.TryAddLoopbackExemption(ctx);

                return ctx;
            }
            catch
            {
                ctx.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Prepares persistent filesystem ACLs and the loopback excemption, if necessary.
        /// </summary>
        private static int ProcessSandboxPrepareCommand(List<string> args, DalamudStartInfo startInfo)
        {
            string? gamePath = null;
            string? sandboxConfigPath = null;
            var showHelp = false;
            var writeConfig = false;

            for (var i = 2; i < args.Count; i++)
            {
                if (args[i] == "-h" || args[i] == "--help")
                    showHelp = true;
                else if (args[i] == "-g")
                    gamePath = args[++i];
                else if (args[i].StartsWith("--game="))
                    gamePath = args[i].Split('=', 2)[1];
                else if (args[i].StartsWith("--sandbox-config="))
                    sandboxConfigPath = args[i].Split('=', 2)[1];
                else if (args[i] == "--write-config")
                    writeConfig = true;
                else
                    Log.Warning($"\"{args[i]}\" is not a valid command line argument, ignoring.");
            }

            if (showHelp)
            {
                ProcessHelpCommand(args, "sandbox-prepare");
                return 0;
            }

            if (startInfo.Platform != OSPlatform.Windows)
            {
                Log.Error("AppContainer sandboxing is only supported on Windows.");
                return -1;
            }

            gamePath ??= ResolveGamePath(startInfo);
            if (gamePath == null)
                return -1;

            var elevated = AppContainerHelper.IsElevated();
            Log.Information("Running {Elevated}elevated.", elevated ? string.Empty : "un");

            var config = SandboxConfiguration.Load(
                sandboxConfigPath ?? SandboxConfiguration.DefaultPath,
                sandboxConfigPath != null);

            if (writeConfig)
            {
                if (config.TryWrite(SandboxConfiguration.DefaultPath))
                {
                    Log.Information("Wrote a sandbox configuration to {Path}", SandboxConfiguration.DefaultPath);
                }
                else
                {
                    Log.Warning(
                        "Not writing a sandbox configuration, {Path} already exists. Delete it first if you want a fresh one.",
                        SandboxConfiguration.DefaultPath);
                }
            }

            var layout = BuildSandboxLayout(startInfo, config, gamePath);

            using var ctx = AppContainerHelper.CreateContext(
                layout.Config.ContainerName,
                "Dalamud",
                "FFXIV running under Dalamud's AppContainer sandbox",
                layout.Config.Capabilities);

            Log.Information(
                "[SANDBOX] AppContainer {Name} ({Sid})",
                layout.Config.ContainerName,
                ctx.ContainerSidString);

            var denied = ApplySandboxGrants(layout, ctx, true);

            if (layout.Config.LoopbackExempt)
                AppContainerHelper.TryAddLoopbackExemption(ctx);

            if (denied.Count > 0)
            {
                Log.Error(
                    "Could not write the DACL on {Count} path(s):{Paths}",
                    denied.Count,
                    string.Concat(denied.Select(x => $"{Environment.NewLine}    {x.Path}")));
                Log.Error(
                    elevated
                        ? "This is unexpected while elevated and probably a bug. Check that the paths are not read-only or on a filesystem without ACL support."
                        : "Re-run this command from an elevated command prompt.");
                return -1;
            }

            Log.Information("Sandbox preparation complete! You can now launch with --sandbox (no elevation needed).");

            if (!config.EnabledGlobally)
            {
                Log.Information(
                    "Launches that don't pass --sandbox will still be unsandboxed. Set \"enabledGlobally\": true in {Path} to sandbox every launch.",
                    SandboxConfiguration.DefaultPath);
            }

            return 0;
        }

        private static bool PathsEqual(string? a, string? b)
        {
            if (a == null || b == null)
                return false;

            return string.Equals(
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(a)),
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(b)),
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAtOrUnder(string? path, string? ancestor)
        {
            if (path == null || ancestor == null)
                return false;

            if (PathsEqual(path, ancestor))
                return true;

            var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
            var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(ancestor)) + Path.DirectorySeparatorChar;
            return full.StartsWith(root, StringComparison.OrdinalIgnoreCase);
        }

        private static void MigrateFileToSandbox(string source, string target)
        {
            try
            {
                if (!File.Exists(source) || File.Exists(target))
                    return;

                File.Copy(source, target);
                Log.Information("[SANDBOX] Migrated {Source} to {Target}", source, target);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[SANDBOX] Could not migrate {Source} to {Target}", source, target);
            }
        }

        private static void MigrateDirectoryToSandbox(string source, string target)
        {
            try
            {
                if (!Directory.Exists(source) || Directory.Exists(target))
                    return;

                static void CopyRecursively(DirectoryInfo from, DirectoryInfo to)
                {
                    foreach (var dir in from.GetDirectories())
                        CopyRecursively(dir, to.CreateSubdirectory(dir.Name));

                    foreach (var file in from.GetFiles())
                        file.CopyTo(Path.Combine(to.FullName, file.Name));
                }

                CopyRecursively(new DirectoryInfo(source), Directory.CreateDirectory(target));
                Log.Information("[SANDBOX] Migrated {Source} to {Target}", source, target);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[SANDBOX] Could not migrate {Source} to {Target}", source, target);
            }
        }

        /// <summary>
        /// Determine the game path from the platform's launcher configuration.
        /// Logs and returns null when it could not be determined.
        /// </summary>
        private static string? ResolveGamePath(DalamudStartInfo dalamudStartInfo)
        {
            string? gamePath;
            try
            {
                if (dalamudStartInfo.Platform == OSPlatform.Windows)
                {
                    gamePath = FindGamePathFromLauncherConfig();
                    Log.Information("Using game installation path configuration from from XIVLauncher: {0}", gamePath);
                }
                else if (dalamudStartInfo.Platform == OSPlatform.Linux)
                {
                    var homeDir = $"Z:\\home\\{Environment.UserName}";
                    var xivlauncherDir = Path.Combine(homeDir, ".xlcore");
                    var launcherConfigPath = Path.Combine(xivlauncherDir, "launcher.ini");
                    var config = File.ReadAllLines(launcherConfigPath)
                        .Where(line => line.Contains('='))
                        .ToDictionary(line => line.Split('=')[0], line => line.Split('=')[1]);
                    gamePath = Path.Combine("Z:" + config["GamePath"].Replace('/', '\\'), "game", "ffxiv_dx11.exe");
                    Log.Information("Using game installation path configuration from from XIVLauncher.Core: {0}", gamePath);
                }
                else
                {
                    var homeDir = $"Z:\\Users\\{Environment.UserName}";
                    var xomlauncherDir = Path.Combine(homeDir, "Library", "Application Support", "XIV on Mac");
                    // we could try to parse the binary plist file here if we really wanted to...
                    gamePath = Path.Combine(xomlauncherDir, "ffxiv", "game", "ffxiv_dx11.exe");
                    Log.Information("Using default game installation path from XOM: {0}", gamePath);
                }
            }
            catch (Exception)
            {
                Log.Error("Failed to read launcher config to get the set-up game path, please specify one using -g");
                return null;
            }

            if (gamePath == null)
            {
                Log.Error("Game path not specified and could not be determined from launcher config, please specify one using -g");
                return null;
            }

            if (!File.Exists(gamePath))
            {
                Log.Error("File not found: {0}", gamePath);
                return null;
            }

            return gamePath;
        }

        private static string? FindGamePathFromLauncherConfig()
        {
            var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var xivlauncherDir = Path.Combine(appDataDir, "XIVLauncher");
            var launcherConfigPath = Path.Combine(xivlauncherDir, "launcherConfigV3.json");

            if (!File.Exists(launcherConfigPath))
                return null;

            var deserializedConfig = JsonSerializer.CreateDefault()
                                                   .Deserialize<Dictionary<string, string>>(
                                                       new JsonTextReader(
                                                           new StringReader(File.ReadAllText(launcherConfigPath))));

            if (deserializedConfig == null)
                return null;

            return Path.Combine(
                deserializedConfig["GamePath"],
                "game",
                "ffxiv_dx11.exe");
        }

        private static unsafe Process GetInheritableCurrentProcessHandle()
        {
            var currentProcessHandle = new HANDLE(Process.GetCurrentProcess().Handle.ToPointer());
            var inheritableHandle = HANDLE.Null;
            if (!Windows.Win32.PInvoke.DuplicateHandle(
                    currentProcessHandle,
                    currentProcessHandle,
                    currentProcessHandle,
                    &inheritableHandle,
                    0,
                    true,
                    DUPLICATE_HANDLE_OPTIONS.DUPLICATE_SAME_ACCESS))
            {
                throw new Win32Exception("Failed to call DuplicateHandle");
            }

            return new ExistingProcess(inheritableHandle);
        }

        private static int ProcessLaunchTestCommand(List<string> args)
        {
            Console.WriteLine("Testing launch command.");
            args[0] = Process.GetCurrentProcess().MainModule!.FileName;
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
            {
                return -1;
            }

            var result = JsonSerializer.CreateDefault().Deserialize<Dictionary<string, int>>(new JsonTextReader(helperProcess.StandardOutput));
            if (result == null)
                throw new Exception("Could not get result from game process");

            var pid = result["pid"];
            var handle = (IntPtr)result["handle"];
            var resultProcess = new ExistingProcess(handle);
            Console.WriteLine("PID: {0}, Handle: {1}", pid, handle);
            Console.WriteLine("Press Enter to force quit");
            Console.ReadLine();
            resultProcess.Kill();
            return 0;
        }

        private static DalamudStartInfo AdjustStartInfo(DalamudStartInfo startInfo, string gamePath)
        {
            var ffxivDir = Path.GetDirectoryName(gamePath) ?? throw new DirectoryNotFoundException($"Could not determine parent directory of {gamePath}.");
            var gameVerStr = File.ReadAllText(Path.Combine(ffxivDir, "ffxivgame.ver"));
            var gameVer = GameVersion.Parse(gameVerStr);

            return startInfo with
            {
                GameVersion = gameVer,
            };
        }

        private static void Inject(Process process, DalamudStartInfo startInfo, bool tryFixAcl = false)
        {
            if (tryFixAcl)
            {
                try
                {
                    GameStart.CopyAclFromSelfToTargetProcess(process.SafeHandle.DangerousGetHandle());
                }
                catch (Win32Exception e1)
                {
                    Log.Warning(e1, "Failed to copy ACL");
                }
            }

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

            if (startInfoAddress == 0)
            {
                throw new Exception("Unable to allocate start info JSON");
            }

            injector.GetFunctionAddress(bootModule, "Initialize", out var initAddress);
            var exitCode = injector.CallRemoteFunction(initAddress, startInfoAddress);

            // ======================================================

            if (exitCode > 0)
            {
                Log.Error("Dalamud.Boot::Initialize returned {ExitCode}", exitCode);
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
            if (argument == null)
            {
                throw new ArgumentNullException(nameof(argument));
            }

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

        private class CommandLineException : Exception
        {
            public CommandLineException(string cause)
                : base(cause)
            {
            }
        }

        private sealed record SandboxGrant(string Path, uint Access, bool Create);

        private sealed record SandboxLayout(SandboxConfiguration Config, string TempDirectory, string RuntimeDirectory, List<SandboxGrant> Grants);

        private sealed class SandboxPreparationRequiredException(List<string> paths, string containerName)
            : Exception(BuildMessage(paths, containerName))
        {
            private static string BuildMessage(List<string> paths, string containerName)
            {
                var exeName = Path.GetFileNameWithoutExtension(Environment.ProcessPath) ?? "Dalamud.Injector";
                var sb = new StringBuilder();
                sb.AppendLine($"The sandbox ({containerName}) is missing filesystem permissions and they could not be granted:");
                foreach (var path in paths)
                    sb.AppendLine($" => {path}");
                sb.AppendLine();
                sb.AppendLine("Run this once from an elevated command prompt, then launch normally:");
                sb.Append($"    {exeName}.exe sandbox-prepare");
                return sb.ToString();
            }
        }
    }
}
