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

            var linuxPath = Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location)!, GetLibraryName() + ".so");
            var windowsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location)!, GetLibraryName() + ".dll");

            // This shouldn't affect wine as it'll be reported as Win32NT
            if (System.Environment.OSVersion.Platform == PlatformID.Unix && File.Exists(linuxPath))
            {
                InitApi(new NativeLibraryContext(linuxPath));
            }
            else
            {
                InitApi(new NativeLibraryContext(windowsPath));
            }
        }

        public static string GetLibraryName()
        {
            return "cimgui";
        }

        public const nint ImDrawCallbackResetRenderState = -8;
    }
}
