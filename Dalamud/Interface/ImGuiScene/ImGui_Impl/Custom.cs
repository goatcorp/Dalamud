using System.Runtime.InteropServices;

namespace ImGuiScene.ImGui_Impl {
    
    // Custom cimgui functions we use for utility purposes
    internal static class Custom {
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void igCustom_ClearStacks();
    }
}
