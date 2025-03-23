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
        /// <returns>Return value (HRESULT).</returns>
        public delegate int MainDelegate(int argc, IntPtr argvPtr);

        /// <summary>
        /// Start the Dalamud injector.
        /// </summary>
        /// <param name="argc">Count of arguments.</param>
        /// <param name="argvPtr">byte** string arguments.</param>
        /// <returns>Return value (HRESULT).</returns>
        public static int Main(int argc, IntPtr argvPtr)
        {
            try
            {
                List<string> args = new(argc);

                unsafe
                {
                    var argv = (IntPtr*)argvPtr;
                    for (var i = 0; i < argc; i++)
                        args.Add(Marshal.PtrToStringUni(argv[i]));
                }

                Init(args);
                args.Remove("-v"); // Remove "verbose" flag

                if (args.Count >= 2 && args[1].ToLowerInvariant() == "launch-test")
                {
                    return ProcessLaunchTestCommand(args);
                }

                DalamudStartInfo startInfo = null;
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
                if (mainCommand.Length > 0 && mainCommand.Length <= 6 && "inject"[..mainCommand.Length] == mainCommand)
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

            var cwd = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
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

        private static DalamudStartInfo ExtractAndInitializeStartInfoFromArguments(DalamudStartInfo? startInfo, List<string> args)
        {
            int len;
            string key;

            startInfo ??= new DalamudStartInfo();

            var workingDirectory = startInfo.WorkingDirectory;
            var configurationPath = startInfo.ConfigurationPath;
            var pluginDirectory = startInfo.PluginDirectory;
            var assetDirectory = startInfo.AssetDirectory;
            var delayInitializeMs = startInfo.DelayInitializeMs;
            var logName = startInfo.LogName;
            var logPath = startInfo.LogPath;
            var languageStr = startInfo.Language.ToString().ToLowerInvariant();
            var unhandledExceptionStr = startInfo.UnhandledException.ToString().ToLowerInvariant();
            var troubleshootingData = "{\"empty\": true, \"description\": \"No troubleshooting data supplied.\"}";

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
                else if (args[i].StartsWith(key = "--dalamud-delay-initialize="))
                {
                    delayInitializeMs = int.Parse(args[i][key.Length..]);
                }
                else if (args[i].StartsWith(key = "--dalamud-client-language="))
                {
                    languageStr = args[i][key.Length..].ToLowerInvariant();
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

            startInfo.WorkingDirectory = workingDirectory;
            startInfo.ConfigurationPath = configurationPath;
            startInfo.PluginDirectory = pluginDirectory;
            startInfo.AssetDirectory = assetDirectory;
            startInfo.Language = clientLanguage;
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
            startInfo.BootShowConsole = args.Contains("--console");
            startInfo.BootEnableEtw = args.Contains("--etw");
            startInfo.BootDisableLegacyCorruptedStateExceptions = args.Contains("--no-legacy-corrupted-state-exceptions");
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
                Console.WriteLine("{0}        [-- game_arg1=value1 game_arg2=value2 ...]", exeSpaces);
            }

            Console.WriteLine("Specifying dalamud start info: [--dalamud-working-directory=path] [--dalamud-configuration-path=path]");
            Console.WriteLine("                               [--dalamud-plugin-directory=path]");
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
                var result = MessageBoxW(IntPtr.Zero, $"Take care: you are manually injecting Dalamud into FFXIV({string.Join(", ", processes.Select(x => $"{x.Id}"))}).\n\nIf you are doing this to use plugins before they are officially whitelisted on patch days, things may go wrong and you may get into trouble.\nWe discourage you from doing this and you won't be warned again in-game.", "Dalamud", MessageBoxType.IconWarning | MessageBoxType.OkCancel);

                // IDCANCEL
                if (result == 2)
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
                Inject(process, AdjustStartInfo(dalamudStartInfo, process.MainModule.FileName), tryFixAcl);

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
                    Log.Error("Failed to read launcherConfigV3.json to get the set-up game path, please specify one using -g");
                    return -1;
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

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    [System.Runtime.InteropServices.DllImport("c")]
#pragma warning disable SA1300
                    static extern ulong clock_gettime_nsec_np(int clockId);
#pragma warning restore SA1300

                    const int CLOCK_MONOTONIC_RAW = 4;
                    var rawTickCountFixed = clock_gettime_nsec_np(CLOCK_MONOTONIC_RAW) / 1000000;
                    Log.Information("ArgumentBuilder::DeriveKey() fixing up rawTickCount from {0} to {1} on macOS", rawTickCount, rawTickCountFixed);
                    rawTickCount = (uint)rawTickCountFixed;
                }

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

            var process = GameStart.LaunchGame(
                Path.GetDirectoryName(gamePath),
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
                waitForGameWindow);

            Log.Verbose("Game process started with PID {0}", process.Id);

            if (!withoutDalamud && dalamudStartInfo.LoadMethod == LoadMethod.DllInject)
            {
                var startInfo = AdjustStartInfo(dalamudStartInfo, gamePath);
                Log.Information("Using start info: {0}", JsonConvert.SerializeObject(startInfo));
                Inject(process, startInfo, false);
            }

            var processHandleForOwner = IntPtr.Zero;
            if (handleOwner != IntPtr.Zero)
            {
                if (!DuplicateHandle(Process.GetCurrentProcess().Handle, process.Handle, handleOwner, out processHandleForOwner, 0, false, DuplicateOptions.SameAccess))
                {
                    Log.Warning("Failed to call DuplicateHandle: Win32 error code {0}", Marshal.GetLastWin32Error());
                }
            }

            Console.WriteLine($"{{\"pid\": {process.Id}, \"handle\": {processHandleForOwner}}}");

            Log.CloseAndFlush();
            return 0;
        }

        private static Process GetInheritableCurrentProcessHandle()
        {
            if (!DuplicateHandle(Process.GetCurrentProcess().Handle, Process.GetCurrentProcess().Handle, Process.GetCurrentProcess().Handle, out var inheritableCurrentProcessHandle, 0, true, DuplicateOptions.SameAccess))
            {
                Log.Error("Failed to call DuplicateHandle: Win32 error code {0}", Marshal.GetLastWin32Error());
                return null;
            }

            return new ExistingProcess(inheritableCurrentProcessHandle);
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
            {
                return -1;
            }

            var result = JsonSerializer.CreateDefault().Deserialize<Dictionary<string, int>>(new JsonTextReader(helperProcess.StandardOutput));
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
            var ffxivDir = Path.GetDirectoryName(gamePath);
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
    }
}
