using System;
using System.Runtime.InteropServices;

namespace Dalamud.Game.Internal {
    public abstract class BaseAddressResolver {
        protected bool IsResolved { get; set; }
        
        public void Setup(SigScanner scanner) {
            // Because C# don't allow to call virtual function while in ctor
            // we have to do this shit :\
            
            if (IsResolved) {
                return;
            }
            
            if (scanner.Is32BitProcess) {
                Setup32Bit(scanner);
            } else {
                Setup64Bit(scanner);
            }
            SetupInternal(scanner);
            
            IsResolved = true;
        }
        
        protected virtual void Setup32Bit(SigScanner scanner) {
            throw new NotSupportedException("32 bit version is not supported.");
        }

        protected virtual void Setup64Bit(SigScanner sig) {
            throw new NotSupportedException("64 bit version is not supported.");
        }

        protected virtual void SetupInternal(SigScanner scanner) {
            // Do nothing 
        }

        protected T GetVirtualFunction<T>(IntPtr address, int vtableOffset, int count) where T : class {
            // Get vtable
            var vtable = Marshal.ReadIntPtr(address, vtableOffset);
            
            // Get an address to the function
            var functionAddress = Marshal.ReadIntPtr(vtable, IntPtr.Size * count);
            
            return Marshal.GetDelegateForFunctionPointer<T>(functionAddress);
        }
    }
}
