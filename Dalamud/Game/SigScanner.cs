using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Iced.Intel;

using Newtonsoft.Json;

using Serilog;

namespace Dalamud.Game;

// TODO(v9): There are static functions here that we can't keep due to interfaces

/// <summary>
/// A SigScanner facilitates searching for memory signatures in a given ProcessModule.
/// </summary>
public class SigScanner : IDisposable, ISigScanner
{
    private readonly FileInfo? cacheFile;

    private nint moduleCopyPtr;
    private nint moduleCopyOffset;

    private ConcurrentDictionary<string, long>? textCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="SigScanner"/> class using the main module of the current process.
    /// </summary>
    /// <param name="doCopy">Whether or not to copy the module upon initialization for search operations to use, as to not get disturbed by possible hooks.</param>
    /// <param name="cacheFile">File used to cached signatures.</param>
    public SigScanner(bool doCopy = false, FileInfo? cacheFile = null)
        : this(Process.GetCurrentProcess().MainModule!, doCopy, cacheFile)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SigScanner"/> class.
    /// </summary>
    /// <param name="module">The ProcessModule to be used for scanning.</param>
    /// <param name="doCopy">Whether or not to copy the module upon initialization for search operations to use, as to not get disturbed by possible hooks.</param>
    /// <param name="cacheFile">File used to cached signatures.</param>
    public SigScanner(ProcessModule module, bool doCopy = false, FileInfo? cacheFile = null)
    {
        this.cacheFile = cacheFile;
        this.Module = module;
        this.Is32BitProcess = !Environment.Is64BitProcess;
        this.IsCopy = doCopy;

        if (this.IsCopy)
            this.SetupCopiedSegments();

        // Limit the search space to .text section.
        this.SetupSearchSpace(module);

        Log.Verbose($"Module base: 0x{this.TextSectionBase.ToInt64():X}");
        Log.Verbose($"Module size: 0x{this.TextSectionSize:X}");

        if (cacheFile != null)
            this.Load();
    }

    /// <inheritdoc/>
    public bool IsCopy { get; }

    /// <inheritdoc/>
    public bool Is32BitProcess { get; }

    /// <inheritdoc/>
    public IntPtr SearchBase => this.IsCopy ? this.moduleCopyPtr : this.Module.BaseAddress;

    /// <inheritdoc/>
    public IntPtr TextSectionBase => new(this.SearchBase.ToInt64() + this.TextSectionOffset);

    /// <inheritdoc/>
    public long TextSectionOffset { get; private set; }

    /// <inheritdoc/>
    public int TextSectionSize { get; private set; }

    /// <inheritdoc/>
    public IntPtr DataSectionBase => new(this.SearchBase.ToInt64() + this.DataSectionOffset);

    /// <inheritdoc/>
    public long DataSectionOffset { get; private set; }

    /// <inheritdoc/>
    public int DataSectionSize { get; private set; }

    /// <inheritdoc/>
    public IntPtr RDataSectionBase => new(this.SearchBase.ToInt64() + this.RDataSectionOffset);

    /// <inheritdoc/>
    public long RDataSectionOffset { get; private set; }

    /// <inheritdoc/>
    public int RDataSectionSize { get; private set; }

    /// <inheritdoc/>
    public ProcessModule Module { get; }

    /// <summary>Gets or sets a value indicating whether this instance of <see cref="SigScanner"/> is meant to be a
    /// Dalamud service.</summary>
    private protected bool IsService { get; set; }

    private IntPtr TextSectionTop => this.TextSectionBase + this.TextSectionSize;

    /// <summary>
    /// Scan memory for a signature.
    /// </summary>
    /// <param name="baseAddress">The base address to scan from.</param>
    /// <param name="size">The amount of bytes to scan.</param>
    /// <param name="signature">The signature to search for.</param>
    /// <returns>The found offset.</returns>
    public static IntPtr Scan(IntPtr baseAddress, int size, string signature)
    {
        var (needle, mask, badShift) = ParseSignature(signature);
        var index = IndexOf(baseAddress, size, needle, mask, badShift);
        if (index < 0)
            throw new KeyNotFoundException($"Can't find a signature of {signature}");
        return baseAddress + index;
    }

    /// <summary>
    /// Try scanning memory for a signature.
    /// </summary>
    /// <param name="baseAddress">The base address to scan from.</param>
    /// <param name="size">The amount of bytes to scan.</param>
    /// <param name="signature">The signature to search for.</param>
    /// <param name="result">The offset, if found.</param>
    /// <returns>true if the signature was found.</returns>
    public static bool TryScan(IntPtr baseAddress, int size, string signature, out IntPtr result)
    {
        try
        {
            result = Scan(baseAddress, size, signature);
            return true;
        }
        catch (KeyNotFoundException)
        {
            result = IntPtr.Zero;
            return false;
        }
    }

    /// <summary>
    /// Scan for a .data address using a .text function.
    /// This is intended to be used with IDA sigs.
    /// Place your cursor on the line calling a static address, and create and IDA sig.
    /// The signature and offset should not break through instruction boundaries.
    /// </summary>
    /// <param name="signature">The signature of the function using the data.</param>
    /// <param name="offset">The offset from function start of the instruction using the data.</param>
    /// <returns>An IntPtr to the static memory location.</returns>
    public unsafe IntPtr GetStaticAddressFromSig(string signature, int offset = 0)
    {
        var instructionAddress = (byte*)this.ScanText(signature);
        instructionAddress += offset;

        try
        {
            var reader = new UnsafeCodeReader(instructionAddress, signature.Length + 8);
            var decoder = Decoder.Create(64, reader, (ulong)instructionAddress, DecoderOptions.AMD);
            while (reader.CanReadByte)
            {
                var instruction = decoder.Decode();
                if (instruction.IsInvalid) continue;
                if (instruction.Op0Kind is OpKind.Memory || instruction.Op1Kind is OpKind.Memory)
                {
                    return (IntPtr)instruction.MemoryDisplacement64;
                }
            }
        }
        catch
        {
            // ignored
        }

        throw new KeyNotFoundException($"Can't find any referenced address in the given signature {signature}.");
    }

    /// <summary>
    /// Try scanning for a .data address using a .text function.
    /// This is intended to be used with IDA sigs.
    /// Place your cursor on the line calling a static address, and create and IDA sig.
    /// </summary>
    /// <param name="signature">The signature of the function using the data.</param>
    /// <param name="result">An IntPtr to the static memory location, if found.</param>
    /// <param name="offset">The offset from function start of the instruction using the data.</param>
    /// <returns>true if the signature was found.</returns>
    public bool TryGetStaticAddressFromSig(string signature, out IntPtr result, int offset = 0)
    {
        try
        {
            result = this.GetStaticAddressFromSig(signature, offset);
            return true;
        }
        catch (KeyNotFoundException)
        {
            result = IntPtr.Zero;
            return false;
        }
    }

    /// <inheritdoc/>
    public IntPtr ScanData(string signature)
    {
        var scanRet = Scan(this.DataSectionBase, this.DataSectionSize, signature);

        if (this.IsCopy)
            scanRet = new IntPtr(scanRet.ToInt64() - this.moduleCopyOffset);

        return scanRet;
    }

    /// <inheritdoc/>
    public bool TryScanData(string signature, out IntPtr result)
    {
        try
        {
            result = this.ScanData(signature);
            return true;
        }
        catch (KeyNotFoundException)
        {
            result = IntPtr.Zero;
            return false;
        }
    }

    /// <inheritdoc/>
    public IntPtr ScanModule(string signature)
    {
        var scanRet = Scan(this.SearchBase, this.Module.ModuleMemorySize, signature);

        if (this.IsCopy)
            scanRet = new IntPtr(scanRet.ToInt64() - this.moduleCopyOffset);

        return scanRet;
    }

    /// <inheritdoc/>
    public bool TryScanModule(string signature, out IntPtr result)
    {
        try
        {
            result = this.ScanModule(signature);
            return true;
        }
        catch (KeyNotFoundException)
        {
            result = IntPtr.Zero;
            return false;
        }
    }

    /// <inheritdoc/>
    public IntPtr ResolveRelativeAddress(IntPtr nextInstAddr, int relOffset)
    {
        if (this.Is32BitProcess) throw new NotSupportedException("32 bit is not supported.");
        return nextInstAddr + relOffset;
    }

    /// <inheritdoc/>
    public IntPtr ScanText(string signature)
    {
        if (this.textCache != null)
        {
            if (this.textCache.TryGetValue(signature, out var address))
            {
                return new IntPtr(address + this.Module.BaseAddress.ToInt64());
            }
        }

        var scanRet = Scan(this.TextSectionBase, this.TextSectionSize, signature);

        if (this.IsCopy)
            scanRet = new IntPtr(scanRet.ToInt64() - this.moduleCopyOffset);

        var insnByte = Marshal.ReadByte(scanRet);

        if (insnByte == 0xE8 || insnByte == 0xE9)
        {
            scanRet = ReadJmpCallSig(scanRet);
            var rel = scanRet - this.Module.BaseAddress;
            if (rel < 0 || rel >= this.TextSectionSize)
            {
                throw new KeyNotFoundException(
                    $"Signature \"{signature}\" resolved to 0x{rel:X} which is outside .text section. Possible signature conflicts?");
            }
        }

        // If this is below the module, there's bound to be a problem with the sig/resolution... Let's not save it
        // TODO: THIS IS A HACK! FIX THE ROOT CAUSE!
        if (this.textCache != null && scanRet.ToInt64() >= this.Module.BaseAddress.ToInt64())
        {
            this.textCache[signature] = scanRet.ToInt64() - this.Module.BaseAddress.ToInt64();
        }

        return scanRet;
    }

    /// <inheritdoc/>
    public bool TryScanText(string signature, out IntPtr result)
    {
        try
        {
            result = this.ScanText(signature);
            return true;
        }
        catch (KeyNotFoundException)
        {
            result = IntPtr.Zero;
            return false;
        }
    }

    /// <inheritdoc/>
    public nint[] ScanAllText(string signature) => this.ScanAllText(signature, default).ToArray();

    /// <inheritdoc/>
    public IEnumerable<nint> ScanAllText(string signature, CancellationToken cancellationToken)
    {
        var (needle, mask, badShift) = ParseSignature(signature);
        var mBase = this.TextSectionBase;
        var mTo = this.TextSectionBase + this.TextSectionSize;
        while (mBase < mTo)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var index = IndexOf(mBase, this.TextSectionSize, needle, mask, badShift);
            if (index < 0)
                break;

            var scanRet = mBase + index;
            if (this.IsCopy)
                scanRet -= this.moduleCopyOffset;

            yield return scanRet;
            mBase = scanRet + 1;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!this.IsService)
            this.DisposeCore();
    }

    /// <summary>
    /// Save the current state of the cache.
    /// </summary>
    internal void Save()
    {
        if (this.cacheFile == null)
            return;

        try
        {
            File.WriteAllText(this.cacheFile.FullName, JsonConvert.SerializeObject(this.textCache));
            Log.Information("Saved cache to {CachePath}", this.cacheFile);
        }
        catch (Exception e)
        {
            Log.Warning(e, "Failed to save cache to {CachePath}", this.cacheFile);
        }
    }

    /// <summary>
    /// Free the memory of the copied module search area on object disposal, if applicable.
    /// </summary>
    private protected void DisposeCore()
    {
        this.Save();
        Marshal.FreeHGlobal(this.moduleCopyPtr);
    }

    /// <summary>
    /// Helper for ScanText to get the correct address for IDA sigs that mark the first JMP or CALL location.
    /// </summary>
    /// <param name="sigLocation">The address the JMP or CALL sig resolved to.</param>
    /// <returns>The real offset of the signature.</returns>
    private static IntPtr ReadJmpCallSig(IntPtr sigLocation)
    {
        var jumpOffset = Marshal.ReadInt32(sigLocation, 1);
        return IntPtr.Add(sigLocation, 5 + jumpOffset);
    }

    private static (byte[] Needle, bool[] Mask, int[] BadShift) ParseSignature(string signature)
    {
        signature = signature.Replace(" ", string.Empty);
        if (signature.Length % 2 != 0)
            throw new ArgumentException("Signature without whitespaces must be divisible by two.", nameof(signature));

        var needleLength = signature.Length / 2;
        var needle = new byte[needleLength];
        var mask = new bool[needleLength];
        for (var i = 0; i < needleLength; i++)
        {
            var hexString = signature.Substring(i * 2, 2);
            if (hexString == "??" || hexString == "**")
            {
                needle[i] = 0;
                mask[i] = true;
                continue;
            }

            needle[i] = byte.Parse(hexString, NumberStyles.AllowHexSpecifier);
            mask[i] = false;
        }

        return (needle, mask, BuildBadCharTable(needle, mask));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe int IndexOf(nint bufferPtr, int bufferLength, byte[] needle, bool[] mask, int[] badShift)
    {
        if (needle.Length > bufferLength) return -1;
        var last = needle.Length - 1;
        var offset = 0;
        var maxoffset = bufferLength - needle.Length;
        var buffer = (byte*)bufferPtr;

        while (offset <= maxoffset)
        {
            int position;
            for (position = last; needle[position] == *(buffer + position + offset) || mask[position]; position--)
            {
                if (position == 0)
                    return offset;
            }

            offset += badShift[*(buffer + offset + last)];
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int[] BuildBadCharTable(byte[] needle, bool[] mask)
    {
        int idx;
        var last = needle.Length - 1;
        var badShift = new int[256];
        for (idx = last; idx > 0 && !mask[idx]; --idx)
        {
        }

        var diff = last - idx;
        if (diff == 0) diff = 1;

        for (idx = 0; idx <= 255; ++idx)
            badShift[idx] = diff;
        for (idx = last - diff; idx < last; ++idx)
            badShift[needle[idx]] = last - idx;
        return badShift;
    }

    private void SetupSearchSpace(ProcessModule module)
    {
        var fileBytes = File.ReadAllBytes(module.FileName);

        var baseAddress = module.BaseAddress;

        // We don't want to read all of IMAGE_DOS_HEADER or IMAGE_NT_HEADER stuff so we cheat here.
        var ntNewOffset = Marshal.ReadInt32(baseAddress, 0x3C);
        var ntHeader = baseAddress + ntNewOffset;

        // IMAGE_NT_HEADER
        var fileHeader = ntHeader + 4;
        var numSections = Marshal.ReadInt16(ntHeader, 6);

        // IMAGE_OPTIONAL_HEADER
        var optionalHeader = fileHeader + 20;

        IntPtr sectionHeader;
        if (this.Is32BitProcess) // IMAGE_OPTIONAL_HEADER32
            sectionHeader = optionalHeader + 224;
        else // IMAGE_OPTIONAL_HEADER64
            sectionHeader = optionalHeader + 240;

        // IMAGE_SECTION_HEADER
        var sectionCursor = sectionHeader;
        for (var i = 0; i < numSections; i++)
        {
            var sectionName = Marshal.ReadInt64(sectionCursor);

            // .text
            switch (sectionName)
            {
                case 0x747865742E: // .text
                    this.TextSectionOffset = Marshal.ReadInt32(sectionCursor, 12);
                    this.TextSectionSize = Marshal.ReadInt32(sectionCursor, 8);

                    var pointerToRawData = Marshal.ReadInt32(sectionCursor, 20);

                    Marshal.Copy(
                                 fileBytes.AsSpan(pointerToRawData, this.TextSectionSize).ToArray(),
                                 0,
                                 this.moduleCopyPtr + (nint)this.TextSectionOffset,
                                 this.TextSectionSize);

                    break;
                case 0x617461642E: // .data
                    this.DataSectionOffset = Marshal.ReadInt32(sectionCursor, 12);
                    this.DataSectionSize = Marshal.ReadInt32(sectionCursor, 8);

                    break;
                case 0x61746164722E: // .rdata
                    this.RDataSectionOffset = Marshal.ReadInt32(sectionCursor, 12);
                    this.RDataSectionSize = Marshal.ReadInt32(sectionCursor, 8);

                    break;
            }

            sectionCursor += 40;
        }
    }

    private unsafe void SetupCopiedSegments()
    {
        // .text
        this.moduleCopyPtr = Marshal.AllocHGlobal(this.Module.ModuleMemorySize);
        Buffer.MemoryCopy(
            this.Module.BaseAddress.ToPointer(),
            this.moduleCopyPtr.ToPointer(),
            this.Module.ModuleMemorySize,
            this.Module.ModuleMemorySize);

        this.moduleCopyOffset = this.moduleCopyPtr - this.Module.BaseAddress;
    }

    private void Load()
    {
        if (this.cacheFile is not { Exists: true })
        {
            this.textCache = new();
            return;
        }

        try
        {
            this.textCache =
                JsonConvert.DeserializeObject<ConcurrentDictionary<string, long>>(
                    File.ReadAllText(this.cacheFile.FullName)) ?? new ConcurrentDictionary<string, long>();
        }
        catch (Exception ex)
        {
            this.textCache = new ConcurrentDictionary<string, long>();
            Log.Error(ex, "Couldn't load cached sigs");
        }
    }

    private unsafe class UnsafeCodeReader : CodeReader
    {
        private readonly int length;
        private readonly byte* address;
        private int pos;

        public UnsafeCodeReader(byte* address, int length)
        {
            this.length = length;
            this.address = address;
        }

        public bool CanReadByte => this.pos < this.length;

        public override int ReadByte()
        {
            if (this.pos >= this.length) return -1;
            return *(this.address + this.pos++);
        }
    }
}
