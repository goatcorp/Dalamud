using Dalamud.Bootstrap.OS;
using Dalamud.Bootstrap.OS.Windows;
using Dalamud.Bootstrap.OS.Windows.Raw;
using Microsoft.Win32.SafeHandles;
using System;

namespace Dalamud.Bootstrap
{
    public sealed partial class GameProcess : IDisposable
    {
        private Process m_process;

        // maybe saved acl shit

        private GameProcess(Process process)
        {
            m_process = process;
        }

        public void Dispose()
        {
            m_process?.Dispose();
            m_process = null!;
        }

        // /// <summary>
        // /// 
        // /// </summary>
        // /// <param name="handle">A process handle.</param>
        // private static void AllowHandleAccess(Process handle)
        // {

        // }

        // private static void DenyHandleAccess(SafeProcessHandle handle)
        // {

        // }
    }
}
