using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Serilog;

namespace Dalamud.Game {
    public sealed class SigScanner : IDisposable {
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

        public bool IsCopy { get; private set; }

        public bool Is32BitProcess { get; }

        public IntPtr TextSectionBase { get; private set; }
        public int TextSectionSize { get; private set; }

        public IntPtr DataSectionBase { get; private set; }
        public int DataSectionSize { get; private set; }

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
                        TextSectionBase = baseAddress + Marshal.ReadInt32(sectionCursor, 12);
                        TextSectionSize = Marshal.ReadInt32(sectionCursor, 8);
                        break;
                    case 0x617461642E: // .data
                        DataSectionBase = baseAddress + Marshal.ReadInt32(sectionCursor, 12);
                        DataSectionSize = Marshal.ReadInt32(sectionCursor, 8);
                        break;
                }

                sectionCursor += 40;
            }
        }

        private IntPtr textCopyPtr;
        private IntPtr dataCopyPtr;

        private unsafe void SetupCopiedSegments() {
            Log.Verbose("text copy START");
            // .text
            this.textCopyPtr = Marshal.AllocHGlobal(TextSectionSize);
            Log.Verbose($"Alloc: {this.textCopyPtr.ToInt64():x}");
            Buffer.MemoryCopy(TextSectionBase.ToPointer(), this.textCopyPtr.ToPointer(), TextSectionSize,
                              TextSectionSize);

            Log.Verbose("data copy START");
            // .data
            this.dataCopyPtr = Marshal.AllocHGlobal(DataSectionSize);
            Buffer.MemoryCopy(DataSectionBase.ToPointer(), this.dataCopyPtr.ToPointer(), DataSectionSize,
                              DataSectionSize);

            Log.Verbose("copy OK!");
        }

        public void Dispose() {
            Marshal.FreeHGlobal(this.textCopyPtr);
            Marshal.FreeHGlobal(this.dataCopyPtr);
        }

        public IntPtr ScanText(string signature) {
            var mBase = IsCopy ? this.textCopyPtr : TextSectionBase;

            var scanRet = Scan(mBase, TextSectionSize, signature);

            return IsCopy
                       ? (new IntPtr(scanRet.ToInt64() - (this.textCopyPtr.ToInt64() - TextSectionBase.ToInt64())))
                       : scanRet;
        }

        public IntPtr ScanData(string signature) {
            var mBase = IsCopy ? this.dataCopyPtr : DataSectionBase;

            var scanRet = Scan(DataSectionBase, DataSectionSize, signature);

            return IsCopy
                       ? (new IntPtr(scanRet.ToInt64() - (this.textCopyPtr.ToInt64() - TextSectionBase.ToInt64())))
                       : scanRet;
        }

        public IntPtr ScanModule(string signature) {
            // TODO: This does not respect the copy flag.
            return Scan(Module.BaseAddress, Module.ModuleMemorySize, signature);
        }

        public IntPtr Scan(IntPtr baseAddress, int size, string signature) {
            Log.Verbose($"Scan at {baseAddress.ToInt64():x} with {size:x} for {signature}");
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
