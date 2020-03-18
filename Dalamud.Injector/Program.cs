using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Dalamud.DiscordBot;
using Dalamud.Game.Chat;
using EasyHook;
using Newtonsoft.Json;

namespace Dalamud.Injector {
    internal static class Program {
        private static void Main(string[] args) {
#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs eventArgs)
            {
                File.WriteAllText("InjectorException.txt", eventArgs.ExceptionObject.ToString());

                MessageBox.Show("Failed to inject the XIVLauncher in-game addon.\nPlease try restarting your game and your PC.\nIf this keeps happening, please report this error.", "XIVLauncher Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                Environment.Exit(0);
            };
#endif

            var pid = int.Parse(args[0]);

            Process process = null;

            switch (pid) {
                case -1:
                    process = Process.GetProcessesByName("ffxiv_dx11")[0];
                    break;
                case -2:
                    process = Process.Start(
                        "C:\\Program Files (x86)\\SquareEnix\\FINAL FANTASY XIV - A Realm Reborn\\game\\ffxiv_dx11.exe",
                        "DEV.TestSID=0 DEV.UseSqPack=1 DEV.DataPathType=1 DEV.LobbyHost01=127.0.0.1 DEV.LobbyPort01=54994 DEV.LobbyHost02=127.0.0.1 DEV.LobbyPort02=54994 DEV.LobbyHost03=127.0.0.1 DEV.LobbyPort03=54994 DEV.LobbyHost04=127.0.0.1 DEV.LobbyPort04=54994 DEV.LobbyHost05=127.0.0.1 DEV.LobbyPort05=54994 DEV.LobbyHost06=127.0.0.1 DEV.LobbyPort06=54994 DEV.LobbyHost07=127.0.0.1 DEV.LobbyPort07=54994 DEV.LobbyHost08=127.0.0.1 DEV.LobbyPort08=54994 SYS.Region=0 language=1 version=1.0.0.0 DEV.MaxEntitledExpansionID=2 DEV.GMServerHost=127.0.0.1 DEV.GameQuitMessageBox=0");
                    Thread.Sleep(1000);
                    break;
                default:
                    process = Process.GetProcessById(pid);
                    break;
            }

            DalamudStartInfo startInfo;
            if (args.Length == 1) {
                startInfo = GetDefaultStartInfo();
                Console.WriteLine("\nA Dalamud start info was not found in the program arguments. One has been generated for you.");
                Console.WriteLine("\nCopy the following contents into the program arguments:");
            } else {
                startInfo = JsonConvert.DeserializeObject<DalamudStartInfo>(Encoding.UTF8.GetString(Convert.FromBase64String(args[1])));
            }

            startInfo.WorkingDirectory = Directory.GetCurrentDirectory();

            // Seems to help with the STATUS_INTERNAL_ERROR condition
            Thread.Sleep(1000);

            // Inject to process
            Inject(process, startInfo);
        }

        private static void Inject(Process process, DalamudStartInfo info) {
            Console.WriteLine($"Injecting to {process.Id}");

            // File check
            var libPath = Path.GetFullPath("Dalamud.dll");
            if (!File.Exists(libPath)) {
                Console.WriteLine($"Can't find a dll on {libPath}");
                return;
            }

            RemoteHooking.Inject(process.Id, InjectionOptions.DoNotRequireStrongName, libPath, libPath, info);

            Console.WriteLine("Injected");
        }

        private static DalamudStartInfo GetDefaultStartInfo() {
            var startInfo = new DalamudStartInfo {
                WorkingDirectory = null,
                ConfigurationPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                                    @"\XIVLauncher\dalamudConfig.json",
                PluginDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                                  @"\XIVLauncher\installedPlugins",
                DefaultPluginDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                                         @"\XIVLauncher\devPlugins",

                GameVersion = File.ReadAllText(@"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\ffxivgame.ver"),
                Language = ClientLanguage.English
            };

            Console.WriteLine("Creating a StartInfo with:\n" +
                              $"ConfigurationPath: {startInfo.ConfigurationPath}\n" +
                              $"PluginDirectory: {startInfo.PluginDirectory}\n" +
                              $"DefaultPluginDirectory: {startInfo.DefaultPluginDirectory}\n" +
                              $"Language: {startInfo.Language}\n" +
                              $"GameVersion: {startInfo.GameVersion}");

            return startInfo;
        }
    }
}
