using System;
using System.Runtime.InteropServices;
using System.Text;
using Serilog;

namespace Dalamud.Game.Internal.Libc {
    /// <summary>
    /// Interation with std::string
    /// </summary>
    public static class StdString {
        public static string ReadFromPointer(IntPtr cstring, ByteWrapper bytes = null) {
            unsafe {
                if (cstring == IntPtr.Zero) {
                    throw new ArgumentNullException(nameof(cstring));
                }
                
                var innerAddress = Marshal.ReadIntPtr(cstring);
                if (innerAddress == IntPtr.Zero) {
                    throw new NullReferenceException("Inner reference to the cstring is null.");
                }
                
                var pInner = (sbyte*) innerAddress.ToPointer();
                var count = 0;
                
                // Count the number of chars. String is assumed to be zero-terminated.
                while (*(pInner + count) != 0) {
                    count += 1;
                }

                // raw copy if requested, as the string conversion returned from this function is lossy
                if (bytes != null)
                {
                    bytes.Bytes = new byte[count];
                    for (int i = 0; i < count; i++)
                    {
                        bytes.Bytes[i] = (byte)pInner[i];
                    }
                }
                
                return new string(pInner, 0, count, Encoding.UTF8);
            }
        }
    }

    /// <summary>
    /// Wrapper so that we can use an optional byte[] as a parameter
    /// </summary>
    public class ByteWrapper
    {
        public byte[] Bytes { get; set; } = null;
    }
}
