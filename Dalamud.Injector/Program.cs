using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
                    
                Process.GetCurrentProcess().Kill();
            };

            int pid = int.Parse(args[0]);

            Process process = null;
            process = pid == -1 ? Process.GetProcessesByName("ffxiv_dx11")[0] : Process.GetProcessById(pid);

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
