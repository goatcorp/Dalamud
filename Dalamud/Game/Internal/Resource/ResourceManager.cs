using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.Internal.Libc;
using Dalamud.Hooking;
using Serilog;

namespace Dalamud.Game.Internal.File
{
    public class ResourceManager {
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr GetResourceAsyncDelegate(IntPtr manager, IntPtr a2, IntPtr a3, IntPtr a4, IntPtr a5, IntPtr a6, byte a7);
        private readonly Hook<GetResourceAsyncDelegate> getResourceAsyncHook;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr GetResourceSyncDelegate(IntPtr manager, IntPtr a2, IntPtr a3, IntPtr a4, IntPtr a5, IntPtr a6);
        private readonly Hook<GetResourceSyncDelegate> getResourceSyncHook;

        private ResourceManagerAddressResolver Address { get; }
        private readonly Dalamud dalamud;

        class ResourceHandleHookInfo {
            public string Path { get; set; }
            public Stream DetourFile { get; set; }
        }

        private Dictionary<IntPtr, ResourceHandleHookInfo> resourceHookMap = new Dictionary<IntPtr, ResourceHandleHookInfo>();

        public ResourceManager(Dalamud dalamud, SigScanner scanner) {
            this.dalamud = dalamud;
            Address = new ResourceManagerAddressResolver();
            Address.Setup(scanner);

            Log.Verbose("=====  R E S O U R C E   M A N A G E R  =====");
            Log.Verbose("GetResourceAsync address {GetResourceAsync}", Address.GetResourceAsync);
            Log.Verbose("GetResourceSync address {GetResourceSync}", Address.GetResourceSync);

            this.getResourceAsyncHook =
                new Hook<GetResourceAsyncDelegate>(Address.GetResourceAsync,
                                                    new GetResourceAsyncDelegate(GetResourceAsyncDetour),
                                                    this);
            
            this.getResourceSyncHook =
                new Hook<GetResourceSyncDelegate>(Address.GetResourceSync,
                                              new GetResourceSyncDelegate(GetResourceSyncDetour),
                                              this);
                                              
        }

        public void Enable() {
            this.getResourceAsyncHook.Enable();
            this.getResourceSyncHook.Enable();
        }

        public void Dispose() {
            this.getResourceAsyncHook.Dispose();
            this.getResourceSyncHook.Dispose();
        }
        
        private IntPtr GetResourceAsyncDetour(IntPtr manager, IntPtr a2, IntPtr a3, IntPtr a4, IntPtr a5, IntPtr a6, byte a7) {

            try {
                var path = Marshal.PtrToStringAnsi(a5);

                var resourceHandle = this.getResourceAsyncHook.Original(manager, a2, a3, a4, IntPtr.Zero, a6, a7);
                //var resourceHandle = IntPtr.Zero;

                Log.Verbose("GetResourceAsync CALL - this:{0} a2:{1} a3:{2} a4:{3} a5:{4} a6:{5} a7:{6} => RET:{7}", manager, a2, a3, a4, a5, a6, a7, resourceHandle);

                Log.Verbose($"->{path}");

                HandleGetResourceHookAcquire(resourceHandle, path);

                return resourceHandle;
            } catch (Exception ex) {
                Log.Error(ex, "Exception on ReadResourceAsync hook.");

                return this.getResourceAsyncHook.Original(manager, a2, a3, a4, a5, a6, a7);
            }
        }

        private void DumpMem(IntPtr address, int len = 512) {
            if (address == IntPtr.Zero)
                return;

            var data = new byte[len];
            Marshal.Copy(address, data, 0, len);

            Log.Verbose($"MEMDMP at {address.ToInt64():X} for {len:X}\n{ByteArrayToHex(data)}");
        }

        private IntPtr GetResourceSyncDetour(IntPtr manager, IntPtr a2, IntPtr a3, IntPtr a4, IntPtr a5, IntPtr a6) {

            try {
                var resourceHandle = this.getResourceSyncHook.Original(manager, a2, a3, a4, a5, a6);

                Log.Verbose("GetResourceSync CALL - this:{0} a2:{1} a3:{2} a4:{3} a5:{4} a6:{5} => RET:{6}", manager, a2, a3, a4, a5, a6, resourceHandle);

                var path = Marshal.PtrToStringAnsi(a5);

                Log.Verbose($"->{path}");

                HandleGetResourceHookAcquire(resourceHandle, path);

                return resourceHandle;
            } catch (Exception ex) {
                Log.Error(ex, "Exception on ReadResourceSync hook.");

                return this.getResourceSyncHook.Original(manager, a2, a3, a4, a5, a6);
            }
        }

        private void HandleGetResourceHookAcquire(IntPtr handlePtr, string path) {
            if (FilePathHasInvalidChars(path))
                return;

            if (this.resourceHookMap.ContainsKey(handlePtr)) {
                Log.Verbose($"-> Handle {handlePtr.ToInt64():X}({path}) was cached!");
                return;
            }

            var hookInfo = new ResourceHandleHookInfo {
                Path = path
            };

            var hookPath = Path.Combine(this.dalamud.StartInfo.WorkingDirectory, "ResourceHook", path);

            if (System.IO.File.Exists(hookPath)) {
                hookInfo.DetourFile = new FileStream(hookPath, FileMode.Open);
                Log.Verbose("-> Added resource hook detour at {0}", hookPath);
            }

            this.resourceHookMap.Add(handlePtr, hookInfo);
        }

        public static bool FilePathHasInvalidChars(string path)
        {

            return (!string.IsNullOrEmpty(path) && path.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0);
        }

        public static string ByteArrayToHex(byte[] bytes, int offset = 0, int bytesPerLine = 16)
        {
            if (bytes == null)
            {
                return string.Empty;
            }

            var hexChars = "0123456789ABCDEF".ToCharArray();

            var offsetBlock = 8 + 3;
            var byteBlock = offsetBlock + bytesPerLine * 3 + (bytesPerLine - 1) / 8 + 2;
            var lineLength = byteBlock + bytesPerLine + Environment.NewLine.Length;

            var line = (new string(' ', lineLength - Environment.NewLine.Length) + Environment.NewLine).ToCharArray();
            var numLines = (bytes.Length + bytesPerLine - 1) / bytesPerLine;

            var sb = new StringBuilder(numLines * lineLength);

            for (var i = 0; i < bytes.Length; i += bytesPerLine)
            {
                var h = i + offset;

                line[0] = hexChars[(h >> 28) & 0xF];
                line[1] = hexChars[(h >> 24) & 0xF];
                line[2] = hexChars[(h >> 20) & 0xF];
                line[3] = hexChars[(h >> 16) & 0xF];
                line[4] = hexChars[(h >> 12) & 0xF];
                line[5] = hexChars[(h >> 8) & 0xF];
                line[6] = hexChars[(h >> 4) & 0xF];
                line[7] = hexChars[(h >> 0) & 0xF];

                var hexColumn = offsetBlock;
                var charColumn = byteBlock;

                for (var j = 0; j < bytesPerLine; j++)
                {
                    if (j > 0 && (j & 7) == 0)
                    {
                        hexColumn++;
                    }

                    if (i + j >= bytes.Length)
                    {
                        line[hexColumn] = ' ';
                        line[hexColumn + 1] = ' ';
                        line[charColumn] = ' ';
                    }
                    else
                    {
                        var by = bytes[i + j];
                        line[hexColumn] = hexChars[(by >> 4) & 0xF];
                        line[hexColumn + 1] = hexChars[by & 0xF];
                        line[charColumn] = by < 32 ? '.' : (char)by;
                    }

                    hexColumn += 3;
                    charColumn++;
                }

                sb.Append(line);
            }

            return sb.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        }
    }
}
