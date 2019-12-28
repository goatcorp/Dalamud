using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Dalamud.DiscordBot;
using Dalamud.Game.Chat;
using EasyHook;
using Newtonsoft.Json;

namespace Dalamud.Injector {
    internal static class Program {
        private static void Main(string[] args) {
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs eventArgs)
            {
                File.WriteAllText("InjectorException.txt", eventArgs.ExceptionObject.ToString());

                MessageBox.Show("Failed to inject the XIVLauncher in-game addon. Please report this error:\n\n" + eventArgs.ExceptionObject, "XIVLauncher Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                Environment.Exit(0);
            };

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
                    break;
                default:
                    process = Process.GetProcessById(pid);
                    break;
            }

            var startInfo = JsonConvert.DeserializeObject<DalamudStartInfo>(Encoding.UTF8.GetString(Convert.FromBase64String(args[1])));
            startInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            
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
    }
}
