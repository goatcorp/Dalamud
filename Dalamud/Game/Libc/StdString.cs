using System.Runtime.InteropServices;
using System.Text;

namespace Dalamud.Game.Libc;

/// <summary>
/// Interation with std::string.
/// </summary>
public class StdString
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StdString"/> class.
    /// </summary>
    private StdString()
    {
    }

    /// <summary>
    /// Gets the value of the cstring.
    /// </summary>
    public string Value { get; private set; }

    /// <summary>
    /// Gets or sets the raw byte representation of the cstring.
    /// </summary>
    public byte[] RawData { get; set; }

    /// <summary>
    /// Marshal a null terminated cstring from memory to a UTF-8 encoded string.
    /// </summary>
    /// <param name="cstring">Address of the cstring.</param>
    /// <returns>A UTF-8 encoded string.</returns>
    public static StdString ReadFromPointer(IntPtr cstring)
    {
        unsafe
        {
            if (cstring == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(cstring));
            }

            var innerAddress = Marshal.ReadIntPtr(cstring);
            if (innerAddress == IntPtr.Zero)
            {
                throw new NullReferenceException("Inner reference to the cstring is null.");
            }

            // Count the number of chars. String is assumed to be zero-terminated.

            var count = 0;
            while (Marshal.ReadByte(innerAddress, count) != 0)
            {
                count += 1;
            }

            // raw copy, as UTF8 string conversion is lossy
            var rawData = new byte[count];
            Marshal.Copy(innerAddress, rawData, 0, count);

            return new StdString
            {
                RawData = rawData,
                Value = Encoding.UTF8.GetString(rawData),
            };
        }
    }
}
