using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory.Exceptions;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.System.String;

using static Dalamud.NativeFunctions;

// Heavily inspired from Reloaded (https://github.com/Reloaded-Project/Reloaded.Memory)

namespace Dalamud.Memory;

/// <summary>
/// A simple class that provides read/write access to arbitrary memory.
/// </summary>
public static unsafe class MemoryHelper
{
    #region Read

    /// <summary>
    /// Reads a generic type from a specified memory address.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <returns>The read in struct.</returns>
    public static T Read<T>(IntPtr memoryAddress) where T : unmanaged
        => Read<T>(memoryAddress, false);

    /// <summary>
    /// Reads a generic type from a specified memory address.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="marshal">Set this to true to enable struct marshalling.</param>
    /// <returns>The read in struct.</returns>
    public static T Read<T>(IntPtr memoryAddress, bool marshal)
    {
        return marshal
                   ? Marshal.PtrToStructure<T>(memoryAddress)
                   : Unsafe.Read<T>((void*)memoryAddress);
    }

    /// <summary>
    /// Reads a byte array from a specified memory address.
    /// </summary>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="length">The amount of bytes to read starting from the memoryAddress.</param>
    /// <returns>The read in byte array.</returns>
    public static byte[] ReadRaw(IntPtr memoryAddress, int length)
    {
        var value = new byte[length];
        Marshal.Copy(memoryAddress, value, 0, value.Length);
        return value;
    }

    /// <summary>
    /// Reads a generic type array from a specified memory address.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="arrayLength">The amount of array items to read.</param>
    /// <returns>The read in struct array.</returns>
    public static T[] Read<T>(IntPtr memoryAddress, int arrayLength) where T : unmanaged
        => Read<T>(memoryAddress, arrayLength, false);

    /// <summary>
    /// Reads a generic type array from a specified memory address.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="arrayLength">The amount of array items to read.</param>
    /// <param name="marshal">Set this to true to enable struct marshalling.</param>
    /// <returns>The read in struct array.</returns>
    public static T[] Read<T>(IntPtr memoryAddress, int arrayLength, bool marshal)
    {
        var structSize = SizeOf<T>(marshal);
        var value = new T[arrayLength];

        for (var i = 0; i < arrayLength; i++)
        {
            var address = memoryAddress + (structSize * i);
            Read(address, out T result, marshal);
            value[i] = result;
        }

        return value;
    }

    /// <summary>
    /// Reads a null-terminated byte array from a specified memory address.
    /// </summary>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <returns>The read in byte array.</returns>
    public static unsafe byte[] ReadRawNullTerminated(IntPtr memoryAddress)
    {
        var byteCount = 0;
        while (*(byte*)(memoryAddress + byteCount) != 0x00)
        {
            byteCount++;
        }

        return ReadRaw(memoryAddress, byteCount);
    }

    #endregion

    #region Read(out)

    /// <summary>
    /// Reads a generic type from a specified memory address.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="value">Local variable to receive the read in struct.</param>
    public static void Read<T>(IntPtr memoryAddress, out T value) where T : unmanaged
        => value = Read<T>(memoryAddress);

    /// <summary>
    /// Reads a generic type from a specified memory address.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="value">Local variable to receive the read in struct.</param>
    /// <param name="marshal">Set this to true to enable struct marshalling.</param>
    public static void Read<T>(IntPtr memoryAddress, out T value, bool marshal)
        => value = Read<T>(memoryAddress, marshal);

    /// <summary>
    /// Reads raw data from a specified memory address.
    /// </summary>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="length">The amount of bytes to read starting from the memoryAddress.</param>
    /// <param name="value">Local variable to receive the read in bytes.</param>
    public static void ReadRaw(IntPtr memoryAddress, int length, out byte[] value)
        => value = ReadRaw(memoryAddress, length);

    /// <summary>
    /// Reads a generic type array from a specified memory address.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="arrayLength">The amount of array items to read.</param>
    /// <param name="value">The read in struct array.</param>
    public static void Read<T>(IntPtr memoryAddress, int arrayLength, out T[] value) where T : unmanaged
        => value = Read<T>(memoryAddress, arrayLength);

    /// <summary>
    /// Reads a generic type array from a specified memory address.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="arrayLength">The amount of array items to read.</param>
    /// <param name="marshal">Set this to true to enable struct marshalling.</param>
    /// <param name="value">The read in struct array.</param>
    public static void Read<T>(IntPtr memoryAddress, int arrayLength, bool marshal, out T[] value)
        => value = Read<T>(memoryAddress, arrayLength, marshal);

    #endregion

    #region ReadString

    /// <summary>
    /// Read a UTF-8 encoded string from a specified memory address.
    /// </summary>
    /// <remarks>
    /// Attention! If this is an <see cref="SeString"/>, use the applicable helper methods to decode.
    /// </remarks>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <returns>The read in string.</returns>
    public static string ReadStringNullTerminated(IntPtr memoryAddress)
        => ReadStringNullTerminated(memoryAddress, Encoding.UTF8);

    /// <summary>
    /// Read a string with the given encoding from a specified memory address.
    /// </summary>
    /// <remarks>
    /// Attention! If this is an <see cref="SeString"/>, use the applicable helper methods to decode.
    /// </remarks>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="encoding">The encoding to use to decode the string.</param>
    /// <returns>The read in string.</returns>
    public static string ReadStringNullTerminated(IntPtr memoryAddress, Encoding encoding)
    {
        var buffer = ReadRawNullTerminated(memoryAddress);
        return encoding.GetString(buffer);
    }

    /// <summary>
    /// Read a UTF-8 encoded string from a specified memory address.
    /// </summary>
    /// <remarks>
    /// Attention! If this is an <see cref="SeString"/>, use the applicable helper methods to decode.
    /// </remarks>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="maxLength">The maximum length of the string.</param>
    /// <returns>The read in string.</returns>
    public static string ReadString(IntPtr memoryAddress, int maxLength)
        => ReadString(memoryAddress, Encoding.UTF8, maxLength);

    /// <summary>
    /// Read a string with the given encoding from a specified memory address.
    /// </summary>
    /// <remarks>
    /// Attention! If this is an <see cref="SeString"/>, use the applicable helper methods to decode.
    /// </remarks>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="encoding">The encoding to use to decode the string.</param>
    /// <param name="maxLength">The maximum length of the string.</param>
    /// <returns>The read in string.</returns>
    public static string ReadString(IntPtr memoryAddress, Encoding encoding, int maxLength)
    {
        if (maxLength <= 0)
            return string.Empty;

        ReadRaw(memoryAddress, maxLength, out var buffer);

        var data = encoding.GetString(buffer);
        var eosPos = data.IndexOf('\0');
        return eosPos >= 0 ? data.Substring(0, eosPos) : data;
    }

    /// <summary>
    /// Read a null-terminated SeString from a specified memory address.
    /// </summary>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <returns>The read in string.</returns>
    public static SeString ReadSeStringNullTerminated(IntPtr memoryAddress)
    {
        var buffer = ReadRawNullTerminated(memoryAddress);
        return SeString.Parse(buffer);
    }

    /// <summary>
    /// Read an SeString from a specified memory address.
    /// </summary>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="maxLength">The maximum length of the string.</param>
    /// <returns>The read in string.</returns>
    public static SeString ReadSeString(IntPtr memoryAddress, int maxLength)
    {
        ReadRaw(memoryAddress, maxLength, out var buffer);

        var eos = Array.IndexOf(buffer, (byte)0);
        if (eos < 0)
        {
            return SeString.Parse(buffer);
        }
        else
        {
            var newBuffer = new byte[eos];
            Buffer.BlockCopy(buffer, 0, newBuffer, 0, eos);
            return SeString.Parse(newBuffer);
        }
    }

    /// <summary>
    /// Read an SeString from a specified Utf8String structure.
    /// </summary>
    /// <param name="utf8String">The memory address to read from.</param>
    /// <returns>The read in string.</returns>
    public static unsafe SeString ReadSeString(Utf8String* utf8String)
    {
        if (utf8String == null)
            return string.Empty;

        var ptr = utf8String->StringPtr;
        if (ptr == null)
            return string.Empty;

        var len = Math.Max(utf8String->BufUsed, utf8String->StringLength);

        return ReadSeString((IntPtr)ptr, (int)len);
    }

    #endregion

    #region ReadString(out)

    /// <summary>
    /// Read a UTF-8 encoded string from a specified memory address.
    /// </summary>
    /// <remarks>
    /// Attention! If this is an <see cref="SeString"/>, use the applicable helper methods to decode.
    /// </remarks>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="value">The read in string.</param>
    public static void ReadStringNullTerminated(IntPtr memoryAddress, out string value)
        => value = ReadStringNullTerminated(memoryAddress);

    /// <summary>
    /// Read a string with the given encoding from a specified memory address.
    /// </summary>
    /// <remarks>
    /// Attention! If this is an <see cref="SeString"/>, use the applicable helper methods to decode.
    /// </remarks>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="encoding">The encoding to use to decode the string.</param>
    /// <param name="value">The read in string.</param>
    public static void ReadStringNullTerminated(IntPtr memoryAddress, Encoding encoding, out string value)
        => value = ReadStringNullTerminated(memoryAddress, encoding);

    /// <summary>
    /// Read a UTF-8 encoded string from a specified memory address.
    /// </summary>
    /// <remarks>
    /// Attention! If this is an <see cref="SeString"/>, use the applicable helper methods to decode.
    /// </remarks>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="value">The read in string.</param>
    /// <param name="maxLength">The maximum length of the string.</param>
    public static void ReadString(IntPtr memoryAddress, out string value, int maxLength)
        => value = ReadString(memoryAddress, maxLength);

    /// <summary>
    /// Read a string with the given encoding from a specified memory address.
    /// </summary>
    /// <remarks>
    /// Attention! If this is an <see cref="SeString"/>, use the applicable helper methods to decode.
    /// </remarks>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="encoding">The encoding to use to decode the string.</param>
    /// <param name="maxLength">The maximum length of the string.</param>
    /// <param name="value">The read in string.</param>
    public static void ReadString(IntPtr memoryAddress, Encoding encoding, int maxLength, out string value)
        => value = ReadString(memoryAddress, encoding, maxLength);

    /// <summary>
    /// Read a null-terminated SeString from a specified memory address.
    /// </summary>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="value">The read in SeString.</param>
    public static void ReadSeStringNullTerminated(IntPtr memoryAddress, out SeString value)
        => value = ReadSeStringNullTerminated(memoryAddress);

    /// <summary>
    /// Read an SeString from a specified memory address.
    /// </summary>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="maxLength">The maximum length of the string.</param>
    /// <param name="value">The read in SeString.</param>
    public static void ReadSeString(IntPtr memoryAddress, int maxLength, out SeString value)
        => value = ReadSeString(memoryAddress, maxLength);

    /// <summary>
    /// Read an SeString from a specified Utf8String structure.
    /// </summary>
    /// <param name="utf8String">The memory address to read from.</param>
    /// <param name="value">The read in string.</param>
    public static unsafe void ReadSeString(Utf8String* utf8String, out SeString value)
        => value = ReadSeString(utf8String);

    #endregion

    #region Write

    /// <summary>
    /// Writes a generic type to a specified memory address.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="item">The item to write to the address.</param>
    public static void Write<T>(IntPtr memoryAddress, T item) where T : unmanaged
        => Write(memoryAddress, item, false);

    /// <summary>
    /// Writes a generic type to a specified memory address.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="item">The item to write to the address.</param>
    /// <param name="marshal">Set this to true to enable struct marshalling.</param>
    public static void Write<T>(IntPtr memoryAddress, T item, bool marshal)
    {
        if (marshal)
            Marshal.StructureToPtr(item, memoryAddress, false);
        else
            Unsafe.Write((void*)memoryAddress, item);
    }

    /// <summary>
    /// Writes raw data to a specified memory address.
    /// </summary>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="data">The bytes to write to memoryAddress.</param>
    public static void WriteRaw(IntPtr memoryAddress, byte[] data)
    {
        Marshal.Copy(data, 0, memoryAddress, data.Length);
    }

    /// <summary>
    /// Writes a generic type array to a specified memory address.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="memoryAddress">The memory address to write to.</param>
    /// <param name="items">The array of items to write to the address.</param>
    public static void Write<T>(IntPtr memoryAddress, T[] items) where T : unmanaged
        => Write(memoryAddress, items, false);

    /// <summary>
    /// Writes a generic type array to a specified memory address.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="memoryAddress">The memory address to write to.</param>
    /// <param name="items">The array of items to write to the address.</param>
    /// <param name="marshal">Set this to true to enable struct marshalling.</param>
    public static void Write<T>(IntPtr memoryAddress, T[] items, bool marshal)
    {
        var structSize = SizeOf<T>(marshal);

        for (var i = 0; i < items.Length; i++)
        {
            var address = memoryAddress + (structSize * i);
            Write(address, items[i], marshal);
        }
    }

    #endregion

    #region WriteString

    /// <summary>
    /// Write a UTF-8 encoded string to a specified memory address.
    /// </summary>
    /// <remarks>
    /// Attention! If this is an <see cref="SeString"/>, use the applicable helper methods to decode.
    /// </remarks>
    /// <param name="memoryAddress">The memory address to write to.</param>
    /// <param name="value">The string to write.</param>
    public static void WriteString(IntPtr memoryAddress, string value)
        => WriteString(memoryAddress, value, Encoding.UTF8);

    /// <summary>
    /// Write a string with the given encoding to a specified memory address.
    /// </summary>
    /// <remarks>
    /// Attention! If this is an <see cref="SeString"/>, use the applicable helper methods to decode.
    /// </remarks>
    /// <param name="memoryAddress">The memory address to write to.</param>
    /// <param name="value">The string to write.</param>
    /// <param name="encoding">The encoding to use.</param>
    public static void WriteString(IntPtr memoryAddress, string value, Encoding encoding)
    {
        if (string.IsNullOrEmpty(value))
            return;

        var bytes = encoding.GetBytes(value + '\0');

        WriteRaw(memoryAddress, bytes);
    }

    /// <summary>
    /// Write an SeString to a specified memory address.
    /// </summary>
    /// <param name="memoryAddress">The memory address to write to.</param>
    /// <param name="value">The SeString to write.</param>
    public static void WriteSeString(IntPtr memoryAddress, SeString value)
    {
        if (value is null)
            return;

        WriteRaw(memoryAddress, value.Encode());
    }

    #endregion

    #region ApiWrappers

    /// <summary>
    /// Allocates fixed size of memory inside the target memory source via Windows API calls.
    /// Returns the address of newly allocated memory.
    /// </summary>
    /// <param name="length">Amount of bytes to be allocated.</param>
    /// <returns>Address to the newly allocated memory.</returns>
    public static IntPtr Allocate(int length)
    {
        var address = VirtualAlloc(
            IntPtr.Zero,
            (UIntPtr)length,
            AllocationType.Commit | AllocationType.Reserve,
            MemoryProtection.ExecuteReadWrite);

        if (address == IntPtr.Zero)
            throw new MemoryAllocationException($"Unable to allocate {length} bytes.");

        return address;
    }

    /// <summary>
    /// Allocates fixed size of memory inside the target memory source via Windows API calls.
    /// Returns the address of newly allocated memory.
    /// </summary>
    /// <param name="length">Amount of bytes to be allocated.</param>
    /// <param name="memoryAddress">Address to the newly allocated memory.</param>
    public static void Allocate(int length, out IntPtr memoryAddress)
        => memoryAddress = Allocate(length);

    /// <summary>
    /// Frees memory previously allocated with Allocate via Windows API calls.
    /// </summary>
    /// <param name="memoryAddress">The address of the memory to free.</param>
    /// <returns>True if the operation is successful.</returns>
    public static bool Free(IntPtr memoryAddress)
    {
        return VirtualFree(memoryAddress, UIntPtr.Zero, AllocationType.Release);
    }

    /// <summary>
    /// Changes the page permissions for a specified combination of address and length via Windows API calls.
    /// </summary>
    /// <param name="memoryAddress">The memory address for which to change page permissions for.</param>
    /// <param name="length">The region size for which to change permissions for.</param>
    /// <param name="newPermissions">The new permissions to set.</param>
    /// <returns>The old page permissions.</returns>
    public static MemoryProtection ChangePermission(IntPtr memoryAddress, int length, MemoryProtection newPermissions)
    {
        var result = VirtualProtect(memoryAddress, (UIntPtr)length, newPermissions, out var oldPermissions);

        if (!result)
            throw new MemoryPermissionException($"Unable to change permissions at 0x{memoryAddress.ToInt64():X} of length {length} and permission {newPermissions} (result={result})");

        var last = Marshal.GetLastWin32Error();
        if (last > 0)
            throw new MemoryPermissionException($"Unable to change permissions at 0x{memoryAddress.ToInt64():X} of length {length} and permission {newPermissions} (error={last})");

        return oldPermissions;
    }

    /// <summary>
    /// Changes the page permissions for a specified combination of address and length via Windows API calls.
    /// </summary>
    /// <param name="memoryAddress">The memory address for which to change page permissions for.</param>
    /// <param name="length">The region size for which to change permissions for.</param>
    /// <param name="newPermissions">The new permissions to set.</param>
    /// <param name="oldPermissions">The old page permissions.</param>
    public static void ChangePermission(IntPtr memoryAddress, int length, MemoryProtection newPermissions, out MemoryProtection oldPermissions)
        => oldPermissions = ChangePermission(memoryAddress, length, newPermissions);

    /// <summary>
    /// Changes the page permissions for a specified combination of address and element from which to deduce size via Windows API calls.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="memoryAddress">The memory address for which to change page permissions for.</param>
    /// <param name="baseElement">The struct element from which the region size to change permissions for will be calculated.</param>
    /// <param name="newPermissions">The new permissions to set.</param>
    /// <param name="marshal">Set to true to calculate the size of the struct after marshalling instead of before.</param>
    /// <returns>The old page permissions.</returns>
    public static MemoryProtection ChangePermission<T>(IntPtr memoryAddress, ref T baseElement, MemoryProtection newPermissions, bool marshal)
        => ChangePermission(memoryAddress, SizeOf<T>(marshal), newPermissions);

    /// <summary>
    /// Reads raw data from a specified memory address via Windows API calls.
    /// This is noticably slower than Unsafe or Marshal.
    /// </summary>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="length">The amount of bytes to read starting from the memoryAddress.</param>
    /// <returns>The read in bytes.</returns>
    public static byte[] ReadProcessMemory(IntPtr memoryAddress, int length)
    {
        var value = new byte[length];
        ReadProcessMemory(memoryAddress, ref value);
        return value;
    }

    /// <summary>
    /// Reads raw data from a specified memory address via Windows API calls.
    /// This is noticably slower than Unsafe or Marshal.
    /// </summary>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="length">The amount of bytes to read starting from the memoryAddress.</param>
    /// <param name="value">The read in bytes.</param>
    public static void ReadProcessMemory(IntPtr memoryAddress, int length, out byte[] value)
        => value = ReadProcessMemory(memoryAddress, length);

    /// <summary>
    /// Reads raw data from a specified memory address via Windows API calls.
    /// This is noticably slower than Unsafe or Marshal.
    /// </summary>
    /// <param name="memoryAddress">The memory address to read from.</param>
    /// <param name="value">The read in bytes.</param>
    public static void ReadProcessMemory(IntPtr memoryAddress, ref byte[] value)
    {
        unchecked
        {
            var length = value.Length;
            var result = NativeFunctions.ReadProcessMemory((IntPtr)0xFFFFFFFF, memoryAddress, value, length, out _);

            if (!result)
                throw new MemoryReadException($"Unable to read memory at 0x{memoryAddress.ToInt64():X} of length {length} (result={result})");

            var last = Marshal.GetLastWin32Error();
            if (last > 0)
                throw new MemoryReadException($"Unable to read memory at 0x{memoryAddress.ToInt64():X} of length {length} (error={last})");
        }
    }

    /// <summary>
    /// Writes raw data to a specified memory address via Windows API calls.
    /// This is noticably slower than Unsafe or Marshal.
    /// </summary>
    /// <param name="memoryAddress">The memory address to write to.</param>
    /// <param name="data">The bytes to write to memoryAddress.</param>
    public static void WriteProcessMemory(IntPtr memoryAddress, byte[] data)
    {
        unchecked
        {
            var length = data.Length;
            var result = NativeFunctions.WriteProcessMemory((IntPtr)0xFFFFFFFF, memoryAddress, data, length, out _);

            if (!result)
                throw new MemoryWriteException($"Unable to write memory at 0x{memoryAddress.ToInt64():X} of length {length} (result={result})");

            var last = Marshal.GetLastWin32Error();
            if (last > 0)
                throw new MemoryWriteException($"Unable to write memory at 0x{memoryAddress.ToInt64():X} of length {length} (error={last})");
        }
    }

    #endregion

    #region Sizing

    /// <summary>
    /// Returns the size of a specific primitive or struct type.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <returns>The size of the primitive or struct.</returns>
    public static int SizeOf<T>()
        => SizeOf<T>(false);

    /// <summary>
    /// Returns the size of a specific primitive or struct type.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="marshal">If set to true; will return the size of an element after marshalling.</param>
    /// <returns>The size of the primitive or struct.</returns>
    public static int SizeOf<T>(bool marshal)
        => marshal ? Marshal.SizeOf<T>() : Unsafe.SizeOf<T>();

    /// <summary>
    /// Returns the size of a specific primitive or struct type.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="elementCount">The number of array elements present.</param>
    /// <returns>The size of the primitive or struct array.</returns>
    public static int SizeOf<T>(int elementCount) where T : unmanaged
        => SizeOf<T>() * elementCount;

    /// <summary>
    /// Returns the size of a specific primitive or struct type.
    /// </summary>
    /// <typeparam name="T">An individual struct type of a class with an explicit StructLayout.LayoutKind attribute.</typeparam>
    /// <param name="elementCount">The number of array elements present.</param>
    /// <param name="marshal">If set to true; will return the size of an element after marshalling.</param>
    /// <returns>The size of the primitive or struct array.</returns>
    public static int SizeOf<T>(int elementCount, bool marshal)
        => SizeOf<T>(marshal) * elementCount;

    #endregion

    #region Game

    /// <summary>
    /// Allocate memory in the game's UI memory space.
    /// </summary>
    /// <param name="size">Amount of bytes to allocate.</param>
    /// <param name="alignment">The alignment of the allocation.</param>
    /// <returns>Pointer to the allocated region.</returns>
    public static IntPtr GameAllocateUi(ulong size, ulong alignment = 0)
    {
        return new IntPtr(IMemorySpace.GetUISpace()->Malloc(size, alignment));
    }

    /// <summary>
    /// Allocate memory in the game's default memory space.
    /// </summary>
    /// <param name="size">Amount of bytes to allocate.</param>
    /// <param name="alignment">The alignment of the allocation.</param>
    /// <returns>Pointer to the allocated region.</returns>
    public static IntPtr GameAllocateDefault(ulong size, ulong alignment = 0)
    {
        return new IntPtr(IMemorySpace.GetDefaultSpace()->Malloc(size, alignment));
    }

    /// <summary>
    /// Allocate memory in the game's animation memory space.
    /// </summary>
    /// <param name="size">Amount of bytes to allocate.</param>
    /// <param name="alignment">The alignment of the allocation.</param>
    /// <returns>Pointer to the allocated region.</returns>
    public static IntPtr GameAllocateAnimation(ulong size, ulong alignment = 0)
    {
        return new IntPtr(IMemorySpace.GetAnimationSpace()->Malloc(size, alignment));
    }

    /// <summary>
    /// Allocate memory in the game's apricot memory space.
    /// </summary>
    /// <param name="size">Amount of bytes to allocate.</param>
    /// <param name="alignment">The alignment of the allocation.</param>
    /// <returns>Pointer to the allocated region.</returns>
    public static IntPtr GameAllocateApricot(ulong size, ulong alignment = 0)
    {
        return new IntPtr(IMemorySpace.GetApricotSpace()->Malloc(size, alignment));
    }

    /// <summary>
    /// Allocate memory in the game's sound memory space.
    /// </summary>
    /// <param name="size">Amount of bytes to allocate.</param>
    /// <param name="alignment">The alignment of the allocation.</param>
    /// <returns>Pointer to the allocated region.</returns>
    public static IntPtr GameAllocateSound(ulong size, ulong alignment = 0)
    {
        return new IntPtr(IMemorySpace.GetSoundSpace()->Malloc(size, alignment));
    }

    /// <summary>
    /// Free memory in the game's memory space.
    /// </summary>
    /// <remarks>The memory you are freeing must be allocated with game allocators.</remarks>
    /// <param name="ptr">Position at which the memory to be freed is located.</param>
    /// <param name="size">Amount of bytes to free.</param>
    public static void GameFree(ref IntPtr ptr, ulong size)
    {
        if (ptr == IntPtr.Zero)
        {
            return;
        }

        IMemorySpace.Free((void*)ptr, size);
        ptr = IntPtr.Zero;
    }

    #endregion

    #region Utility

    /// <summary>
    /// Null-terminate a byte array.
    /// </summary>
    /// <param name="bytes">The byte array to terminate.</param>
    /// <returns>The terminated byte array.</returns>
    public static byte[] NullTerminate(this byte[] bytes)
    {
        if (bytes.Length == 0 || bytes[^1] != 0)
        {
            var newBytes = new byte[bytes.Length + 1];
            Array.Copy(bytes, newBytes, bytes.Length);
            newBytes[^1] = 0;

            return newBytes;
        }

        return bytes;
    }

    #endregion
}
