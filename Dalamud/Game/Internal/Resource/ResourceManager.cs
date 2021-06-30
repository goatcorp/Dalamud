using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using Dalamud.Hooking;
using Serilog;

namespace Dalamud.Game.Internal.File
{
    /// <summary>
    /// This class facilitates modifying how the game loads resources from disk.
    /// </summary>
    public class ResourceManager
    {
        private readonly Dalamud dalamud;
        private readonly ResourceManagerAddressResolver address;
        private readonly Hook<GetResourceAsyncDelegate> getResourceAsyncHook;
        private readonly Hook<GetResourceSyncDelegate> getResourceSyncHook;

        private Dictionary<IntPtr, ResourceHandleHookInfo> resourceHookMap = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceManager"/> class.
        /// </summary>
        /// <param name="dalamud">The Dalamud instance.</param>
        /// <param name="scanner">The SigScanner instance.</param>
        public ResourceManager(Dalamud dalamud, SigScanner scanner)
        {
            this.dalamud = dalamud;
            this.address = new ResourceManagerAddressResolver();
            this.address.Setup(scanner);

            Log.Verbose("=====  R E S O U R C E   M A N A G E R  =====");
            Log.Verbose("GetResourceAsync address {GetResourceAsync}", this.address.GetResourceAsync);
            Log.Verbose("GetResourceSync address {GetResourceSync}", this.address.GetResourceSync);

            this.getResourceAsyncHook = new Hook<GetResourceAsyncDelegate>(this.address.GetResourceAsync, this.GetResourceAsyncDetour);
            this.getResourceSyncHook = new Hook<GetResourceSyncDelegate>(this.address.GetResourceSync, this.GetResourceSyncDetour);
        }

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr GetResourceAsyncDelegate(IntPtr manager, IntPtr a2, IntPtr a3, IntPtr a4, IntPtr pathPtr, IntPtr a6, byte a7);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr GetResourceSyncDelegate(IntPtr manager, IntPtr a2, IntPtr a3, IntPtr a4, IntPtr pathPtr, IntPtr a6);

        /// <summary>
        /// Check if a filepath has any invalid characters.
        /// </summary>
        /// <param name="path">The filepath to check.</param>
        /// <returns>A value indicating whether the filepath is safe to use.</returns>
        public static bool FilePathHasInvalidChars(string path)
        {
            return !string.IsNullOrEmpty(path) && path.IndexOfAny(Path.GetInvalidPathChars()) >= 0;
        }

        /// <summary>
        /// Enable this module.
        /// </summary>
        public void Enable()
        {
            this.getResourceAsyncHook.Enable();
            this.getResourceSyncHook.Enable();
        }

        /// <summary>
        /// Dispose of managed and unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.getResourceAsyncHook.Dispose();
            this.getResourceSyncHook.Dispose();
        }

        private IntPtr GetResourceAsyncDetour(IntPtr manager, IntPtr a2, IntPtr a3, IntPtr a4, IntPtr pathPtr, IntPtr a6, byte a7)
        {
            try
            {
                var path = Marshal.PtrToStringAnsi(pathPtr);

                var resourceHandle = this.getResourceAsyncHook.Original(manager, a2, a3, a4, IntPtr.Zero, a6, a7);
                // var resourceHandle = IntPtr.Zero;

                Log.Verbose("GetResourceAsync CALL - this:{0} a2:{1} a3:{2} a4:{3} a5:{4} a6:{5} a7:{6} => RET:{7}", manager, a2, a3, a4, pathPtr, a6, a7, resourceHandle);

                Log.Verbose($"->{path}");

                this.HandleGetResourceHookAcquire(resourceHandle, path);

                return resourceHandle;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception on ReadResourceAsync hook.");

                return this.getResourceAsyncHook.Original(manager, a2, a3, a4, pathPtr, a6, a7);
            }
        }

        private IntPtr GetResourceSyncDetour(IntPtr manager, IntPtr a2, IntPtr a3, IntPtr a4, IntPtr pathPtr, IntPtr a6)
        {
            try
            {
                var resourceHandle = this.getResourceSyncHook.Original(manager, a2, a3, a4, pathPtr, a6);

                Log.Verbose("GetResourceSync CALL - this:{0} a2:{1} a3:{2} a4:{3} a5:{4} a6:{5} => RET:{6}", manager, a2, a3, a4, pathPtr, a6, resourceHandle);

                var path = Marshal.PtrToStringAnsi(pathPtr);

                Log.Verbose($"->{path}");

                this.HandleGetResourceHookAcquire(resourceHandle, path);

                return resourceHandle;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception on ReadResourceSync hook.");

                return this.getResourceSyncHook.Original(manager, a2, a3, a4, pathPtr, a6);
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

        private class ResourceHandleHookInfo
        {
            public string Path { get; set; }

            public Stream DetourFile { get; set; }
        }
    }
}
