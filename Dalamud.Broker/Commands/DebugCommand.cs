using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;
using Dalamud.Broker.Game;
using Dalamud.Broker.Win32;

namespace Dalamud.Broker.Commands;

internal sealed class DebugCommand
{
    public static async Task Run(DebugCommandOptions options)
    {
        var (process, thread) = ProcessLauncher.Start(@"C:\Windows\System32\notepad.exe");

        var ctx = new CancellationTokenSource();
        var waiter = new ProcessWaiter(process);
        await waiter.WaitAsync(ctx.Token);
    }
    //     var procContext = new ProcessLaunchContext
    //     {
    //         ApplicationPath = options.ExecutablePath,
    //         Arguments = new []
    //         {
    //             "DEV.TestSID=AAAAAAAA"
    //         },
    //         CreationFlags = PROCESS_CREATION_FLAGS.CREATE_SUSPENDED,
    //     };
    //     
    //     var processHandle = ProcessLauncher.Start(procContext);
    //     
    //
    //     var ok = PInvoke.DuplicateHandle(Process.GetCurrentProcess().SafeHandle, processHandle.Process, processHandle.Process,
    //                             out var dupHandle, 0, false, DUPLICATE_HANDLE_OPTIONS.DUPLICATE_SAME_ACCESS);
    //     if (!ok)
    //     {
    //         throw new Win32Exception();
    //     }
    //     
    //     var startInfo = CreateStartInfo(dupHandle);
    //     
    //     var errc = RewriteRemoteEntryPointW(processHandle.Process,  @"D:\Games\FINAL FANTASY XIV\game\ffxiv_dx11.exe", startInfo);
    //     if (errc != 0)
    //     {
    //         throw new Exception("..wtf?");
    //     }
    //
    //     // var sid = new NTAccount(Environment.UserName);
    //     // var rule = new AccessRule<Shit>(sid, Shit.PROCESS_VM_WRITE, AccessControlType.Allow);
    //     // var access = new AclShit(processHandle.Process);
    //     // access.RemoveAccessRule(rule);
    //     // access.Persist();
    //     //
    //     // TODO: rewrite entry point thing?
    //     var result = PInvoke.ResumeThread(processHandle.Thread);
    //
    //     // wait for game window?
    //     // PInvoke.OpenProcess(())
    //     //.. exi?
    // }
    //
    // private static string CreateStartInfo(SafeHandle handle)
    // {
    //     var info = new DalamudStartInfo
    //     {
    //        WorkingDirectory = Directory.GetCurrentDirectory(),
    //        ConfigurationPath = @"C:\Users\Workbench\AppData\Roaming\XIVLauncher\dalamudConfig.json",
    //        PluginDirectory = @"C:\Users\Workbench\AppData\Roaming\XIVLauncher\installedPlugins",
    //        DefaultPluginDirectory = @"C:\Users\Workbench\AppData\Roaming\XIVLauncher\devPlugins",
    //        AssetDirectory = @"C:\Users\Workbench\AppData\Roaming\XIVLauncher\dalamudAssets\dev",
    //        BootShowConsole = true,
    //        CrashHandlerShow = true,
    //        BootLogPath = @"D:\Projects\FFXIV\minoost\Dalamud.Experiment.Ldm\bin\Debug\dalamud_injector.log",
    //        BootDotnetOpenProcessHookMode = 0,
    //        BootWaitMessageBox = 1 | 2 | 4,
    //        BootVehEnabled = true,
    //        NoLoadPlugins = false,
    //        NoLoadThirdPartyPlugins = false,
    //        Language = ClientLanguage.English,
    //        
    //     };
    //
    //     var infoUtf8 = JsonSerializer.SerializeToUtf8Bytes(info);
    //
    //     return Encoding.UTF8.GetString(infoUtf8);
    // }
}
