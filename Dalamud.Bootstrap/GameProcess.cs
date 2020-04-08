using Dalamud.Bootstrap.OS;
using Dalamud.Bootstrap.OS.Windows.Raw;
using Microsoft.Win32.SafeHandles;
using System;

namespace Dalamud.Bootstrap
{
    public sealed partial class GameProcess : IDisposable
    {
        private SafeProcessHandle m_handle;

        private GameProcess(SafeProcessHandle handle)
        {
            m_handle = handle;
        }

        public static GameProcess Create(GameProcessCreationOptions options)
        {

        }

        public static GameProcess Open(uint pid)
        {
            
            var handle = OpenHandle(pid, TODO);

            return new GameProcess(handle);
        }

        private static SafeProcessHandle OpenHandle(uint pid, PROCESS_ACCESS_RIGHTS access)
        {
            var handle = Kernel32.OpenProcess((uint)access, false, pid);

            if (handle.IsInvalid)
            {
                ProcessException.ThrowLastOsError();
            }

            return handle;
        }

        public void Dispose()
        {
            m_handle?.Dispose();
            m_handle = null!;
        }
    }
}
