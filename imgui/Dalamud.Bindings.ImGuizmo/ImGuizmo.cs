using System.Reflection;

namespace Dalamud.Bindings.ImGuizmo
{
    using HexaGen.Runtime;
    using System.Diagnostics;

    public static class ImGuizmoConfig
    {
        public static bool AotStaticLink;
    }

    public static unsafe partial class ImGuizmo
    {
        static ImGuizmo()
        {
            if (ImGuizmoConfig.AotStaticLink)
            {
                InitApi(new NativeLibraryContext(Process.GetCurrentProcess().MainModule!.BaseAddress));
            }
            else
            {
                // InitApi(new NativeLibraryContext(LibraryLoader.LoadLibrary(GetLibraryName, null)));
                InitApi(new NativeLibraryContext(Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location)!, GetLibraryName() + ".dll")));
            }
        }

        public static string GetLibraryName()
        {
            return "cimguizmo";
        }
    }
}
