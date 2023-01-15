using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Windows.Win32;
using Windows.Win32.System.Threading;
using Dalamud.Broker.Game;
using Dalamud.Broker.Win32;

namespace Dalamud.Broker.Commands;

internal sealed class DebugCommand
{
    
    [DllImport("Dalamud.Boot.dll")]
    private static extern int RewriteRemoteEntryPointW(SafeHandle hProcess, [MarshalAs(UnmanagedType.LPWStr)] string gamePath, [MarshalAs(UnmanagedType.LPWStr)] string loadInfoJson);

    public static void Run(DebugCommandOptions options)
    {
        var procContext = new ProcessLaunchContext
        {
            ApplicationPath = @"D:\Games\FINAL FANTASY XIV\game\ffxiv_dx11.exe",
            Arguments = new []
            {
                "DEV.TestSID=AAAAAAAA"
            },
            CreationFlags = PROCESS_CREATION_FLAGS.CREATE_SUSPENDED,
        };
        
        var processHandle = ProcessLauncher.Start(procContext);
        var startInfo = CreateStartInfo();
        
        var errc = RewriteRemoteEntryPointW(processHandle.Process,  @"D:\Games\FINAL FANTASY XIV\game\ffxiv_dx11.exe", startInfo);
        if (errc != 0)
        {
            throw new Exception("..wtf?");
        }
        
        // TODO: rewrite entry point thing?
        PInvoke.ResumeThread(processHandle.Thread);

        // wait for game window?
        
        //.. exi?
    }

    private static string CreateStartInfo()
    {
        var info = new DalamudStartInfo
        {
           
        };

        var infoUtf8 = JsonSerializer.SerializeToUtf8Bytes(info);

        return Encoding.UTF8.GetString(infoUtf8);
    }
}
