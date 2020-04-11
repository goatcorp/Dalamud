using System;
using Dalamud.Bootstrap.OS.Windows.Raw;
using Microsoft.Win32.SafeHandles;

namespace Dalamud.Bootstrap.Windows
{
    internal sealed class RelaxedProcessHandle : IDisposable
    {
        private SafeProcessHandle m_handle;

        private RelaxedProcessHandle(SafeProcessHandle handle)
        {
            m_handle = handle;
        }

        public void Dispose()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        /// <remarks>
        /// 
        /// </remarks>
        public static RelaxedProcessHandle Create(SafeProcessHandle handle, PROCESS_ACCESS_RIGHTS access)
        {
            

            return new RelaxedProcessHandle(handle);
        }
    }
}
