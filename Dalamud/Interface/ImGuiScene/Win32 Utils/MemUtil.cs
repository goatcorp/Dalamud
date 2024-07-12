using System.Runtime.InteropServices;

namespace ImGuiScene
{
    public static unsafe class MemUtil
    {
        public static T* Allocate<T>() where T : unmanaged {
            return (T*)Marshal.AllocHGlobal(Marshal.SizeOf<T>());
        }

        public static void Free(this IntPtr obj) {
            Marshal.FreeHGlobal(obj);
        }
    }
}
