using System;
using System.Collections.Generic;
using System.Text;

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

        public void Inject(uint pid)
        {

        }
    }
}
