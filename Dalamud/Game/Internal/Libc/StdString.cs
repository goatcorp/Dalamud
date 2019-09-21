using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Dalamud.Game.Internal.Libc {
    /// <summary>
    /// Interation with std::string
    /// </summary>
    public static class StdString {
        public static string ReadFromPointer(IntPtr cstring) {
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
                
                return new string(pInner, 0, count, Encoding.UTF8);
            }
        }
    }
}
