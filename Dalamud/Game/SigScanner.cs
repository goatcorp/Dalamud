using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Serilog;

namespace Dalamud.Game {
    /// <summary>
    /// A SigScanner facilitates searching for memory signatures in a given ProcessModule.
    /// </summary>
    public sealed class SigScanner : IDisposable {
        /// <summary>
        /// Set up the SigScanner.
        /// </summary>
        /// <param name="module">The ProcessModule to be used for scanning</param>
        /// <param name="doCopy">Whether or not to copy the module upon initialization for search operations to use, as to not get disturbed by possible hooks.</param>
        public SigScanner(ProcessModule module, bool doCopy = false) {
            Module = module;
            Is32BitProcess = !Environment.Is64BitProcess;
            IsCopy = doCopy;

            // Limit the search space to .text section.
            SetupSearchSpace(module);

            if (IsCopy)
                SetupCopiedSegments();

            Log.Verbose("Module base: {Address}", TextSectionBase);
            Log.Verbose("Module size: {Size}", TextSectionSize);
        }

        /// <summary>
        /// If the search on this module is performed on a copy.
        /// </summary>
        public bool IsCopy { get; private set; }

        /// <summary>
        /// If the ProcessModule is 32-bit.
        /// </summary>
        public bool Is32BitProcess { get; }

        /// <summary>
        /// The base address of the search area. When copied, this will be the address of the copy.
        /// </summary>
        public IntPtr SearchBase => IsCopy ? this.moduleCopyPtr : Module.BaseAddress;

        /// <summary>
        /// The base address of the .text section search area.
        /// </summary>
        public IntPtr TextSectionBase => new IntPtr(SearchBase.ToInt64() + TextSectionOffset);
        /// <summary>
        /// The offset of the .text section from the base of the module.
        /// </summary>
        public long TextSectionOffset { get; private set; }
        /// <summary>
        /// The size of the text section.
        /// </summary>
        public int TextSectionSize { get; private set; }

        /// <summary>
        /// The base address of the .data section search area.
        /// </summary>
        public IntPtr DataSectionBase => new IntPtr(SearchBase.ToInt64() + DataSectionOffset);
        /// <summary>
        /// The offset of the .data section from the base of the module.
        /// </summary>
        public long DataSectionOffset { get; private set; }
        /// <summary>
        /// The size of the .data section.
        /// </summary>
        public int DataSectionSize { get; private set; }

        /// <summary>
        /// The ProcessModule on which the search is performed.
        /// </summary>
        public ProcessModule Module { get; }

        private IntPtr TextSectionTop => TextSectionBase + TextSectionSize;

        private void SetupSearchSpace(ProcessModule module) {
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
            if (Is32BitProcess) // IMAGE_OPTIONAL_HEADER32
                sectionHeader = optionalHeader + 224;
            else // IMAGE_OPTIONAL_HEADER64
                sectionHeader = optionalHeader + 240;

            // IMAGE_SECTION_HEADER
            var sectionCursor = sectionHeader;
            for (var i = 0; i < numSections; i++) {
                var sectionName = Marshal.ReadInt64(sectionCursor);

                // .text
                switch (sectionName) {
                    case 0x747865742E: // .text
                        TextSectionOffset = Marshal.ReadInt32(sectionCursor, 12);
                        TextSectionSize = Marshal.ReadInt32(sectionCursor, 8);
                        break;
                    case 0x617461642E: // .data
                        DataSectionOffset = Marshal.ReadInt32(sectionCursor, 12);
                        DataSectionSize = Marshal.ReadInt32(sectionCursor, 8);
                        break;
                }

                sectionCursor += 40;
            }
        }

        private IntPtr moduleCopyPtr;
        private long moduleCopyOffset;

        private unsafe void SetupCopiedSegments() {
            Log.Verbose("module copy START");
            // .text
            this.moduleCopyPtr = Marshal.AllocHGlobal(Module.ModuleMemorySize);
            Log.Verbose($"Alloc: {this.moduleCopyPtr.ToInt64():x}");
            Buffer.MemoryCopy(Module.BaseAddress.ToPointer(), this.moduleCopyPtr.ToPointer(), Module.ModuleMemorySize,
                              Module.ModuleMemorySize);

            this.moduleCopyOffset = this.moduleCopyPtr.ToInt64() - Module.BaseAddress.ToInt64();

            Log.Verbose("copy OK!");
        }

        /// <summary>
        /// Free the memory of the copied module search area on object disposal, if applicable.
        /// </summary>
        public void Dispose() {
            Marshal.FreeHGlobal(this.moduleCopyPtr);
        }

        /// <summary>
        /// Scan for a byte signature in the .text section.
        /// </summary>
        /// <param name="signature">The signature.</param>
        /// <returns>The real offset of the found signature.</returns>
        public IntPtr ScanText(string signature) {
            var mBase = IsCopy ? this.moduleCopyPtr : TextSectionBase;

            var scanRet = Scan(mBase, TextSectionSize, signature);

            if (IsCopy)
                scanRet = new IntPtr(scanRet.ToInt64() - this.moduleCopyOffset);
            
            if (Marshal.ReadByte(scanRet) == 0xE8)
                return ReadCallSig(scanRet);

            return scanRet;
        }

        /// <summary>
        /// Helper for ScanText to get the correct address for 
        /// IDA sigs that mark the first CALL location.
        /// </summary>
        /// <param name="sigLocation">The address the CALL sig resolved to.</param>
        /// <returns>The real offset of the signature.</returns>
        private IntPtr ReadCallSig(IntPtr SigLocation)
        {
            int jumpOffset = Marshal.ReadInt32(IntPtr.Add(SigLocation, 1));
            return IntPtr.Add(SigLocation, 5 + jumpOffset);
        }

        /// <summary>
        /// Scan for a byte signature in the .data section.
        /// </summary>
        /// <param name="signature">The signature.</param>
        /// <returns>The real offset of the found signature.</returns>
        public IntPtr ScanData(string signature) {
            var scanRet = Scan(DataSectionBase, DataSectionSize, signature);

            if (IsCopy)
                scanRet = new IntPtr(scanRet.ToInt64() - this.moduleCopyOffset);

            return scanRet;
        }

        /// <summary>
        /// Scan for a byte signature in the whole module search area.
        /// </summary>
        /// <param name="signature">The signature.</param>
        /// <returns>The real offset of the found signature.</returns>
        public IntPtr ScanModule(string signature) {
            var scanRet = Scan(SearchBase, Module.ModuleMemorySize, signature);

            if (IsCopy)
                scanRet = new IntPtr(scanRet.ToInt64() - this.moduleCopyOffset);

            return scanRet;
        }

        public IntPtr Scan(IntPtr baseAddress, int size, string signature) {
            var needle = SigToNeedle(signature);

            unsafe {
                var pCursor = (byte*) baseAddress.ToPointer();
                var pTop = (byte*) (baseAddress + size - needle.Length);
                while (pCursor < pTop) {
                    if (IsMatch(pCursor, needle)) return (IntPtr) pCursor;

                    // Advance an offset
                    pCursor += 1;
                }
            }

            throw new KeyNotFoundException($"Can't find a signature of {signature}");
        }

        public IntPtr ResolveRelativeAddress(IntPtr nextInstAddr, int relOffset) {
            if (Is32BitProcess) throw new NotSupportedException("32 bit is not supported.");

            return nextInstAddr + relOffset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe bool IsMatch(byte* pCursor, byte?[] needle) {
            for (var i = 0; i < needle.Length; i++) {
                var expected = needle[i];
                if (expected == null) continue;

                var actual = *(pCursor + i);
                if (expected != actual) return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte?[] SigToNeedle(string signature) {
            // Strip all whitespaces
            signature = signature.Replace(" ", "");

            if (signature.Length % 2 != 0)
                throw new ArgumentException("Signature without whitespaces must be divisible by two.",
                                            nameof(signature));

            var needleLength = signature.Length / 2;
            var needle = new byte?[needleLength];

            for (var i = 0; i < needleLength; i++) {
                var hexString = signature.Substring(i * 2, 2);
                if (hexString == "??" || hexString == "**") {
                    needle[i] = null;
                    continue;
                }

                needle[i] = byte.Parse(hexString, NumberStyles.AllowHexSpecifier);
            }

            return needle;
        }
    }
}
