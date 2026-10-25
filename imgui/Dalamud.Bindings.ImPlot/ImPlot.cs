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
        /// <summary>Same values as the default constructor of the native <c>ImPlotSpec</c>.</summary>
        /// <remarks>Default arguments use this instead of the native constructor, which allocates on every call.</remarks>
        internal static readonly ImPlotSpec DefaultSpec = new()
        {
            LineColor = new(0, 0, 0, -1),
            LineWeight = 1,
            FillColor = new(0, 0, 0, -1),
            FillAlpha = 1,
            Marker = ImPlotMarker.None,
            MarkerSize = 4,
            MarkerLineColor = new(0, 0, 0, -1),
            MarkerFillColor = new(0, 0, 0, -1),
            Size = 4,
            Stride = -1,
        };

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
