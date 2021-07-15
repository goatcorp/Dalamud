using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Serilog;

namespace Dalamud.Game
{
    /// <summary>
    /// A SigScanner facilitates searching for memory signatures in a given ProcessModule.
    /// </summary>
    public sealed class SigScanner : IDisposable
    {
        private IntPtr moduleCopyPtr;
        private long moduleCopyOffset;

        /// <summary>
        /// Initializes a new instance of the <see cref="SigScanner"/> class.
        /// </summary>
        /// <param name="module">The ProcessModule to be used for scanning.</param>
        /// <param name="doCopy">Whether or not to copy the module upon initialization for search operations to use, as to not get disturbed by possible hooks.</param>
        public SigScanner(ProcessModule module, bool doCopy = false)
        {
            this.Module = module;
            this.Is32BitProcess = !Environment.Is64BitProcess;
            this.IsCopy = doCopy;

            // Limit the search space to .text section.
            this.SetupSearchSpace(module);

            if (this.IsCopy)
                this.SetupCopiedSegments();

            Log.Verbose($"Module base: 0x{this.TextSectionBase.ToInt64():X}");
            Log.Verbose($"Module size: 0x{this.TextSectionSize:X}");
        }

        /// <summary>
        /// Gets a value indicating whether or not the search on this module is performed on a copy.
        /// </summary>
        public bool IsCopy { get; }

        /// <summary>
        /// Gets a value indicating whether or not the ProcessModule is 32-bit.
        /// </summary>
        public bool Is32BitProcess { get; }

        /// <summary>
        /// Gets the base address of the search area. When copied, this will be the address of the copy.
        /// </summary>
        public IntPtr SearchBase => this.IsCopy ? this.moduleCopyPtr : this.Module.BaseAddress;

        /// <summary>
        /// Gets the base address of the .text section search area.
        /// </summary>
        public IntPtr TextSectionBase => new(this.SearchBase.ToInt64() + this.TextSectionOffset);

        /// <summary>
        /// Gets the offset of the .text section from the base of the module.
        /// </summary>
        public long TextSectionOffset { get; private set; }

        /// <summary>
        /// Gets the size of the text section.
        /// </summary>
        public int TextSectionSize { get; private set; }

        /// <summary>
        /// Gets the base address of the .data section search area.
        /// </summary>
        public IntPtr DataSectionBase => new(this.SearchBase.ToInt64() + this.DataSectionOffset);

        /// <summary>
        /// Gets the offset of the .data section from the base of the module.
        /// </summary>
        public long DataSectionOffset { get; private set; }

        /// <summary>
        /// Gets the size of the .data section.
        /// </summary>
        public int DataSectionSize { get; private set; }

        /// <summary>
        /// Gets the base address of the .rdata section search area.
        /// </summary>
        public IntPtr RDataSectionBase => new(this.SearchBase.ToInt64() + this.RDataSectionOffset);

        /// <summary>
        /// Gets the offset of the .rdata section from the base of the module.
        /// </summary>
        public long RDataSectionOffset { get; private set; }

        /// <summary>
        /// Gets the size of the .rdata section.
        /// </summary>
        public int RDataSectionSize { get; private set; }

        /// <summary>
        /// Gets the ProcessModule on which the search is performed.
        /// </summary>
        public ProcessModule Module { get; }

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
            var (needle, mask) = ParseSignature(signature);
            var index = IndexOf(baseAddress, size, needle, mask);
            if (index < 0)
                throw new KeyNotFoundException($"Can't find a signature of {signature}");
            return baseAddress + index;
        }

        /// <summary>
        /// Scan for a .data address using a .text function.
        /// This is intended to be used with IDA sigs.
        /// Place your cursor on the line calling a static address, and create and IDA sig.
        /// </summary>
        /// <param name="signature">The signature of the function using the data.</param>
        /// <param name="offset">The offset from function start of the instruction using the data.</param>
        /// <returns>An IntPtr to the static memory location.</returns>
        public IntPtr GetStaticAddressFromSig(string signature, int offset = 0)
        {
            var instrAddr = this.ScanText(signature);
            instrAddr = IntPtr.Add(instrAddr, offset);
            var bAddr = (long)this.Module.BaseAddress;
            long num;

            do
            {
                instrAddr = IntPtr.Add(instrAddr, 1);
                num = Marshal.ReadInt32(instrAddr) + (long)instrAddr + 4 - bAddr;
            }
            while (!(num >= this.DataSectionOffset && num <= this.DataSectionOffset + this.DataSectionSize)
                   && !(num >= this.RDataSectionOffset && num <= this.RDataSectionOffset + this.RDataSectionSize));

            return IntPtr.Add(instrAddr, Marshal.ReadInt32(instrAddr) + 4);
        }

        /// <summary>
        /// Scan for a byte signature in the .data section.
        /// </summary>
        /// <param name="signature">The signature.</param>
        /// <returns>The real offset of the found signature.</returns>
        public IntPtr ScanData(string signature)
        {
            var scanRet = Scan(this.DataSectionBase, this.DataSectionSize, signature);

            if (this.IsCopy)
                scanRet = new IntPtr(scanRet.ToInt64() - this.moduleCopyOffset);

            return scanRet;
        }

        /// <summary>
        /// Scan for a byte signature in the whole module search area.
        /// </summary>
        /// <param name="signature">The signature.</param>
        /// <returns>The real offset of the found signature.</returns>
        public IntPtr ScanModule(string signature)
        {
            var scanRet = Scan(this.SearchBase, this.Module.ModuleMemorySize, signature);

            if (this.IsCopy)
                scanRet = new IntPtr(scanRet.ToInt64() - this.moduleCopyOffset);

            return scanRet;
        }

        /// <summary>
        /// Resolve a RVA address.
        /// </summary>
        /// <param name="nextInstAddr">The address of the next instruction.</param>
        /// <param name="relOffset">The relative offset.</param>
        /// <returns>The calculated offset.</returns>
        public IntPtr ResolveRelativeAddress(IntPtr nextInstAddr, int relOffset)
        {
            if (this.Is32BitProcess) throw new NotSupportedException("32 bit is not supported.");
            return nextInstAddr + relOffset;
        }

        /// <summary>
        /// Scan for a byte signature in the .text section.
        /// </summary>
        /// <param name="signature">The signature.</param>
        /// <returns>The real offset of the found signature.</returns>
        public IntPtr ScanText(string signature)
        {
            var mBase = this.IsCopy ? this.moduleCopyPtr : this.TextSectionBase;

            var scanRet = Scan(mBase, this.TextSectionSize, signature);

            if (this.IsCopy)
                scanRet = new IntPtr(scanRet.ToInt64() - this.moduleCopyOffset);

            var insnByte = Marshal.ReadByte(scanRet);

            if (insnByte == 0xE8 || insnByte == 0xE9)
                return ReadJmpCallSig(scanRet);

            return scanRet;
        }

        /// <summary>
        /// Free the memory of the copied module search area on object disposal, if applicable.
        /// </summary>
        public void Dispose()
        {
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

        private static (byte[] Needle, bool[] Mask) ParseSignature(string signature)
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

            return (needle, mask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int IndexOf(IntPtr bufferPtr, int bufferLength, byte[] needle, bool[] mask)
        {
            if (needle.Length > bufferLength) return -1;
            var badShift = BuildBadCharTable(needle, mask);
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

            this.moduleCopyOffset = this.moduleCopyPtr.ToInt64() - this.Module.BaseAddress.ToInt64();
        }
    }
}
