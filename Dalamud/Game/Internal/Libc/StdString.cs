using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Dalamud.Game.Internal.Libc {
    /// <summary>
    /// Interation with std::string
    /// </summary>
    public class StdString {
        public static StdString ReadFromPointer(IntPtr cstring) {
            unsafe {
                if (cstring == IntPtr.Zero) {
                    throw new ArgumentNullException(nameof(cstring));
                }
                
                var innerAddress = Marshal.ReadIntPtr(cstring);
                if (innerAddress == IntPtr.Zero) {
                    throw new NullReferenceException("Inner reference to the cstring is null.");
                }

                var count = 0;
                
                // Count the number of chars. String is assumed to be zero-terminated.
                while (Marshal.ReadByte(innerAddress + count) != 0) {
                    count += 1;
                }

                // raw copy, as UTF8 string conversion is lossy
                var rawData = new byte[count];
                Marshal.Copy(innerAddress, rawData, 0, count);

                return new StdString {
                    RawData = rawData,
                    Value = Encoding.UTF8.GetString(rawData)
                };
            }
        }

        private StdString() { }

        public string Value { get; private set; }

        public byte[] RawData { get; set; }
    }
}
