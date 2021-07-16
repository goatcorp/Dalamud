using System;
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
        private readonly ExternalMemory extMemory;
        private readonly CircularBuffer circularBuffer;
        private readonly PrivateMemoryBuffer privateBuffer;

        private IntPtr loadLibraryShellPtr;
        private IntPtr loadLibraryRetPtr;

        private IntPtr getProcAddressShellPtr;
        private IntPtr getProcAddressRetPtr;

        /// <summary>
        /// Initializes a new instance of the <see cref="Injector"/> class.
        /// </summary>
        /// <param name="targetProcess">Process to inject.</param>
        public Injector(Process targetProcess)
        {
            this.targetProcess = targetProcess;

            this.extMemory = new ExternalMemory(targetProcess);
            this.circularBuffer = new CircularBuffer(4096, this.extMemory);
            this.privateBuffer = new MemoryBufferHelper(targetProcess).CreatePrivateMemoryBuffer(4096);

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

            this.targetProcess?.Dispose();
            this.circularBuffer?.Dispose();
            this.privateBuffer?.Dispose();
        }

        /// <summary>
        /// Load a module by absolute file path.
        /// </summary>
        /// <param name="modulePath">Absolute file path.</param>
        /// <param name="address">Address to the module.</param>
        public void LoadLibrary(string modulePath, out IntPtr address)
        {
            var lpParameter = this.WriteNullTerminatedUnicodeString(modulePath);

            if (lpParameter == IntPtr.Zero)
                throw new Exception("Unable to allocate LoadLibraryW parameter");

            var threadHandle = CreateRemoteThread(
                this.targetProcess.Handle,
                IntPtr.Zero,
                UIntPtr.Zero,
                this.loadLibraryShellPtr,
                lpParameter,
                CreateThreadFlags.RunImmediately,
                out _);

            _ = WaitForSingleObject(threadHandle, uint.MaxValue);

            this.extMemory.Read(this.loadLibraryRetPtr, out address);

            if (address == IntPtr.Zero)
                throw new Exception($"Error calling LoadLibraryW with {modulePath}");
        }

        /// <summary>
        /// Get the address of an exported module function.
        /// </summary>
        /// <param name="module">Module address.</param>
        /// <param name="functionName">Name of the exported method.</param>
        /// <param name="address">Address to the function.</param>
        public void GetFunctionAddress(IntPtr module, string functionName, out IntPtr address)
        {
            var getProcAddressParams = new GetProcAddressParams(module, this.WriteNullTerminatedASCIIString(functionName));
            var lpParameter = this.circularBuffer.Add(ref getProcAddressParams);

            if (lpParameter == IntPtr.Zero)
                throw new Exception("Unable to allocate GetProcAddress parameter ptr");

            var threadHandle = CreateRemoteThread(
                this.targetProcess.Handle,
                IntPtr.Zero,
                UIntPtr.Zero,
                this.getProcAddressShellPtr,
                lpParameter,
                CreateThreadFlags.RunImmediately,
                out _);

            _ = WaitForSingleObject(threadHandle, uint.MaxValue);

            this.extMemory.Read(this.getProcAddressRetPtr, out address);

            if (address == IntPtr.Zero)
                throw new Exception($"Error calling GetProcAddress with {functionName}");
        }

        /// <summary>
        /// Call a method in a remote process via CreateRemoteThread.
        /// </summary>
        /// <param name="methodAddress">Method address.</param>
        /// <param name="parameterAddress">Parameter address.</param>
        /// <param name="exitCode">Thread exit code.</param>
        public void CallRemoteFunction(IntPtr methodAddress, IntPtr parameterAddress, out uint exitCode)
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

            _ = WaitForSingleObject(threadHandle, uint.MaxValue);

            GetExitCodeThread(threadHandle, out exitCode);
        }

        private void SetupLoadLibrary(ProcessModule kernel32Module, ExportFunction[] kernel32Exports)
        {
            var offset = this.GetExportedFunctionOffset(kernel32Exports, "LoadLibraryW");
            var functionAddr = kernel32Module.BaseAddress + (int)offset;
            var functionPtr = this.privateBuffer.Add(ref functionAddr);

            if (functionPtr == IntPtr.Zero)
                throw new Exception("Unable to allocate LoadLibraryW function ptr");

            var dummy = 0L;
            this.loadLibraryRetPtr = this.privateBuffer.Add(ref dummy);

            if (this.loadLibraryRetPtr == IntPtr.Zero)
                throw new Exception("Unable to allocate LoadLibraryW return value");

            var func = functionPtr.ToInt64();
            var retVal = this.loadLibraryRetPtr.ToInt64();

            var asm = new Assembler(64);

            asm.sub(rsp, 40);                               // sub rsp, 40                   // Re-align stack to 16 byte boundary + shadow space.
            asm.call(__qword_ptr[__qword_ptr[func]]);       // call qword [qword func]       // CreateRemoteThread lpParameter with string already in ECX.
            asm.mov(__qword_ptr[__qword_ptr[retVal]], rax); // mov qword [qword retVal], rax //
            asm.add(rsp, 40);                               // add rsp, 40                   // Re-align stack to 16 byte boundary + shadow space.
            asm.ret();                                      // ret                           // Restore stack ptr. (Callee cleanup)

            var bytes = this.Assemble(asm);
            this.loadLibraryShellPtr = this.privateBuffer.Add(bytes);

            if (this.loadLibraryShellPtr == IntPtr.Zero)
                throw new Exception("Unable to allocate LoadLibraryW shellcode");
        }

        private void SetupGetProcAddress(ProcessModule kernel32Module, ExportFunction[] kernel32Exports)
        {
            var offset = this.GetExportedFunctionOffset(kernel32Exports, "GetProcAddress");
            var functionAddr = kernel32Module.BaseAddress + (int)offset;
            var functionPtr = this.privateBuffer.Add(ref functionAddr);

            if (functionPtr == IntPtr.Zero)
                throw new Exception("Unable to allocate GetProcAddress function ptr");

            var dummy = 0L;
            this.getProcAddressRetPtr = this.privateBuffer.Add(ref dummy);

            if (this.getProcAddressRetPtr == IntPtr.Zero)
                throw new Exception("Unable to allocate GetProcAddress return value");

            var func = functionPtr.ToInt64();
            var retVal = this.getProcAddressRetPtr.ToInt64();

            var asm = new Assembler(64);

            asm.sub(rsp, 40);                                // sub rsp, 40                    // Re-align stack to 16 byte boundary +32 shadow space
            asm.mov(rdx, __qword_ptr[__qword_ptr[rcx + 8]]); // mov rdx, qword [qword rcx + 8] // lpProcName
            asm.mov(rcx, __qword_ptr[__qword_ptr[rcx + 0]]); // mov rcx, qword [qword rcx + 0] // hModule
            asm.call(__qword_ptr[__qword_ptr[func]]);        // call qword [qword func]        //
            asm.mov(__qword_ptr[__qword_ptr[retVal]], rax);  // mov qword [qword retVal]       //
            asm.add(rsp, 40);                                // add rsp, 40                    // Re-align stack to 16 byte boundary + shadow space.
            asm.ret();                                       // ret                            // Restore stack ptr. (Callee cleanup)

            var bytes = this.Assemble(asm);
            this.getProcAddressShellPtr = this.privateBuffer.Add(bytes);

            if (this.getProcAddressShellPtr == IntPtr.Zero)
                throw new Exception("Unable to allocate GetProcAddress shellcode");
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

        private IntPtr WriteNullTerminatedASCIIString(string libraryPath)
        {
            var libraryNameBytes = Encoding.ASCII.GetBytes(libraryPath + '\0');
            var value = this.circularBuffer.Add(libraryNameBytes);

            if (value == IntPtr.Zero)
                throw new Exception("Unable to write ASCII string to buffer");

            return value;
        }

        private IntPtr WriteNullTerminatedUnicodeString(string libraryPath)
        {
            var libraryNameBytes = Encoding.Unicode.GetBytes(libraryPath + '\0');
            var value = this.circularBuffer.Add(libraryNameBytes);

            if (value == IntPtr.Zero)
                throw new Exception("Unable to write Unicode string to buffer");

            return value;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct GetProcAddressParams
        {
            public GetProcAddressParams(IntPtr hModule, IntPtr lPProcName)
            {
                this.HModule = hModule.ToInt64();
                this.LPProcName = lPProcName.ToInt64();
            }

            public long HModule { get; set; }

            public long LPProcName { get; set; }
        }
    }
}
