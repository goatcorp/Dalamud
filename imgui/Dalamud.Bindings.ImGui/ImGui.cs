#nullable disable

using System.Reflection;

namespace Dalamud.Bindings.ImGui
{
    using HexaGen.Runtime;
    using System.Diagnostics;

    public static class ImGuiConfig
    {
        public static bool AotStaticLink;
    }

    public static unsafe partial class ImGui
    {
        static ImGui()
        {
            if (ImGuiConfig.AotStaticLink)
            {
                InitApi(new NativeLibraryContext(Process.GetCurrentProcess().MainModule!.BaseAddress));
            }
            else
            {
                //InitApi(new NativeLibraryContext(LibraryLoader.LoadLibrary(GetLibraryName, null)));
                InitApi(new NativeLibraryContext(Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location)!, GetLibraryName() + ".dll")));
            }
        }

        public static string GetLibraryName()
        {
            return "cimgui";
        }

        public const nint ImDrawCallbackResetRenderState = -8;
    }
}
