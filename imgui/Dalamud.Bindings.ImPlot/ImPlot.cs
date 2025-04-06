using System.Reflection;

namespace Dalamud.Bindings.ImPlot
{
    using HexaGen.Runtime;
    using System.Diagnostics;

    public static class ImPlotConfig
    {
        public static bool AotStaticLink;
    }

    public static unsafe partial class ImPlot
    {
        static ImPlot()
        {
            if (ImPlotConfig.AotStaticLink)
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
            return "cimplot";
        }
    }
}
