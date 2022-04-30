using System;

namespace Dalamud.Utility
{
    /// <summary>
    /// Extension methods for working with Byte Arrays.
    /// </summary>
    public static class ByteArrayExtensions
    {
        /// <summary>
        /// Terminates a byte array.
        /// </summary>
        /// <param name="arr">Array to terminate.</param>
        /// <returns>Terminated array.</returns>
        public static byte[] Terminate(this byte[] arr)
        {
            var terminated = new byte[arr.Length + 1];
            Array.Copy(arr, terminated, arr.Length);
            terminated[^1] = 0;
            return terminated;
        }
    }
}
