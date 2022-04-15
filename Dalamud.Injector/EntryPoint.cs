using System;
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
            InitUnhandledException();
            InitLogging();

            var args = new string[argc];

            unsafe
            {
                var argv = (IntPtr*)argvPtr;
                for (var i = 0; i < argc; i++)
                {
                    args[i] = Marshal.PtrToStringUni(argv[i]);
                }
            }

            var cwd = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
            if (cwd.FullName != Directory.GetCurrentDirectory())
            {
                Log.Debug($"Changing cwd to {cwd}");
                Directory.SetCurrentDirectory(cwd.FullName);
            }

            var process = GetProcess(args.ElementAtOrDefault(1));
            if (process == null)
            {
                Log.Error("Could not open process");
                return;
            }

            var startInfo = GetStartInfo(args.ElementAtOrDefault(2), process);

            // TODO: XL does not set this!!! we need to keep this line here for now, otherwise we crash in the Dalamud entrypoint
            startInfo.WorkingDirectory = Directory.GetCurrentDirectory();

            // This seems to help with the STATUS_INTERNAL_ERROR condition
            Thread.Sleep(1000);

            Inject(process, startInfo);

            Thread.Sleep(1000);
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

        private static Process GetProcessFromExecuting(string gamePath)
        {
            var gameVersion = File.ReadAllText(Path.Combine(Directory.GetParent(gamePath).FullName, "ffxivgame.ver"));
            var sqpackPath = Path.Combine(Directory.GetParent(gamePath).FullName, "sqpack");
            var maxEntitledExpansionId = 0;
            while (File.Exists(Path.Combine(sqpackPath, $"ex{maxEntitledExpansionId + 1}", $"ex{maxEntitledExpansionId + 1}.ver")))
                maxEntitledExpansionId++;

            var process = Process.Start(gamePath, new string[]
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

            Thread.Sleep(1000);

            return process;
        }

        private static Process? GetProcess(string? arg)
        {
            Process process = null;

            var pid = -1;
            if (arg != default)
            {
                try
                {
                    pid = int.Parse(arg);
                }
                catch (FormatException)
                {
                    if (File.Exists(arg))
                        return GetProcessFromExecuting(arg);
                }
            }

            switch (pid)
            {
                case -1:
                    process = Process.GetProcessesByName("ffxiv_dx11").FirstOrDefault();

                    if (process == default)
                    {
                        throw new Exception("Could not find process");
                    }

                    #if !DEBUG
                    var result = MessageBoxW(IntPtr.Zero, $"Take care: you are manually injecting Dalamud into FFXIV({process.Id}).\n\nIf you are doing this to use plugins before they are officially whitelisted on patch days, things may go wrong and you may get into trouble.\nWe discourage you from doing this and you won't be warned again in-game.", "Dalamud", MessageBoxType.IconWarning | MessageBoxType.OkCancel);

                    // IDCANCEL
                    if (result == 2)
                    {
                        Log.Information("User cancelled injection");
                        return null;
                    }
                    #endif

                    break;

                case -2:
                    return GetProcessFromExecuting("C:\\Program Files (x86)\\SquareEnix\\FINAL FANTASY XIV - A Realm Reborn\\game\\ffxiv_dx11.exe");

                default:
                    try
                    {
                        process = Process.GetProcessById(pid);
                    }
                    catch (ArgumentException)
                    {
                        Log.Error("Could not find process with PID: {Pid}", pid);
                    }

                    break;
            }

            return process;
        }

        private static DalamudStartInfo GetStartInfo(string? arg, Process process)
        {
            DalamudStartInfo startInfo;

            if (arg != default)
            {
                startInfo = JsonConvert.DeserializeObject<DalamudStartInfo>(Encoding.UTF8.GetString(Convert.FromBase64String(arg)));
            }
            else
            {
                var ffxivDir = Path.GetDirectoryName(process.MainModule.FileName);
                var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var xivlauncherDir = Path.Combine(appDataDir, "XIVLauncher");

                var gameVerStr = File.ReadAllText(Path.Combine(ffxivDir, "ffxivgame.ver"));
                var gameVer = GameVersion.Parse(gameVerStr);

                startInfo = new DalamudStartInfo
                {
                    WorkingDirectory = null,
                    ConfigurationPath = Path.Combine(xivlauncherDir, "dalamudConfig.json"),
                    PluginDirectory = Path.Combine(xivlauncherDir, "installedPlugins"),
                    DefaultPluginDirectory = Path.Combine(xivlauncherDir, "devPlugins"),
                    AssetDirectory = Path.Combine(xivlauncherDir, "dalamudAssets", "dev"),
                    GameVersion = gameVer,
                    Language = ClientLanguage.English,
                };

                Log.Debug(
                    "Creating a new StartInfo with:\n" +
                    $"    WorkingDirectory: {startInfo.WorkingDirectory}\n" +
                    $"    ConfigurationPath: {startInfo.ConfigurationPath}\n" +
                    $"    PluginDirectory: {startInfo.PluginDirectory}\n" +
                    $"    DefaultPluginDirectory: {startInfo.DefaultPluginDirectory}\n" +
                    $"    AssetDirectory: {startInfo.AssetDirectory}\n" +
                    $"    GameVersion: {startInfo.GameVersion}\n" +
                    $"    Language: {startInfo.Language}\n");

                Log.Information("A Dalamud start info was not found in the program arguments. One has been generated for you.");
                Log.Information("Copy the following contents into the program arguments:");

                var startInfoJson = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(startInfo)));
                Log.Information(startInfoJson);
            }

            return startInfo;
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
    }
}
