using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;
using CoreHook.BinaryInjection;
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
            //
        }

        public void Inject(uint pid)
        {

            var corehookConfig = new RemoteInjectorConfiguration
            {
                ClrBootstrapLibrary = "",
                ClrRootPath = "",
                DetourLibrary = "",
                HostLibrary = "",
                InjectionPipeName = "",
                PayloadLibrary = "",
                VerboseLog = false,
            };

            RemoteInjector.Inject((int)pid, corehookConfig, new PipePlatform(), m_options.RootDirectory);
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
