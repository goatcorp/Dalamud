using System.Runtime.InteropServices;
using System.Text;

namespace Dalamud;

/// <summary>
/// Class facilitating safe memory access.
/// </summary>
/// <remarks>
/// Attention! The performance of these methods is severely worse than regular <see cref="Marshal"/> calls.
/// Please consider using those instead in performance-critical code.
/// </remarks>
public static class SafeMemory
{
    private static readonly IntPtr Handle;

    static SafeMemory()
    {
        Handle = Imports.GetCurrentProcess();
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
    public static bool Read<T>(IntPtr address, out T result) where T : struct
    {
        if (!ReadBytes(address, SizeCache<T>.Size, out var buffer))
        {
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
    public static T[]? Read<T>(IntPtr address, int count) where T : struct
    {
        var size = SizeOf<T>();
        if (!ReadBytes(address, count * size, out var buffer))
            return null;
        var result = new T[count];
        using var mem = new LocalMemory(buffer.Length);
        mem.Write(buffer);
        for (var i = 0; i < result.Length; i++)
            result[i] = mem.Read<T>(i * size);
        return result;
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
        using var mem = new LocalMemory(SizeCache<T>.Size);
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
        var size = SizeCache<T>.Size;
        using var mem = new LocalMemory(objArray.Length * size);
        for (var i = 0; i < objArray.Length; i++)
            mem.Write(objArray[i], i * size);
        return WriteBytes(address, mem.Read());
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
    public static string? ReadString(IntPtr address, int maxLength = 256)
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
    public static string? ReadString(IntPtr address, Encoding encoding, int maxLength = 256)
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
    /// Marshals data from an unmanaged block of memory to a managed object.
    /// </summary>
    /// <typeparam name="T">The type to create.</typeparam>
    /// <param name="addr">The address to read from.</param>
    /// <returns>The read object, or null, if it could not be read.</returns>
    public static T? PtrToStructure<T>(IntPtr addr) where T : struct => (T?)PtrToStructure(addr, typeof(T));

    /// <summary>
    /// Marshals data from an unmanaged block of memory to a managed object.
    /// </summary>
    /// <param name="addr">The address to read from.</param>
    /// <param name="type">The type to create.</param>
    /// <returns>The read object, or null, if it could not be read.</returns>
    public static object? PtrToStructure(IntPtr addr, Type type)
    {
        var size = Marshal.SizeOf(type);

        if (!ReadBytes(addr, size, out var buffer))
            return null;

        var mem = new LocalMemory(size);
        mem.Write(buffer);

        return mem.Read(type);
    }

    /// <summary>
    /// Get the size of the passed type.
    /// </summary>
    /// <typeparam name="T">The type to inspect.</typeparam>
    /// <returns>The size of the passed type.</returns>
    public static int SizeOf<T>() where T : struct
    {
        return SizeCache<T>.Size;
    }

    private static class SizeCache<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        public static readonly int Size;

        static SizeCache()
        {
            var type = typeof(T);
            if (type.IsEnum)
                type = type.GetEnumUnderlyingType();
            Size = Type.GetTypeCode(type) == TypeCode.Boolean ? 1 : Marshal.SizeOf(type);
        }
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
        private readonly int size;

        private IntPtr hGlobal;

        public LocalMemory(int size)
        {
            this.size = size;
            this.hGlobal = Marshal.AllocHGlobal(this.size);
        }

        ~LocalMemory() => this.Dispose();

        public byte[] Read()
        {
            var bytes = new byte[this.size];
            Marshal.Copy(this.hGlobal, bytes, 0, this.size);
            return bytes;
        }

        public T Read<T>(int offset = 0) => (T)Marshal.PtrToStructure(this.hGlobal + offset, typeof(T));

        public object? Read(Type type, int offset = 0) => Marshal.PtrToStructure(this.hGlobal + offset, type);

        public void Write(byte[] data, int index = 0) => Marshal.Copy(data, index, this.hGlobal, this.size);

        public void Write<T>(T data, int offset = 0) => Marshal.StructureToPtr(data, this.hGlobal + offset, false);

        public void Dispose()
        {
            Marshal.FreeHGlobal(this.hGlobal);
            this.hGlobal = IntPtr.Zero;
            GC.SuppressFinalize(this);
        }
    }
}
