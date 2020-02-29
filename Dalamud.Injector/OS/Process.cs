using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dalamud.Injector.OS
{
    internal sealed partial class Process : IDisposable
    {
        private SafeProcessHandle m_handle;
    }

    internal sealed partial class Process {
        internal Process(SafeProcessHandle handle)
        {
            m_handle = handle;
        }
        
        public static Process Open(uint pid/* and perms? maybe? */)
        {
            //
            throw new NotImplementedException("TODO");
        }

        

        public void Dispose()
        {
            m_handle?.Dispose();
            m_handle = null!;
        }
    }
}
