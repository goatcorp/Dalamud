using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using CoreHook.BinaryInjection.RemoteInjection;
using CoreHook.BinaryInjection.RemoteInjection.Configuration;
using CoreHook.IPC.Platform;

namespace Dalamud.Injector
{
    public sealed class DalamudLauncher
    {
        private readonly DalamudLauncherOptions m_options;

        public DalamudLauncher(DalamudLauncherOptions options)
        {
            m_options = options;
        }

        public void Launch(string exePath)
        {
            //
        }

        public void Relaunch(uint pid)
        {
            // TODO
            // 1. Open process `pid` with handle
            // 2. Read command arguments (requires reading PEB)
            // 3. Construct new arguments 
            //   3.1 Decrypt arguments acquired from step.2 (possible key space is very small so it's feasible to do this)
            //   3.2 Manipulate arguments as needed
            //   3.3 Re-encrypt arguments with new timestamp
            // 4 Launch a new process with new argument which was computed from step.3
            //   4.1 Create process with CREATE_SUSPENDED
            //   4.2 Figure out entry-point of ffxiv_dx11.exe
            //   4.3 Insert a hook on entry-point to wait for user-mode process initialization to be finished, but not the code from ffxiv_dx11.exe
            //     - This can be implemented in a such way that constantly checking program counter from `GetThreadContext` returns a value we expect.
            //       Before you might ask: Yes, this is not the cleanest method I could come up with per se, but hey it gives far less headache to actually implement!
            // 5 Attempt to inject into that process.
            // 6. If all succeeded, terminate the old process.
            //
            // delegate Step 3 to 5 to Launch() maybe?
        }

        /// <summary>
        /// Injects Dalamud into the process. See remarks for process state prerequisites.
        /// </summary>
        /// <remarks>
        /// TODO: CREATE_SUSPENDED -> entrypoint explainations
        /// </remarks>
        /// <param name="pid">A process id to inject Dalamud into.</param>
        public void Inject(uint pid)
        {
            // Please keep in mind that this config values (especially ClrRootPath) assumes that
            // Dalamud is compiled as self-contained to avoid requiring people to pre-install specific .NET Core version.
            // https://docs.microsoft.com/en-us/dotnet/core/deploying/
            var corehookConfig = new RemoteInjectorConfiguration
            {
                InjectionPipeName = $"Dalamud-{pid}-CoreHook",
                ClrRootPath = m_options.BinaryDirectory, // `dotnet.runtimeconfig.json` is not needed for self-contained app.
                ClrBootstrapLibrary = Path.Combine(m_options.BinaryDirectory, "CoreHook.CoreLoad.dll"),
                DetourLibrary = Path.Combine(m_options.BinaryDirectory, "corehook64.dll"),
                HostLibrary = Path.Combine(m_options.BinaryDirectory, "coreload64.dll"),
                PayloadLibrary = Path.Combine(m_options.BinaryDirectory, "Dalamud.dll"),
                VerboseLog = false,
            };

            try
            {
                RemoteInjector.Inject((int)pid, corehookConfig, new PipePlatform(), m_options.RootDirectory);
            }
            catch (Exception ex)
            {
                const string message = "Failed to inject Dalamud library into the process.";

                // Could not inject Dalamud for whatever reason; it could be process is not actually running, insufficient os privilege, or whatever the thing SE put in their game;
                // Therefore there's not much we can do on this side; You have to trobleshoot by yourself somehow.
                throw new DalamudLauncherException(pid, message, ex);
            }
        }
    }

    internal sealed class PipePlatform : IPipePlatform
    {
        public NamedPipeServerStream CreatePipeByName(string pipeName, string serverName = ".")
        {
            return new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 0x10000, 0x10000);
        }
    }
}
