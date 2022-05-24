using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

using Iced.Intel;
using PeNet;
using PeNet.Header.Pe;
using Reloaded.Memory.Buffers;
using Reloaded.Memory.Sources;
using Serilog;

using static Dalamud.Injector.NativeFunctions;
using static Iced.Intel.AssemblerRegisters;

namespace Dalamud.Injector;

/// <summary>
/// This class implements rewriting the original entrypoint of a remote process.
/// </summary>
internal sealed class RewriteOriginalEntrypoint : IDisposable
{
    private readonly Process targetProcess;
    private readonly bool disposeTargetProcess;

    private readonly ExternalMemory extMemory;
    private readonly MemoryBuffer memoryBuffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="RewriteOriginalEntrypoint"/> class.
    /// </summary>
    /// <param name="targetProcess">Process to inject.</param>
    /// <param name="disposeTargetProcess">Dispose given process on disposing self.</param>
    public RewriteOriginalEntrypoint(Process targetProcess, bool disposeTargetProcess = true)
    {
        this.targetProcess = targetProcess;
        this.disposeTargetProcess = disposeTargetProcess;

        this.extMemory = new ExternalMemory(targetProcess);
        this.memoryBuffer = new MemoryBufferHelper(targetProcess).CreateMemoryBuffer(2048);
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="RewriteOriginalEntrypoint"/> class.
    /// </summary>
    ~RewriteOriginalEntrypoint() => this.Dispose();

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (this.disposeTargetProcess)
            this.targetProcess?.Dispose();
    }

    /// <summary>
    /// Rewrite the entrypoint.
    /// </summary>
    /// <param name="gamePath">Game path, MainModule doesn't work with the process suspended.</param>
    /// <param name="startInfo">Serialized startInfo.</param>
    public void Rewrite(string gamePath, string startInfo)
    {
        var hProcess = this.targetProcess.Handle;

        // Get the original entrypoint address
        var (_, oepPtr) = this.GetMappedImageBaseAndEntrypoint(hProcess, gamePath);

        // Get addresses to Kernel32 functions, this is loaded in the same place across all processes
        using var kernel32Module = Util.GetProcessModule(Process.GetCurrentProcess(), "KERNEL32.DLL");
        var kernel32Exports = Util.GetExportedFunctions(kernel32Module.FileName);
        var loadLibraryPtr = Util.GetExportedFunctionAddress(kernel32Module, kernel32Exports, "LoadLibraryW").ToInt64();
        var getProcAddressPtr = Util.GetExportedFunctionAddress(kernel32Module, kernel32Exports, "GetProcAddress").ToInt64();

        // I'm not a fan of this, but its better than subtracting struct sizes and checking alignment
        var allocation = (IntPtr)this.memoryBuffer.GetType()
            .GetProperty("AllocationAddress", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(this.memoryBuffer);

        // ======================================================
        // Generate and write the byte arrays for each necessary field/parameter

        var nethostPath = Path.GetFullPath("nethost.dll");
        var nethostPathBytes = Encoding.Unicode.GetBytes(nethostPath + '\0');
        var nethostPathPtr = this.memoryBuffer.Add(nethostPathBytes).ToInt64();

        var dalamudBootPath = Path.GetFullPath("Dalamud.Boot.dll");
        var dalamudBootPathBytes = Encoding.Unicode.GetBytes(dalamudBootPath + '\0');
        var dalamudBootPathPtr = this.memoryBuffer.Add(dalamudBootPathBytes).ToInt64();

        var newEpName = "RewrittenEntryPoint";
        var newEpNameBytes = Encoding.UTF8.GetBytes(newEpName + '\0');
        var newEpNamePtr = this.memoryBuffer.Add(newEpNameBytes).ToInt64();

        var startInfoBytes = Encoding.UTF8.GetBytes(startInfo + '\0');
        var startInfoPtr = this.memoryBuffer.Add(startInfoBytes);

        this.extMemory.ReadRaw(oepPtr, out var oepBackupBytes, 12);
        var oepBackupPtr = this.memoryBuffer.Add(oepBackupBytes);

        // ======================================================
        // Build the entrypoint param

        var newEpParam = new RewrittenEntrypointParameters()
        {
            Allocation = allocation,
            Entrypoint = oepPtr,
            EntrypointBackup = oepBackupPtr,
            EntrypointLength = oepBackupBytes.Length,
            StartInfo = startInfoPtr,
            MainThread = IntPtr.Zero,
            MainThreadContinue = IntPtr.Zero,
        };
        var newEpParamPtr = this.memoryBuffer.Add(ref newEpParam, true).ToInt64();

        // ======================================================
        // Build the trampoline

        var asm = new Assembler(64);

        // stackalloc
        asm.sub(rsp, 0x80);

        // LoadLibraryW("nethost")
        asm.mov(rcx, nethostPathPtr);     // lpLibFileName
        asm.mov(rdi, loadLibraryPtr);     // &LoadLibrary
        asm.call(rdi);

        // LoadlibraryW("Dalamud.Boot")
        asm.mov(rcx, dalamudBootPathPtr); // lpLibFileName
        asm.mov(rdi, loadLibraryPtr);     // &LoadLibrary
        asm.call(rdi);

        // GetProcAddress("RewrittenEntrypoint")
        asm.mov(rcx, rax);                // Dalamud.Boot handle
        asm.mov(rdx, newEpNamePtr);       // lpProcName
        asm.mov(rdi, getProcAddressPtr);  // &GetProcAddress
        asm.call(rdi);

        // stackrelease
        asm.add(rsp, 0x80);

        // handle GetProcAddress return
        asm.mov(rdi, rax); // GetProcAddress -> rdi
        asm.pop(rax);

        asm.sub(rax, 0xC); // sizeof(asmThunk)

        // InjectEntrypoint(rewrittenParam)
        asm.mov(rcx, newEpParamPtr); // &RewrittenEntrypointParam
        asm.push(rax);
        asm.jmp(rdi);

        // Save the trampoline into the buffer
        var trampolineBytes = asm.AssembleBytes();
        var trampolinePtr = this.memoryBuffer.Add(trampolineBytes).ToInt64();

        // ======================================================
        // Build the thunk

        asm.Reset();

        asm.mov(rdi, trampolinePtr); // &trampoline
        asm.call(rdi);

        // Overwrite the original entrypoint with the new thunk
        var thunkBytes = asm.AssembleBytes();
        this.extMemory.WriteRaw(oepPtr, thunkBytes);

        // ======================================================
        // Finished
    }

    /// <summary>
    /// Get the entrypoint of the mapped file/image corresponding to the path given in the target process.
    /// The strategy is to wait by cycling through the modules, until kernel32 is loaded.
    /// </summary>
    /// <param name="hProcess">Process handle.</param>
    /// <param name="gamePath">Game path, MainModule doesn't work with the process suspended.</param>
    /// <returns>Image base and entrypoint of the memory mapped file in the target process.</returns>
    private (IntPtr ImageBase, IntPtr EntryPoint) GetMappedImageBaseAndEntrypoint(IntPtr hProcess, string gamePath)
    {
        using var stream = new FileStream(gamePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var exeFile = new PeFile(stream);

        var exeDosHeader = exeFile.ImageDosHeader;
        if (exeDosHeader.E_magic != 0x5A4D) // IMAGE_DOS_SIGNATURE "MZ"
            throw new Exception("Game executable is corrupt (DOS header)");

        var exeNtHeader = exeFile.ImageNtHeaders;
        if (exeNtHeader.Signature != 0x00004550) // IMAGE_NT_SIGNATURE "PE00"
            throw new Exception("Game executable is corrupt (NT header)");

        var exeSectionHeaders = exeFile.ImageSectionHeaders;

        // var mbi = default(MEMORY_BASIC_INFORMATION);
        var mbiSize = Marshal.SizeOf<MEMORY_BASIC_INFORMATION>();
        var dosHeaderSize = Marshal.SizeOf<IMAGE_DOS_HEADER>();
        var ntHeaderSize = Marshal.SizeOf<IMAGE_NT_HEADERS64>();
        var imageSectionHeadeSize = Marshal.SizeOf<IMAGE_SECTION_HEADER>();

        for (
            var mbi = default(MEMORY_BASIC_INFORMATION);
            VirtualQueryEx(hProcess, mbi.BaseAddress, out mbi, mbiSize) != 0;
            mbi.BaseAddress = new IntPtr((long)mbi.BaseAddress + (long)mbi.RegionSize))
        {
            if ((mbi.State & MEM_COMMIT) == 0 || mbi.Type != MEM_IMAGE)
            {
                Log.Verbose("RewriteOEP: State/type check failed, continuing");
                continue;
            }

            if (!ReadProcessMemoryT(hProcess, mbi.BaseAddress, out IMAGE_DOS_HEADER memDosHeader, dosHeaderSize, out var lpNumberOfBytesRead))
            {
                Log.Verbose("RewriteOEP: IMAGE_DOS_HEADER could not be read, continuing");
                continue;
            }

            if (exeDosHeader.E_magic != memDosHeader.e_magic)
            {
                Log.Verbose($"RewriteOEP: IMAGE_DOS_HEADER.E_magic failed, continuing");
                continue;
            }

            var memNtHeaderAddress = new IntPtr((long)mbi.BaseAddress + memDosHeader.e_lfanew);
            if (!ReadProcessMemoryT(hProcess, memNtHeaderAddress, out IMAGE_NT_HEADERS64 memNtHeaders, ntHeaderSize, out lpNumberOfBytesRead))
            {
                Log.Verbose("RewriteOEP: IMAGE_NT_HEADERS could not be read, continuing");
                continue;
            }

            if (exeNtHeader.Signature != memNtHeaders.Signature)
            {
                Log.Verbose("RewriteOEP: IMAGE_NT_HEADERS.Signature failed, continuing");
                continue;
            }

            if (exeNtHeader.FileHeader.TimeDateStamp != memNtHeaders.FileHeader.TimeDateStamp)
            {
                Log.Verbose("RewriteOEP: IMAGE_NT_HEADERS.TimeDateStamp failed, continuing");
                continue;
            }

            if (exeNtHeader.FileHeader.SizeOfOptionalHeader != memNtHeaders.FileHeader.SizeOfOptionalHeader)
            {
                Log.Verbose("RewriteOEP: IMAGE_NT_HEADERS.SizeOfOptionalHeader failed, continuing");
                continue;
            }

            if (exeNtHeader.FileHeader.NumberOfSections != memNtHeaders.FileHeader.NumberOfSections)
            {
                Log.Verbose("RewriteOEP: IMAGE_NT_HEADERS.NumberOfSections check, continuing");
                continue;
            }

            if (exeNtHeader.OptionalHeader.SizeOfImage != memNtHeaders.OptionalHeader.SizeOfImage)
            {
                Log.Verbose("RewriteOEP: IMAGE_NT_HEADERS.OptionalHeader.SizeOfImage failed, continuing");
                continue;
            }

            if (exeNtHeader.OptionalHeader.CheckSum != memNtHeaders.OptionalHeader.CheckSum)
            {
                Log.Verbose("RewriteOEP: IMAGE_NT_HEADERS.OptionalHeader.CheckSum failed, continuing");
                continue;
            }

            var numSections = memNtHeaders.FileHeader.NumberOfSections;
            var memSectionHeadersAddress = new IntPtr((long)memNtHeaderAddress + ntHeaderSize);

            if (exeSectionHeaders.Length != numSections)
            {
                Log.Verbose("RewriteOEP: IMAGE_SECTION_HEADER length mismatch, continuing");
                continue;
            }

            var sectionHeaderGood = true;
            for (var i = 0; i < numSections; i++)
            {
                if (!ReadProcessMemoryT(hProcess, memSectionHeadersAddress, out IMAGE_SECTION_HEADER memSectionHeader, imageSectionHeadeSize, out lpNumberOfBytesRead))
                {
                    Log.Verbose($"RewriteOEP: IMAGE_SECTION_HEADER[{i}] could not be read, continuing");
                    sectionHeaderGood = false;
                    break;
                }

                var exeSectionHeader = exeSectionHeaders[i];
                if (!this.CompareImageSectionHeader(exeSectionHeader, memSectionHeader))
                {
                    Log.Verbose($"RewriteOEP: IMAGE_SECTION_HEADER[{i}] content mismatch, continuing");
                    sectionHeaderGood = false;
                    break;
                }

                memSectionHeadersAddress = new IntPtr((long)memSectionHeadersAddress + imageSectionHeadeSize);
            }

            if (!sectionHeaderGood)
                continue;

            var imageBase = mbi.AllocationBase;
            var entrypoint = new IntPtr((long)imageBase + memNtHeaders.OptionalHeader.AddressOfEntryPoint);
            Log.Debug($"RewriteOEP: Image base is 0x{imageBase:X}");
            Log.Debug($"RewriteOEP: Entrypoint is 0x{entrypoint:X}");

            return (imageBase, entrypoint);
        }

        throw new Exception("Could not get imagebase/entrypoint");
    }

    /// <summary>
    /// Compare a IMAGE_SECTION_HEADER from disk to memory.
    /// </summary>
    /// <param name="exe">Disk header.</param>
    /// <param name="mem">Memory header.</param>
    /// <returns>A value indicating whether the data is equal.</returns>
    private bool CompareImageSectionHeader(ImageSectionHeader exe, IMAGE_SECTION_HEADER mem)
    {
        return
            exe.VirtualSize == mem.VirtualSize &&
            exe.VirtualAddress == mem.VirtualAddress &&
            exe.SizeOfRawData == mem.SizeOfRawData &&
            exe.PointerToRawData == mem.PointerToRawData &&
            exe.PointerToRelocations == mem.PointerToRelocations &&
            exe.PointerToLinenumbers == mem.PointerToLinenumbers &&
            exe.NumberOfRelocations == mem.NumberOfRelocations &&
            exe.NumberOfLinenumbers == mem.NumberOfLinenumbers &&
            (uint)exe.Characteristics == mem.Characteristics;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RewrittenEntrypointParameters
    {
        public IntPtr Allocation;         // *void
        public IntPtr Entrypoint;         // *void
        public IntPtr EntrypointBackup;   // *byte[]
        public long EntrypointLength;     // size_t
        public IntPtr StartInfo;          // LPStr
        public IntPtr MainThread;         // HANDLE
        public IntPtr MainThreadContinue; // HANDLE
    }
}
