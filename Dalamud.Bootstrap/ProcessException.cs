using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Dalamud.Bootstrap
{
    public class ProcessException : BootstrapException
    {
        internal ProcessException() : base() { }

        internal ProcessException(string message) : base(message) { }

        internal ProcessException(string message, Exception innerException) : base(message, innerException) { }

        internal static void ThrowLastOsError()
        {
            var inner = new Win32Exception();

            throw new ProcessException(inner.Message, inner);
        }
    }
}
