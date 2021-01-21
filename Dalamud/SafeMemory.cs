using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Dalamud
{
    /// <summary>
    /// Class facilitating safe memory access
    /// </summary>
    /// <remarks>
    /// Attention! The performance of these methods is severely worse than regular <see cref="Marshal"/> calls.
    /// Please consider using these instead in performance-critical code.
    /// </remarks>
    public static class SafeMemory
    {
        private static readonly IntPtr Handle;
        private static readonly IntPtr MainModule;

        static SafeMemory()
        {
            Handle = Imports.GetCurrentProcess();
            MainModule = Process.GetCurrentProcess().MainModule?.BaseAddress ?? IntPtr.Zero;
        }

        /// <summary>
        /// Read a byte array from the current process.
        /// </summary>
        /// <param name="address">The address to read from.</param>
        /// <param name="count">The amount of bytes to read.</param>
        /// <param name="buffer">The result buffer.</param>
        /// <returns>Whether or not the read succeeded.</returns>
        public static bool ReadBytes(IntPtr address, int count, out byte[] buffer)
        {
            buffer = new byte[count <= 0 ? 0 : count];
            return Imports.ReadProcessMemory(Handle, address, buffer, buffer.Length, out _);
        }

        /// <summary>
        /// Write a byte array to the current process.
        /// </summary>
        /// <param name="address">The address to write to.</param>
        /// <param name="buffer">The buffer to write.</param>
        /// <returns>Whether or not the write succeeded.</returns>
        public static bool WriteBytes(IntPtr address, byte[] buffer)
        {
            return Imports.WriteProcessMemory(Handle, address, buffer, buffer.Length, out _);
        }

        /// <summary>
        /// Read an object of the specified struct from the current process.
        /// </summary>
        /// <typeparam name="T">The type of the struct.</typeparam>
        /// <param name="address">The address to read from.</param>
        /// <param name="result">The resulting object.</param>
        /// <returns>Whether or not the read succeeded.</returns>
        public static bool Read<T>(IntPtr address, out T result) where T : struct {
            if (!ReadBytes(address, SizeOf<T>(), out var buffer)) {
                result = default;
                return false;
            }

            using var mem = new LocalMemory(buffer.Length);
            mem.Write(buffer);

            result = mem.Read<T>();
            return true;
        }

        /// <summary>
        /// Read an array of objects of the specified struct from the current process.
        /// </summary>
        /// <typeparam name="T">The type of the struct.</typeparam>
        /// <param name="address">The address to read from.</param>
        /// <param name="count">The length of the array.</param>
        /// <returns>An array of the read objects, or null if any entry of the array failed to read.</returns>
        [CanBeNull]
        public static T[] Read<T>(IntPtr address, int count) where T : struct
        {
            var size = SizeOf<T>();
            var result = new T[count];

            var readSucceeded = true;
            for (var i = 0; i < count; i++) {
                var success = Read<T>(address + i * size, out var res);

                if (!success)
                    readSucceeded = false;

                result[i] = res;
            }

            return !readSucceeded ? null : result;
        }

        /// <summary>
        /// Write a struct to the current process.
        /// </summary>
        /// <typeparam name="T">The type of the struct.</typeparam>
        /// <param name="address">The address to write to.</param>
        /// <param name="obj">The object to write.</param>
        /// <returns>Whether or not the write succeeded.</returns>
        public static bool Write<T>(IntPtr address, T obj) where T : struct
        {
            using var mem = new LocalMemory(SizeOf<T>());
            mem.Write(obj);
            return WriteBytes(address, mem.Read());
        }

        /// <summary>
        /// Write an array of structs to the current process.
        /// </summary>
        /// <typeparam name="T">The type of the structs.</typeparam>
        /// <param name="address">The address to write to.</param>
        /// <param name="objArray">The array to write.</param>
        /// <returns>Whether or not the write succeeded.</returns>
        public static bool Write<T>(IntPtr address, T[] objArray) where T : struct
        {
            if (objArray == null || objArray.Length == 0)
                return true;
            var size = SizeOf<T>();
            for (var i = 0; i < objArray.Length; i++)
                if (!Write(address + i * size, objArray[i]))
                    return false;
            return true;
        }

        /// <summary>
        /// Read a string from the current process(UTF-8).
        /// </summary>
        /// <remarks>
        /// Attention! This will use the .NET Encoding.UTF8 class to decode the text.
        /// If you read a FFXIV string, please use ReadBytes and parse the string with the applicable class,
        /// since Encoding.UTF8 destroys the FFXIV payload structure.
        /// </remarks>
        /// <param name="address">The address to read from.</param>
        /// <param name="maxLength">The maximum length of the string.</param>
        /// <returns>The read string, or null in case the read was not successful.</returns>
        [CanBeNull]
        public static string ReadString(IntPtr address, int maxLength = 256)
        {
            return ReadString(address, Encoding.UTF8, maxLength);
        }

        /// <summary>
        /// Read a string from the current process(UTF-8).
        /// </summary>
        /// <remarks>
        /// Attention! This will use the .NET Encoding class to decode the text.
        /// If you read a FFXIV string, please use ReadBytes and parse the string with the applicable class,
        /// since Encoding may destroy the FFXIV payload structure.
        /// </remarks>
        /// <param name="address">The address to read from.</param>
        /// <param name="encoding">The encoding to use to decode the string.</param>
        /// <param name="maxLength">The maximum length of the string.</param>
        /// <returns>The read string, or null in case the read was not successful.</returns>
        [CanBeNull]
        public static string ReadString(IntPtr address, Encoding encoding, int maxLength = 256)
        {
            if (!ReadBytes(address, maxLength, out var buffer))
                return null;
            var data = encoding.GetString(buffer);
            var eosPos = data.IndexOf('\0');
            return eosPos == -1 ? data : data.Substring(0, eosPos);
        }

        /// <summary>
        /// Write a string to the current process.
        /// </summary>
        /// <remarks>
        /// Attention! This will use the .NET Encoding class to encode the text.
        /// If you read a FFXIV string, please use WriteBytes with the applicable encoded SeString,
        /// since Encoding may destroy the FFXIV payload structure.
        /// </remarks>
        /// <param name="address">The address to write to.</param>
        /// <param name="str">The string to write.</param>
        /// <returns>Whether or not the write succeeded.</returns>
        public static bool WriteString(IntPtr address, string str)
        {
            return WriteString(address, str, Encoding.UTF8);
        }

        /// <summary>
        /// Write a string to the current process.
        /// </summary>
        /// <remarks>
        /// Attention! This will use the .NET Encoding class to encode the text.
        /// If you read a FFXIV string, please use WriteBytes with the applicable encoded SeString,
        /// since Encoding may destroy the FFXIV payload structure.
        /// </remarks>
        /// <param name="address">The address to write to.</param>
        /// <param name="str">The string to write.</param>
        /// <param name="encoding">The encoding to use.</param>
        /// <returns>Whether or not the write succeeded.</returns>
        public static bool WriteString(IntPtr address, string str, Encoding encoding)
        {
            if (string.IsNullOrEmpty(str))
                return true;
            return WriteBytes(address, encoding.GetBytes(str + "\0"));
        }

        /// <summary>
        /// Get the size of the passed type.
        /// </summary>
        /// <typeparam name="T">The type to inspect.</typeparam>
        /// <returns>The size of the passed type.</returns>
        public static int SizeOf<T>()
        {
            var type = typeof(T);
            if (type.IsEnum)
                type = type.GetEnumUnderlyingType();
            return Type.GetTypeCode(type) == TypeCode.Boolean ? 1 : Marshal.SizeOf(type);
        }

        private static class Imports
        {
            [DllImport("kernel32", SetLastError = true)]
            public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int nSize, out int lpNumberOfBytesRead);

            [DllImport("kernel32", SetLastError = true)]
            public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);

            [DllImport("kernel32", SetLastError = false)]
            public static extern IntPtr GetCurrentProcess();
        }

        private sealed class LocalMemory : IDisposable
        {
            private IntPtr hGlobal;
            private readonly int size;

            public LocalMemory(int size)
            {
                this.size = size;
                this.hGlobal = Marshal.AllocHGlobal(this.size);
            }

            public byte[] Read()
            {
                var bytes = new byte[this.size];
                Marshal.Copy(this.hGlobal, bytes, 0, this.size);
                return bytes;
            }

            public T Read<T>() => (T)Marshal.PtrToStructure(this.hGlobal, typeof(T));
            public void Write(byte[] data, int index = 0) => Marshal.Copy(data, index, this.hGlobal, this.size);
            public void Write<T>(T data) => Marshal.StructureToPtr(data, this.hGlobal, false);
            ~LocalMemory() => Dispose();

            public void Dispose()
            {
                Marshal.FreeHGlobal(this.hGlobal);
                this.hGlobal = IntPtr.Zero;
                GC.SuppressFinalize(this);
            }
        }
    }
}
