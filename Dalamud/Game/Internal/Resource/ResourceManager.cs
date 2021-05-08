using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using Dalamud.Hooking;
using Serilog;

namespace Dalamud.Game.Internal.File
{
    public class ResourceManager
    {
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr GetResourceAsyncDelegate(IntPtr manager, IntPtr a2, IntPtr a3, IntPtr a4, IntPtr a5, IntPtr a6, byte a7);

        private readonly Hook<GetResourceAsyncDelegate> getResourceAsyncHook;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr GetResourceSyncDelegate(IntPtr manager, IntPtr a2, IntPtr a3, IntPtr a4, IntPtr a5, IntPtr a6);

        private readonly Hook<GetResourceSyncDelegate> getResourceSyncHook;

        private ResourceManagerAddressResolver Address { get; }

        private readonly Dalamud dalamud;

        class ResourceHandleHookInfo
        {
            public string Path { get; set; }

            public Stream DetourFile { get; set; }
        }

        private Dictionary<IntPtr, ResourceHandleHookInfo> resourceHookMap = new();

        public ResourceManager(Dalamud dalamud, SigScanner scanner)
        {
            this.dalamud = dalamud;
            this.Address = new ResourceManagerAddressResolver();
            this.Address.Setup(scanner);

            Log.Verbose("=====  R E S O U R C E   M A N A G E R  =====");
            Log.Verbose("GetResourceAsync address {GetResourceAsync}", this.Address.GetResourceAsync);
            Log.Verbose("GetResourceSync address {GetResourceSync}", this.Address.GetResourceSync);

            this.getResourceAsyncHook = new Hook<GetResourceAsyncDelegate>(this.Address.GetResourceAsync, new GetResourceAsyncDelegate(this.GetResourceAsyncDetour), this);

            this.getResourceSyncHook = new Hook<GetResourceSyncDelegate>(this.Address.GetResourceSync, new GetResourceSyncDelegate(this.GetResourceSyncDetour), this);
        }

        public void Enable()
        {
            this.getResourceAsyncHook.Enable();
            this.getResourceSyncHook.Enable();
        }

        public void Dispose()
        {
            this.getResourceAsyncHook.Dispose();
            this.getResourceSyncHook.Dispose();
        }

        private IntPtr GetResourceAsyncDetour(IntPtr manager, IntPtr a2, IntPtr a3, IntPtr a4, IntPtr a5, IntPtr a6, byte a7)
        {
            try
            {
                var path = Marshal.PtrToStringAnsi(a5);

                var resourceHandle = this.getResourceAsyncHook.Original(manager, a2, a3, a4, IntPtr.Zero, a6, a7);
                // var resourceHandle = IntPtr.Zero;

                Log.Verbose("GetResourceAsync CALL - this:{0} a2:{1} a3:{2} a4:{3} a5:{4} a6:{5} a7:{6} => RET:{7}", manager, a2, a3, a4, a5, a6, a7, resourceHandle);

                Log.Verbose($"->{path}");

                this.HandleGetResourceHookAcquire(resourceHandle, path);

                return resourceHandle;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception on ReadResourceAsync hook.");

                return this.getResourceAsyncHook.Original(manager, a2, a3, a4, a5, a6, a7);
            }
        }

        private void DumpMem(IntPtr address, int len = 512)
        {
            if (address == IntPtr.Zero)
                return;

            var data = new byte[len];
            Marshal.Copy(address, data, 0, len);

            Log.Verbose($"MEMDMP at {address.ToInt64():X} for {len:X}\n{Util.ByteArrayToHex(data)}");
        }

        private IntPtr GetResourceSyncDetour(IntPtr manager, IntPtr a2, IntPtr a3, IntPtr a4, IntPtr a5, IntPtr a6)
        {
            try
            {
                var resourceHandle = this.getResourceSyncHook.Original(manager, a2, a3, a4, a5, a6);

                Log.Verbose("GetResourceSync CALL - this:{0} a2:{1} a3:{2} a4:{3} a5:{4} a6:{5} => RET:{6}", manager, a2, a3, a4, a5, a6, resourceHandle);

                var path = Marshal.PtrToStringAnsi(a5);

                Log.Verbose($"->{path}");

                this.HandleGetResourceHookAcquire(resourceHandle, path);

                return resourceHandle;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception on ReadResourceSync hook.");

                return this.getResourceSyncHook.Original(manager, a2, a3, a4, a5, a6);
            }
        }

        private void HandleGetResourceHookAcquire(IntPtr handlePtr, string path)
        {
            if (FilePathHasInvalidChars(path))
                return;

            if (this.resourceHookMap.ContainsKey(handlePtr))
            {
                Log.Verbose($"-> Handle {handlePtr.ToInt64():X}({path}) was cached!");
                return;
            }

            var hookInfo = new ResourceHandleHookInfo
            {
                Path = path,
            };

            var hookPath = Path.Combine(this.dalamud.StartInfo.WorkingDirectory, "ResourceHook", path);

            if (System.IO.File.Exists(hookPath))
            {
                hookInfo.DetourFile = new FileStream(hookPath, FileMode.Open);
                Log.Verbose("-> Added resource hook detour at {0}", hookPath);
            }

            this.resourceHookMap.Add(handlePtr, hookInfo);
        }

        public static bool FilePathHasInvalidChars(string path)
        {
            return !string.IsNullOrEmpty(path) && path.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0;
        }
    }
}
