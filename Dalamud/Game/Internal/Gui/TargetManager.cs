using System;
using System.Runtime.InteropServices;

using Dalamud.Hooking;
using Serilog;

namespace Dalamud.Game.Internal.Gui
{
    public class TargetManager
    {
        public delegate IntPtr GetTargetDelegate(IntPtr manager);

        private Hook<GetTargetDelegate> getTargetHook;

        private TargetManagerAddressResolver Address;

        public unsafe TargetManager(Dalamud dalamud, SigScanner scanner)
        {
            this.Address = new TargetManagerAddressResolver();
            this.Address.Setup(scanner);

            Log.Verbose("===== T A R G E T   M A N A G E R =====");
            Log.Verbose("GetTarget address {GetTarget}", this.Address.GetTarget);

            this.getTargetHook = new Hook<GetTargetDelegate>(this.Address.GetTarget, new GetTargetDelegate(this.GetTargetDetour), this);
        }

        public void Enable()
        {
            this.getTargetHook.Enable();
        }

        public void Dispose()
        {
            this.getTargetHook.Dispose();
        }

        private IntPtr GetTargetDetour(IntPtr manager)
        {
            try
            {
                var res = this.getTargetHook.Original(manager);

                var test = Marshal.ReadInt32(res);

                Log.Debug($"GetTargetDetour {manager.ToInt64():X} -> RET: {res:X}");

                return res;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception GetTargetDetour hook.");
                return this.getTargetHook.Original(manager);
            }
        }
    }
}
