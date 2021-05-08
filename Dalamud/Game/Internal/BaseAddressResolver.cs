using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Dalamud.Game.Internal
{
    public abstract class BaseAddressResolver
    {
        protected bool IsResolved { get; set; }

        public static Dictionary<string, List<(string, IntPtr)>> DebugScannedValues = new Dictionary<string, List<(string, IntPtr)>>();

        public void Setup(SigScanner scanner)
        {
            // Because C# don't allow to call virtual function while in ctor
            // we have to do this shit :\

            if (this.IsResolved)
            {
                return;
            }

            if (scanner.Is32BitProcess)
            {
                this.Setup32Bit(scanner);
            }
            else
            {
                this.Setup64Bit(scanner);
            }

            this.SetupInternal(scanner);

            var className = this.GetType().Name;
            DebugScannedValues[className] = new List<(string, IntPtr)>();

            foreach (var property in this.GetType().GetProperties().Where(x => x.PropertyType == typeof(IntPtr)))
            {
                DebugScannedValues[className].Add((property.Name, (IntPtr)property.GetValue(this)));
            }

            this.IsResolved = true;
        }

        protected virtual void Setup32Bit(SigScanner scanner)
        {
            throw new NotSupportedException("32 bit version is not supported.");
        }

        protected virtual void Setup64Bit(SigScanner sig)
        {
            throw new NotSupportedException("64 bit version is not supported.");
        }

        protected virtual void SetupInternal(SigScanner scanner)
        {
            // Do nothing
        }

        public T GetVirtualFunction<T>(IntPtr address, int vtableOffset, int count) where T : class
        {
            // Get vtable
            var vtable = Marshal.ReadIntPtr(address, vtableOffset);

            // Get an address to the function
            var functionAddress = Marshal.ReadIntPtr(vtable, IntPtr.Size * count);

            return Marshal.GetDelegateForFunctionPointer<T>(functionAddress);
        }
    }
}
