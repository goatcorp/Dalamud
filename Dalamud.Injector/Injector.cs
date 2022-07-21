using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Iced.Intel;
using PeNet;
using PeNet.Header.Pe;
using Reloaded.Memory.Buffers;
using Reloaded.Memory.Sources;
using Reloaded.Memory.Utilities;
using Serilog;

using static Dalamud.Injector.NativeFunctions;
using static Iced.Intel.AssemblerRegisters;

namespace Dalamud.Injector
{
    /// <summary>
    /// This class implements injecting into a remote process. It is a highly stripped down version of the
    /// https://github.com/Reloaded-Project injector/assembler implementation due to issues with Lutris and
    /// Wine.
    /// </summary>
    internal sealed class Injector : IDisposable
    {
        private readonly Process targetProcess;
        private readonly bool disposeTargetProcess;
        private readonly ExternalMemory extMemory;
        private readonly CircularBuffer circularBuffer;
        private readonly PrivateMemoryBuffer memoryBuffer;

        private nuint loadLibraryShellPtr;
        private nuint loadLibraryRetPtr;

        private nuint getProcAddressShellPtr;
        private nuint getProcAddressRetPtr;

        /// <summary>
        /// Initializes a new instance of the <see cref="Injector"/> class.
        /// </summary>
        /// <param name="targetProcess">Process to inject.</param>
        /// <param name="disposeTargetProcess">Dispose given process on disposing self.</param>
        public Injector(Process targetProcess, bool disposeTargetProcess = true)
        {
            this.targetProcess = targetProcess;
            this.disposeTargetProcess = disposeTargetProcess;

            this.extMemory = new ExternalMemory(targetProcess);
            this.circularBuffer = new CircularBuffer(4096, this.extMemory);
            this.memoryBuffer = new MemoryBufferHelper(targetProcess).CreatePrivateMemoryBuffer(4096);

            using var kernel32Module = this.GetProcessModule("KERNEL32.DLL");
            var kernel32PeFile = new PeFile(kernel32Module.FileName);
            var kernel32Exports = kernel32PeFile.ExportedFunctions;

            this.SetupLoadLibrary(kernel32Module, kernel32Exports);
            this.SetupGetProcAddress(kernel32Module, kernel32Exports);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="Injector"/> class.
        /// </summary>
        ~Injector() => this.Dispose();

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            if (this.disposeTargetProcess)
                this.targetProcess?.Dispose();
            this.circularBuffer?.Dispose();
            this.memoryBuffer?.Dispose();
        }

        /// <summary>
        /// Load a module by absolute file path.
        /// </summary>
        /// <param name="modulePath">Absolute file path.</param>
        /// <param name="address">Address to the module.</param>
        public void LoadLibrary(string modulePath, out IntPtr address)
        {
            var lpParameter = this.WriteNullTerminatedUnicodeString(modulePath);

            if (lpParameter == 0)
                throw new Exception("Unable to allocate LoadLibraryW parameter");

            this.CallRemoteFunction(this.loadLibraryShellPtr, lpParameter, out var err);
            this.extMemory.Read<IntPtr>(this.loadLibraryRetPtr, out address);
            if (address == IntPtr.Zero)
                throw new Exception($"LoadLibraryW(\"{modulePath}\") failure: {new Win32Exception((int)err).Message} ({err})");
        }

        /// <summary>
        /// Get the address of an exported module function.
        /// </summary>
        /// <param name="module">Module address.</param>
        /// <param name="functionName">Name of the exported method.</param>
        /// <param name="address">Address to the function.</param>
        public void GetFunctionAddress(IntPtr module, string functionName, out nuint address)
        {
            var functionNamePtr = this.WriteNullTerminatedASCIIString(functionName);
            var getProcAddressParams = new GetProcAddressParams(module, functionNamePtr);
            var lpParameter = this.circularBuffer.Add(ref getProcAddressParams);
            if (lpParameter == 0)
                throw new Exception("Unable to allocate GetProcAddress parameter ptr");

            this.CallRemoteFunction(this.getProcAddressShellPtr, lpParameter, out var err);
            this.extMemory.Read<nuint>(this.getProcAddressRetPtr, out address);
            if (address == 0)
                throw new Exception($"GetProcAddress(0x{module:X}, \"{functionName}\") failure: {new Win32Exception((int)err).Message} ({err})");
        }

        /// <summary>
        /// Call a method in a remote process via CreateRemoteThread.
        /// </summary>
        /// <param name="methodAddress">Method address.</param>
        /// <param name="parameterAddress">Parameter address.</param>
        /// <param name="exitCode">Thread exit code.</param>
        public void CallRemoteFunction(nuint methodAddress, nuint parameterAddress, out uint exitCode)
        {
            // Create and initialize a thread at our address and parameter address.
            var threadHandle = CreateRemoteThread(
                this.targetProcess.Handle,
                IntPtr.Zero,
                UIntPtr.Zero,
                methodAddress,
                parameterAddress,
                CreateThreadFlags.RunImmediately,
                out _);

            if (threadHandle == IntPtr.Zero)
                throw new Exception($"CreateRemoteThread failure: {Marshal.GetLastWin32Error()}");

            _ = WaitForSingleObject(threadHandle, uint.MaxValue);

            GetExitCodeThread(threadHandle, out exitCode);

            CloseHandle(threadHandle);
        }

        private void SetupLoadLibrary(ProcessModule kernel32Module, ExportFunction[] kernel32Exports)
        {
            var getLastErrorAddr = kernel32Module.BaseAddress + (int)this.GetExportedFunctionOffset(kernel32Exports, "GetLastError");
            Log.Verbose($"GetLastError:           0x{getLastErrorAddr.ToInt64():X}");

            var functionAddr = kernel32Module.BaseAddress + (int)this.GetExportedFunctionOffset(kernel32Exports, "LoadLibraryW");
            Log.Verbose($"LoadLibraryW:           0x{functionAddr.ToInt64():X}");

            var functionPtr = this.memoryBuffer.Add(ref functionAddr);
            Log.Verbose($"LoadLibraryPtr:         0x{functionPtr:X}");

            if (functionPtr == 0)
                throw new Exception("Unable to allocate LoadLibraryW function ptr");

            var dummy = IntPtr.Zero;
            this.loadLibraryRetPtr = this.memoryBuffer.Add(ref dummy);
            Log.Verbose($"LoadLibraryRetPtr:      0x{this.loadLibraryRetPtr:X}");

            if (this.loadLibraryRetPtr == 0)
                throw new Exception("Unable to allocate LoadLibraryW return value");

            var asm = new Assembler(64);

            asm.sub(rsp, 40);                               // sub rsp, 40                   // Re-align stack to 16 byte boundary + shadow space.
            asm.call(__qword_ptr[__qword_ptr[functionPtr]]);       // call qword [qword func]       // CreateRemoteThread lpParameter with string already in ECX.
            asm.mov(__qword_ptr[__qword_ptr[this.loadLibraryRetPtr]], rax); // mov qword [qword retVal], rax //
            asm.add(rsp, 40);                               // add rsp, 40                   // Re-align stack to 16 byte boundary + shadow space.
            asm.mov(rax, (ulong)getLastErrorAddr);          // mov rax, pfnGetLastError      // Change return address to GetLastError.
            asm.push(rax);                                  // push rax                      //
            asm.ret();                                      // ret                           // Jump to GetLastError.

            var bytes = this.Assemble(asm);
            this.loadLibraryShellPtr = this.memoryBuffer.Add(bytes);
            Log.Verbose($"LoadLibraryShellPtr:    0x{this.loadLibraryShellPtr:X}");

            if (this.loadLibraryShellPtr == 0)
                throw new Exception("Unable to allocate LoadLibraryW shellcode");

            this.extMemory.ChangePermission(this.loadLibraryShellPtr, bytes.Length, Reloaded.Memory.Kernel32.Kernel32.MEM_PROTECTION.PAGE_EXECUTE_READWRITE);

#if DEBUG
            this.extMemory.Read<IntPtr>(functionPtr, out var outFunctionPtr);
            Log.Verbose($"LoadLibraryPtr:         {this.GetResultMarker(outFunctionPtr == functionAddr)}");

            this.extMemory.Read<IntPtr>(this.loadLibraryRetPtr, out var outRetPtr);
            Log.Verbose($"LoadLibraryRet:         {this.GetResultMarker(dummy == outRetPtr)}");

            this.extMemory.ReadRaw(this.loadLibraryShellPtr, out var outBytes, bytes.Length);
            Log.Verbose($"LoadLibraryShellPtr:    {this.GetResultMarker(Enumerable.SequenceEqual(bytes, outBytes))}");
#endif
        }

        private void SetupGetProcAddress(ProcessModule kernel32Module, ExportFunction[] kernel32Exports)
        {
            var getLastErrorAddr = kernel32Module.BaseAddress + (int)this.GetExportedFunctionOffset(kernel32Exports, "GetLastError");
            Log.Verbose($"GetLastError:           0x{getLastErrorAddr.ToInt64():X}");

            var offset = this.GetExportedFunctionOffset(kernel32Exports, "GetProcAddress");
            var functionAddr = kernel32Module.BaseAddress + (int)offset;
            Log.Verbose($"GetProcAddress:         0x{functionAddr.ToInt64():X}");

            var functionPtr = this.memoryBuffer.Add(ref functionAddr);
            Log.Verbose($"GetProcAddressPtr:      0x{functionPtr:X}");

            if (functionPtr == 0)
                throw new Exception("Unable to allocate GetProcAddress function ptr");

            var dummy = IntPtr.Zero;
            this.getProcAddressRetPtr = this.memoryBuffer.Add(ref dummy);
            Log.Verbose($"GetProcAddressRetPtr:   0x{this.loadLibraryRetPtr:X}");

            if (this.getProcAddressRetPtr == 0)
                throw new Exception("Unable to allocate GetProcAddress return value");

            var asm = new Assembler(64);

            asm.sub(rsp, 40);                                // sub rsp, 40                    // Re-align stack to 16 byte boundary +32 shadow space
            asm.mov(rdx, __qword_ptr[__qword_ptr[rcx + 8]]); // mov rdx, qword [qword rcx + 8] // lpProcName
            asm.mov(rcx, __qword_ptr[__qword_ptr[rcx + 0]]); // mov rcx, qword [qword rcx + 0] // hModule
            asm.call(__qword_ptr[__qword_ptr[functionPtr]]);        // call qword [qword func]        //
            asm.mov(__qword_ptr[__qword_ptr[this.getProcAddressRetPtr]], rax);  // mov qword [qword retVal]       //
            asm.add(rsp, 40);                                // add rsp, 40                    // Re-align stack to 16 byte boundary + shadow space.
            asm.mov(rax, (ulong)getLastErrorAddr);           // mov rax, pfnGetLastError       // Change return address to GetLastError.
            asm.push(rax);                                   // push rax                       //
            asm.ret();                                       // ret                            // Jump to GetLastError.

            var bytes = this.Assemble(asm);
            this.getProcAddressShellPtr = this.memoryBuffer.Add(bytes);
            Log.Verbose($"GetProcAddressShellPtr: 0x{this.getProcAddressShellPtr:X}");

            if (this.getProcAddressShellPtr == 0)
                throw new Exception("Unable to allocate GetProcAddress shellcode");

            this.extMemory.ChangePermission(this.getProcAddressShellPtr, bytes.Length, Reloaded.Memory.Kernel32.Kernel32.MEM_PROTECTION.PAGE_EXECUTE_READWRITE);

#if DEBUG
            this.extMemory.Read<IntPtr>(functionPtr, out var outFunctionPtr);
            Log.Verbose($"GetProcAddressPtr:      {this.GetResultMarker(outFunctionPtr == functionAddr)}");

            this.extMemory.Read<IntPtr>(this.loadLibraryRetPtr, out var outRetPtr);
            Log.Verbose($"GetProcAddressRet:      {this.GetResultMarker(dummy == outRetPtr)}");

            this.extMemory.ReadRaw(this.getProcAddressShellPtr, out var outBytes, bytes.Length);
            Log.Verbose($"GetProcAddressShellPtr: {this.GetResultMarker(Enumerable.SequenceEqual(bytes, outBytes))}");
#endif
        }

        private byte[] Assemble(Assembler assembler)
        {
            using var stream = new MemoryStream();
            assembler.Assemble(new StreamCodeWriter(stream), 0);

            stream.Position = 0;
            var reader = new StreamCodeReader(stream);

            int next;
            var bytes = new byte[stream.Length];
            while ((next = reader.ReadByte()) >= 0)
            {
                bytes[stream.Position - 1] = (byte)next;
            }

            return bytes;
        }

        private ProcessModule GetProcessModule(string moduleName)
        {
            var modules = this.targetProcess.Modules;
            for (var i = 0; i < modules.Count; i++)
            {
                var module = modules[i];
                if (module.ModuleName.Equals(moduleName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return module;
                }
            }

            throw new Exception($"Failed to find {moduleName} in target process' modules");
        }

        private uint GetExportedFunctionOffset(ExportFunction[] exportFunctions, string functionName)
        {
            var exportFunction = exportFunctions.FirstOrDefault(func => func.Name == functionName);

            if (exportFunction == default)
                throw new Exception($"Failed to find exported function {functionName} in target module's exports");

            return exportFunction.Address;
        }

        private nuint WriteNullTerminatedASCIIString(string value)
        {
            var bytes = Encoding.ASCII.GetBytes(value + '\0');
            var address = this.circularBuffer.Add(bytes);

            if (address == 0)
                throw new Exception("Unable to write ASCII string to buffer");

#if DEBUG
            this.extMemory.ReadRaw(address, out var outBytes, bytes.Length);
            Log.Verbose($"WriteASCII:             {this.GetResultMarker(Enumerable.SequenceEqual(bytes, outBytes))} 0x{address:X} {value}");
#endif

            return address;
        }

        private nuint WriteNullTerminatedUnicodeString(string value)
        {
            var bytes = Encoding.Unicode.GetBytes(value + '\0');
            var address = this.circularBuffer.Add(bytes);

            if (address == 0)
                throw new Exception("Unable to write Unicode string to buffer");

#if DEBUG
            this.extMemory.ReadRaw(address, out var outBytes, bytes.Length);
            Log.Verbose($"WriteUnicode:           {this.GetResultMarker(Enumerable.SequenceEqual(bytes, outBytes))} 0x{address:X} {value}");
#endif

            return address;
        }

#if DEBUG
        private string GetResultMarker(bool result) => result ? "✅" : "❌";
#endif

        [StructLayout(LayoutKind.Sequential)]
        private struct GetProcAddressParams
        {
            public GetProcAddressParams(IntPtr hModule, nuint lPProcName)
            {
                this.HModule = hModule.ToInt64();
                this.LPProcName = lPProcName;
            }

            public long HModule { get; set; }

            public nuint LPProcName { get; set; }
        }
    }
}
